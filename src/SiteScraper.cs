using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using System.Net;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace SiteScraper
{
	public sealed class SiteScraper
	{
		public SiteScraper(string[] args)
		{
			Options options = new Options();
			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				if (options.Scrape)
				{
					m_isScraping = true;

					if (options.Output != null && options.Paths != null)
					{
						Console.Error.WriteLine("Use either Output or Paths, but not both.");
						Console.Error.WriteLine(options.GetUsage());
						Environment.Exit(-1);
					}

					if (options.Urls != null && options.Paths != null && options.Urls.Length != options.Paths.Length)
					{
						Console.Error.WriteLine("The number of Paths must equal the number of Urls.");
						Console.Error.WriteLine(options.GetUsage());
						Environment.Exit(-1);
					}
				}
				else
				{
					if (options.Output != null)
						Console.WriteLine("Flag \"output\" not used in crawl mode. To scrape use \"--scrape\"");
					if (options.Paths != null)
						Console.WriteLine("Flag \"paths\" not used in crawl mode. To scrape use \"--scrape\"");
				}
				m_urlQueue = new ConcurrentQueue<ScrapePair>();
				if (options.Output != null)
				{
					Uri output;

					if (Uri.TryCreate(options.Output, UriKind.Absolute, out output))
					{
						for (int i = 0; i < options.Urls.Length; ++i)
						{
							Uri url;
							if (Uri.TryCreate(options.Urls[i], UriKind.Absolute, out url))
							{
								m_urlQueue.Enqueue(new ScrapePair(url, output));
							}
							else
							{
								Console.Error.WriteLine("Your url '{0}' was of incorrect form.", options.Urls[i]);
								continue;
							}
						}
					}
					else
					{
						Console.Error.WriteLine("Your output path '{0}' was of incorrect form.", options.Output);
						Environment.Exit(-1);
					}
				}
				else
				{
					for (int i = 0; i < options.Urls.Length; ++i)
					{
						Uri url, path;
						if (!Uri.TryCreate(options.Urls[i], UriKind.Absolute, out url))
						{
							Console.Error.WriteLine("Your url '{0}' was of incorrect form.", options.Urls[i]);
							continue;
						}
						if (!Uri.TryCreate(options.Paths[i], UriKind.Absolute, out path))
						{
							Console.Error.WriteLine("Your path '{0}' was of incorrect form.", options.Paths[i]);
							continue;
						}
						m_urlQueue.Enqueue(new ScrapePair(url, path));
					}
				}
			}
			else
			{
				Environment.Exit(-1);
			}
		}

		public void Scrape()
		{
			Console.WriteLine("pwd:{0}", Directory.GetCurrentDirectory());
			ScrapePair scrapePair;
			while (!m_urlQueue.IsEmpty)
			{
				m_urlQueue.TryDequeue(out scrapePair);

				if (!Directory.Exists(scrapePair.Path.AbsolutePath))
				{
					Console.Error.WriteLine("The following path doesn't exist: {0}", scrapePair.Path);
					System.IO.Directory.CreateDirectory(scrapePair.Path.AbsolutePath);
				}

				// TODO(cm): Add support of other schemes (Issue ID: 1)
				if (scrapePair.Url.Scheme != Uri.UriSchemeHttp)
				{
					Console.Error.WriteLine("Scheme Not Supported: uri '{0}' is not of the HTTP scheme.", scrapePair.Url.AbsoluteUri);
					continue;
				}

				string siteDirectory = Path.Combine(scrapePair.Path.AbsolutePath, scrapePair.Url.Host.Split('.').Reverse().Skip(1).First());

				if (!FileOrDirectoryExists(siteDirectory))
				{
					Directory.CreateDirectory(siteDirectory);
				}
				else
				{
					int i = 2;
					while (FileOrDirectoryExists(string.Format("{0}({1})", siteDirectory, i)))
						i++;
					siteDirectory = string.Format("{0}({1})", siteDirectory, i);
					Directory.CreateDirectory(siteDirectory);
				}

				Scrape(scrapePair.Url.AbsoluteUri, siteDirectory);
			}
		}

		void Scrape(string url, string path)
		{
			try
			{
				Uri uri = new Uri(url);
				int depth = 0;
				string dataPath = null;
				if (uri.AbsolutePath == "/")
				{
					dataPath = Path.Combine(path, "base");
					if (File.Exists(dataPath))
						return;
					else
					{
						dataPath = GetUrl(uri.AbsoluteUri, dataPath);
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
							Console.WriteLine("Creating Dir:@{1}:{0}", currentLevel, new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileLineNumber());
							Directory.CreateDirectory(currentLevel);
						}
					}
					// If the file doesn't have an extension append html
					string filename = directory.Last();
					IfNecessaryAppendHtml(ref filename);
					dataPath = Path.Combine(path, Path.Combine(dataPath, filename));
					if (File.Exists(dataPath))
					{
						Console.Error.WriteLine("{0} already exists", dataPath);
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
						//Console.WriteLine("nextUrl:{0}", nextUrl);
						Scrape(nextUrl, path);
					}
					else
					{
						//File that is an outside resource
					}
				}
			}
			catch (WebException e)
			{
				Console.WriteLine("WebExceptionMessage :" + e.Message);
				if (e.Status == WebExceptionStatus.ProtocolError)
				{
					Console.WriteLine("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode);
					Console.WriteLine("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription);
				}
			}
		}

		string GetUrl(string url, string path)
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
						if (Directory.Exists(path))
							path = Path.Combine(path, "root");
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

		List<string> GetLinks(string urlData, int depth)
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

		bool FileOrDirectoryExists(string name)
		{
			return (Directory.Exists(name) || File.Exists(name));
		}

		bool HasAFileExtension(string filename)
		{
			return filename.Split('.').Last() != filename && filename.Split('.').Last().Length <= 4;
		}

		void IfNecessaryAppendHtml(ref string filename)
		{
			if (!HasAFileExtension(filename))
				filename = String.Format("{0}{1}", filename, c_noExtensionFile);
		}

		public const string c_noExtensionFile = ".html";

		public bool IsScraping { get { return m_isScraping; } }

		ConcurrentQueue<ScrapePair> m_urlQueue;
		readonly bool m_isScraping;

		sealed class ScrapePair
		{
			public ScrapePair(Uri url, Uri path)
			{
				m_url = url;
				m_path = path;
			}

			public Uri Url { get { return m_url; } }
			public Uri Path { get { return m_path; } }

			Uri m_url;
			Uri m_path;
		}

		sealed class Options
		{
			[OptionArray('u', "urls", Required = true, HelpText = "Urls to crawl.")]
			public string[] Urls { get; set; }

			[OptionArray('p', "paths", HelpText = "Path to scrape site to. Will use a subfolder of the site name here. Requireds -s.")]
			public string[] Paths { get; set; }

			[Option('s', "scrape", HelpText = "Should scrape to directory.")]
			public bool Scrape { get; set; }

			[Option('o', "output", HelpText = "Single output path. Requires -s.")]
			public string Output { get; set; }

			[HelpOption('h', "help", HelpText = "Display this screen.")]
			public string GetUsage()
			{



				return string.Format("Usage: {0} [[url path],]\n{1}", System.AppDomain.CurrentDomain.FriendlyName, 
					HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current)).ToString());
			}
		}
	}
}

