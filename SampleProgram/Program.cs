using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using HttpWebReFormer;

namespace HttpWebReFormer.SampleProgram
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            //Enable caching on the extensions (it's true by default)
            //This will allow the methods to return cached results
            //for Links, Forms and Images instead of having to
            //re-parse the response on every call
            HttpWebResponseExtensions.EnableCaching = true;

            // Get the login form, by allowing a redirect to the login page
            var fileLocation = new Uri("https://www.facebook.com");

            //Store session variables here
            var cookies = new CookieContainer();

            var formRequestClient = WebRequest.Create(fileLocation) as HttpWebRequest;
            formRequestClient.Method = WebRequestMethods.Http.Post;
            formRequestClient.ContentLength = 0;
            formRequestClient.CookieContainer = cookies;
            formRequestClient.AllowAutoRedirect = true;
            formRequestClient.UseDefaultCredentials = true;

            // Send the initial request for the login page
            var formResponse = formRequestClient.GetResponse() as HttpWebResponse;

            //We've already verified that 'login_form' is the one we need
            //by inspecting the available Ids
            var loginForm = formResponse.Forms()
							.Where(f => f.Id == "login_form")
							.First();

            // This is how you get the links and images if you need them
            var loginImages = formResponse.Images();
            var loginLinks = formResponse.Links();

            //Enter the username and password form values here
            //In practice, you'll need to inspect the available
            //fields to figure out which one to use
            loginForm.Fields["email"] = "youremail@email.com";
            loginForm.Fields["pass"] = "yourpassword";

            //Prepare the login request
            //=========================
            //Depending on the server, the Action may be a complete URL or you may
            //need to concatenate it with the hostname
//            var loginUrl = new Uri(fileLocation.Scheme + "://" + fileLocation.Host + "/" + loginForm.Action);
            var loginUrl = loginForm.Action;

            var loginClient = WebRequest.Create(loginUrl) as HttpWebRequest;
            loginClient.CookieContainer = cookies;
            loginClient.Method = WebRequestMethods.Http.Post;
            loginClient.AllowAutoRedirect = true;
            loginClient.ContentType = "application/x-www-form-urlencoded";
            loginClient.UseDefaultCredentials = true;
            loginClient.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
            loginClient.ProtocolVersion = HttpVersion.Version11;

            //Write the post data into the body of the request
            string postData = loginForm.Fields.ToString();
            loginClient.Body(postData);

            var oldCookiesCount = cookies.Count;

            // Send login request
            var loginResponse = loginClient.GetResponse() as HttpWebResponse;

            //verify login success by checking for authentication cookies
            if (cookies.Count == oldCookiesCount)
            {
                Console.WriteLine("Login failed.");
                return;
            }

            // Prepare authenticated download request
            var fileDownloadClient = WebRequest.Create(fileLocation) as HttpWebRequest;
            fileDownloadClient.Method = WebRequestMethods.Http.Post;
            fileDownloadClient.ContentLength = 0;
            fileDownloadClient.CookieContainer = cookies;
            fileDownloadClient.UseDefaultCredentials = true;

            // Send download request
            var downloadResponse = fileDownloadClient.GetResponse() as HttpWebResponse;

            if (downloadResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("File Download returned status code: " + downloadResponse.StatusCode);
                return;
            }

            //Dump the response into a file
            var stream = downloadResponse.GetResponseStream();
            var filestr = File.Create("Output.txt");
            stream.CopyTo(filestr);
            filestr.Flush();
            filestr.Close();

			
        }
    }
}
