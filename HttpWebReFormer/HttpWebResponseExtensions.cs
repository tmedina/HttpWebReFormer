using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using HttpWebReFormer;

/***********************************************************
 * HttpWebReformer
 * Author: Terrance Medina
 *
 * Description: A set of extension methods to HttpWebRequest
 * and HttpWebResponse to simplify their use when making
 * cookie-based authenticated requests.
 * Their behavior is similar to that of the Invoke-WebRequest
 * cmdlet available in Powershell
***********************************************************/

namespace HttpWebReFormer
{
	public static class HttpWebREquestExtensions
	{
			/*******************************************************
			 * 	Body
			 * 		Write string data into the body of a request
			*******************************************************/
		public static void Body(this HttpWebRequest r, string data)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			r.ContentLength = bytes.Length;
			var reqStream = r.GetRequestStream();
			using (StreamWriter strWriter = new StreamWriter(reqStream))
			{
				strWriter.Write(data);
				strWriter.Flush();
			}


		}
	}

	public static class HttpWebResponseExtensions
	{
		public static bool EnableCaching { get; set; }

		//Caches
		private static Dictionary<HttpWebResponse, HtmlDocument> documentCache = new Dictionary<HttpWebResponse, HtmlDocument>();
		private static Dictionary<HttpWebResponse, MemoryStream> streamCache = new Dictionary<HttpWebResponse, MemoryStream>();
		private static Dictionary<HttpWebResponse, IEnumerable<Dictionary<string,string>>> linksCache = new Dictionary<HttpWebResponse, IEnumerable<Dictionary<string,string>>>();
		private static Dictionary<HttpWebResponse, IEnumerable<Dictionary<string,string>>> imagesCache = new Dictionary<HttpWebResponse, IEnumerable<Dictionary<string,string>>>();
		private static Dictionary<HttpWebResponse, List<HttpWebResponseForm>> formsCache = new Dictionary<HttpWebResponse, List<HttpWebResponseForm>>();

		//CTOR
		static HttpWebResponseExtensions()
		{
			EnableCaching = true;
			HtmlNode.ElementsFlags.Remove("form");

		}

				/************************************************************
				 * GetResponseStreamCopy
				 * 	Copy the ResponseStream into memory and cache it for reuse
				************************************************************/
		public static Stream GetResponseStreamCopy(this HttpWebResponse r)
		{
			if (!streamCache.ContainsKey(r))
			{
				MemoryStream ms = new MemoryStream();
				r.GetResponseStream().CopyTo(ms);
				//Always Cache
				streamCache.Add(r, ms);
			}
			streamCache[r].Position = 0;
			return streamCache[r] as Stream;

		}

        /**************************************************************
				 * GetResponseDocument
				 * 	Parse the response stream and cache it for reuse. Uses the
				 * 	HtmlAgilityPack library for the heavy lifting.
				**************************************************************/
		public static HtmlDocument GetResponseDocument(this HttpWebResponse r)
		{
			if (!documentCache.ContainsKey(r))
			{

				try
				{
					HtmlDocument doc = new HtmlDocument();
					doc.Load(r.GetResponseStreamCopy());
					//Always Cache
					documentCache.Add(r, doc);
				}
				catch (Exception e)
				{
					throw new Exception("Unable to load HtmlDocument from Response Stream. " + e.Message);
				}

			}
			return documentCache[r];


		}

		// Extract the forms from the parsed response stream
		public static List<HttpWebResponseForm> Forms(this HttpWebResponse r)
		{
			if (formsCache.ContainsKey(r))
				return formsCache[r];

			var result = new List<HttpWebResponseForm>();

			HtmlDocument parsedDoc;
			try
			{
				parsedDoc = GetResponseDocument(r);
			}
			catch
			{
				return result;
			}

			var listOfForms = parsedDoc.DocumentNode.Descendants()
				.Where(d => d.Name == "form")
				.Select(f =>
				new
				{
					id = f.Attributes
								.Where(a => a.Name == "id")
								.Select(a => a.Value)
								.DefaultIfEmpty("")
								.First(),
					name = f.Attributes
									.Where(a => a.Name == "name")
									.Select(a => a.Value)
									.DefaultIfEmpty("")
									.First(),
					method = f.Attributes
										.Where(a => a.Name == "method")
										.Select(a => a.Value)
										.DefaultIfEmpty("")
										.First(),
					action = f.Attributes
										.Where(a => a.Name == "action")
										.Select(a => a.Value)
										.DefaultIfEmpty("")
										.First(),
					inputs = f.Descendants()
										.Where(d => d.Name == "input")
										.Select(i => i.Attributes.ToList())
										.Select(ag => new
											{
												name = ag.Where(a => a.Name == "name")
																	.Select(a => a.Value)
																	.DefaultIfEmpty("")
																	.First(),
												value = ag.Where(a => a.Name == "value")
																						.Select(a => a.Value)
																						.DefaultIfEmpty("")
																						.First(),
											})
				}
			);

			foreach (var f in listOfForms)
			{
				var form = new HttpWebResponseForm();
				form.Action = f.action;
				form.Id = f.id;
				form.Name = f.name;
				form.Method = f.method;
				form.Fields = HttpUtility.ParseQueryString("");
				foreach (var ff in f.inputs)
				{
					form.Fields.Add(ff.name, HttpUtility.HtmlDecode(ff.value));
				}

				result.Add(form);
			}

			if (EnableCaching)
				formsCache.Add(r, result);
			return result;

		}


		// Extract img tags and attributes
		public static IEnumerable<Dictionary<string, string>> Images(this HttpWebResponse r)
		{
			if (imagesCache.ContainsKey(r))
				return imagesCache[r];
			var result = r.GetTags("img");
			if (EnableCaching)
				imagesCache.Add(r, result);
			return result;
		}

		// Extract a tags and attributes
		public static IEnumerable<Dictionary<string, string>> Links(this HttpWebResponse r)
		{
			if (linksCache.ContainsKey(r))
				return imagesCache[r];
			var result = r.GetTags("a");
			if (EnableCaching)
				linksCache.Add(r, result);
			return result;
		}

		// Extract tags and attributes
		public static IEnumerable<Dictionary<string, string>> GetTags(this HttpWebResponse r, string tagName)
		{

			HtmlDocument parsedDoc;
			try
			{
				parsedDoc = GetResponseDocument(r);
			}
			catch
			{
				return new List<Dictionary<string,string>>();
			}

			var attributeMap = parsedDoc.DocumentNode.Descendants()
				.Where(d => d.Name == tagName)
				.Select(
					tag => (new List<KeyValuePair<string, string>>()
					{
						new KeyValuePair<string, string>("innerHTML", tag.InnerHtml),
						new KeyValuePair<string, string>("outerHTML", tag.OuterHtml),
						new KeyValuePair<string, string>("innerText", tag.InnerText),
						new KeyValuePair<string, string>("tagName", tag.Name),
					})
				.Union(
					tag.Attributes
							.Select(attr => new KeyValuePair<string, string>(attr.Name, attr.Value))
				)
				.ToDictionary(k => k.Key, v => v.Value)
			);

			return attributeMap;
		}

	}
}
