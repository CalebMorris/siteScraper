using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace SiteScraper
{
	public class SiteScraperUtility
	{
		// Example Inputs
		// url: http://www.google.com/foo/bar
		// path: /home/username/Desktop/google/
		public static void Scrape(string url, string path)
		{
			try
			{
				// Full path of saved file should be path+url(after top level domain)
				//System.Console.WriteLine("Scraping url:{0}\n\tto Path:{1}", url, path);

				// Check if this portion has already completed
				Uri uri = new Uri(url);
				int depth = 0;
				string dataPath = null;
				if (uri.AbsolutePath == "/")
				{
					dataPath = Path.Combine(path, c_indexFileName);
					if (File.Exists(dataPath))
						return;
					else
					{
						dataPath = GetUrl((new Uri(uri, c_indexFileName)).ToString(), dataPath);
					}
				}
				else
				{
					string[] directory = uri.AbsolutePath.Split('/').Where(x => x != "").ToArray();
					depth = directory.Length;
					dataPath = dataPath ?? "";
					foreach (string dir in directory.Reverse().Skip(1).Reverse())
					{
						dataPath = Path.Combine(dataPath, dir);
						string currentLevel = (new Uri(Path.Combine(path, dataPath))).AbsolutePath;
						if (!FileOrDirectoryExists(currentLevel))
						{
							System.Console.WriteLine("Creating Dir:@{1}:{0}", currentLevel, new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileLineNumber());
							Directory.CreateDirectory(currentLevel);
						}
					}
					// If the file doesn't have an extension append html
					string filename = directory.Last();
					IfNecessaryAppendHtml(ref filename);
					dataPath = Path.Combine(path, Path.Combine(dataPath, filename));
					if (File.Exists(dataPath))
					{
						System.Console.Error.WriteLine("{0} already exists", dataPath);
						return;
					}
					else
					{
						if (filename.Split('.').Where(x => x != "").Last().StartsWith("htm"))
							depth = directory.Length - 1;
						GetUrl(url, dataPath);
					}
				}

				List<string> resources = GetLinks(dataPath, depth);

				foreach (string resource in resources)
				{
					if (resource.First() == '/')
					{
						string nextUrl = String.Format("{0}{1}{2}{3}", uri.Scheme, Uri.SchemeDelimiter, uri.Authority, resource);
						//System.Console.WriteLine("nextUrl:{0}", nextUrl);
						SiteScraperUtility.Scrape(nextUrl, path);
					}
					else
					{
						//File that is an outside resource
					}
				}
			}
			catch(WebException e)
			{
				Console.WriteLine("WebExceptionMessage :" + e.Message);
				if(e.Status == WebExceptionStatus.ProtocolError) {
					Console.WriteLine("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode);
					Console.WriteLine("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription);
				}
			}
		}

		static string GetUrl(string url, string path)
		{
			HttpWebResponse response;
			try
			{
				HttpWebRequest request = HttpWebRequest.Create(new UriBuilder(url).Uri) as HttpWebRequest;
				request.UserAgent = "TurtleSpider/0.1";
				using (response = request.GetResponse() as HttpWebResponse)
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						using (Stream fileOut = File.Create(path))
						using (Stream responseStream = response.GetResponseStream())
						{
							byte[] buffer = new byte[8 * 1024];
							int len;
							while ((len = responseStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								fileOut.Write(buffer, 0, len);
							}
						}

						return path;
					}
				}
			}
			catch (WebException ex)
			{
				response = ex.Response as HttpWebResponse;
				if (response.StatusCode == HttpStatusCode.NotFound && response.ResponseUri.AbsoluteUri.EndsWith("index.html"))
				{
					return GetUrl(response.ResponseUri.Authority + "/index.php", path.Remove(path.Length - 4) + "php");
				}
			}
			return default(string);
		}

		static List<string> GetLinks(string urlData, int depth)
		{
			HtmlDocument doc = new HtmlDocument();
			List<string> resources = new List<string>();
			doc.Load(urlData);
			if (doc.ParseErrors == null || doc.ParseErrors.Count() == 0)
			{
				if (doc.DocumentNode != null)
				{
					foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//link[@href]").EmptyOrNotNull())
					{
						HtmlAttribute att = link.Attributes["href"];
						resources.Add(att.Value);
						string attributeValue = att.Value;
						IfNecessaryAppendHtml(ref attributeValue);
						if (attributeValue.ToCharArray().First() == '/')
						{
							string prependDepth = ".";
							for (int i = 0; i < depth; ++i)
							{
								prependDepth += "/..";
							}
							attributeValue = string.Format("{1}{0}", attributeValue, prependDepth);
						}
						att.Value = attributeValue;
						link.Attributes["href"] = att;
					}
					foreach (HtmlNode imgLink in doc.DocumentNode.SelectNodes("//img[@src]").EmptyOrNotNull())
					{
						HtmlAttribute att = imgLink.Attributes["src"];
						resources.Add(att.Value);
						string attributeValue = att.Value;
						IfNecessaryAppendHtml(ref attributeValue);
						if (attributeValue.ToCharArray().First() == '/')
						{
							string prependDepth = ".";
							for (int i = 0; i < depth; ++i)
							{
								prependDepth += "/..";
							}
							attributeValue = string.Format("{1}{0}", attributeValue, prependDepth);
						}
						att.Value = attributeValue;
						imgLink.Attributes["src"] = att;
					}
					foreach (HtmlNode hyperLink in doc.DocumentNode.SelectNodes("//a[@href]").EmptyOrNotNull())
					{
						HtmlAttribute att = hyperLink.Attributes["href"];
						resources.Add(att.Value);
						string attributeValue = att.Value;
						IfNecessaryAppendHtml(ref attributeValue);
						if (attributeValue.ToCharArray().First() == '/')
						{
							string prependDepth = ".";
							for (int i = 0; i < depth; ++i)
							{
								prependDepth += "/..";
							}
							attributeValue = string.Format("{1}{0}", attributeValue, prependDepth);
						}
						att.Value = attributeValue;
						hyperLink.Attributes["href"] = att;
					}
					doc.Save(urlData);
				}
			}
			return resources;
		}

		public static bool FileOrDirectoryExists(string name)
		{
			return (Directory.Exists(name) || File.Exists(name));
		}

		static bool HasAFileExtension(string filename)
		{
			return filename.Split('.').Last() != filename && filename.Split('.').Last().Length <= 4;
		}

		static void IfNecessaryAppendHtml(ref string filename)
		{
			if (!HasAFileExtension(filename))
				filename = String.Format("{0}{1}", filename, c_noExtensionFile);
		}

		public const string c_indexFileName = "index.html";
		public const string c_noExtensionFile = ".html";
	}
}

