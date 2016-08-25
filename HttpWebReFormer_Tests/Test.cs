using System;
using System.Linq;
using System.Net;
using HttpWebReFormer;
using NUnit.Framework;

namespace FileDownloaderTestSuite
{
	[TestFixture ()]
	public class Test
	{
		HttpWebResponse formResponse;

		[TestFixtureSetUp]
		public void Init()
		{
        var fileLocation = new Uri("http://www.twitter.com");
        CookieContainer cookies = new CookieContainer();
        var formRequestClient = WebRequest.Create(fileLocation) as HttpWebRequest;
        formRequestClient.Method = WebRequestMethods.Http.Post;
        formRequestClient.ContentLength = 0;
        formRequestClient.CookieContainer = cookies;
        formRequestClient.AllowAutoRedirect = true;
        formRequestClient.UseDefaultCredentials = true;
        formResponse = formRequestClient.GetResponse() as HttpWebResponse;
	
		}

		[Test ()]
		public void TestForms ()
		{
				var numberOfForms = formResponse.Forms().Count;
				Assert.AreEqual(5, numberOfForms, "Wrong number of forms returned.");
		}

		[Test()]
		public void TestNumberOfFields()
		{
				var numberOfFields1 = formResponse.Forms()[0].Fields.Count;
				var numberOfFields2 = formResponse.Forms()[1].Fields.Count;
				var numberOfFields3 = formResponse.Forms()[2].Fields.Count;
				var numberOfFields4 = formResponse.Forms()[3].Fields.Count;
				var numberOfFields5 = formResponse.Forms()[4].Fields.Count;
				Assert.AreEqual(1, numberOfFields1, "Wrong number of fields returned.");
				Assert.AreEqual(1, numberOfFields2, "Wrong number of fields returned.");
				Assert.AreEqual(0, numberOfFields3, "Wrong number of fields returned.");
				Assert.AreEqual(1, numberOfFields4, "Wrong number of fields returned.");
				Assert.AreEqual(8, numberOfFields5, "Wrong number of fields returned.");

		}

		[Test()]
		public void TestNumberOfImages()
		{
			var numberOfImages = formResponse.Images().ToList().Count;
			Assert.AreNotEqual(0, numberOfImages, "Wrong number of images returned.");
		}

		[Test()]
		public void TestNumberOfLinks()
		{
			var numberOfLinks = formResponse.Links().ToList().Count;
			Assert.AreNotEqual(0, numberOfLinks, "Wrong number of links returned.");
		}

	}
}
