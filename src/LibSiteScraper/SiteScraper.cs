using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LibSiteScraper
{
	public sealed class SiteScraper
	{
		public SiteScraper(ScrapePair scrapePair, bool isScraping)
		{
			m_scrapePair = scrapePair;
			m_isScraping = isScraping;
			m_directoryTree = new DirectoryTree();
		}

		public async Task Scrape(Action<string> onNewLinkFound, CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;

			DirectoryTreeNode node = null;
			string scrapeFolder = m_isScraping
				? (new Uri(Path.Combine(m_scrapePair.Path.AbsolutePath, m_scrapePair.Url.Authority))).AbsolutePath
				: (new Uri(Path.Combine(Path.GetTempPath(), AppDomain.CurrentDomain.FriendlyName, Path.GetRandomFileName()))).AbsolutePath;
			string basePath;
			if (!FileOrDirectoryExists(scrapeFolder))
			{
				basePath = scrapeFolder;
			}
			else
			{
				int duplicateCount = 1;
				basePath = scrapeFolder + "(" + duplicateCount.ToString() + ")";
				while (FileOrDirectoryExists(basePath))
					basePath = scrapeFolder + "(" + (++duplicateCount).ToString() + ")";
			}

			Console.CancelKeyPress += delegate { CleanUpDirectoryIfNecessary(basePath); };

			Directory.CreateDirectory(basePath);

			try
			{
				Uri root = null;
				if (m_scrapePair.Url.AbsolutePath == "/")
				{
					root = new Uri(Path.Combine(basePath, "base"));
					if (File.Exists(root.AbsolutePath))
					{
						Console.Error.WriteLine("File '{0}' already exists.", root.AbsolutePath);
						return;
					}
					else
					{
						node = await GetUrl(m_scrapePair.Url, root.AbsolutePath);
					}
				}

				onNewLinkFound(node.Path.AbsoluteUri);
				DirectoryTreeNode directoryTreeNode = m_directoryTree.AddLink(null, node.Path.AbsoluteUri, node.Status);

				if (root == null)
					return;
				List<string> resources = GetLinks(root, 0);

				foreach (string resource in resources)
				{
					if (resource.First() == '/')
					{
						Uri nextUrl;
						string urlInput = string.Format("{0}{1}{2}{3}", m_scrapePair.Url.Scheme, Uri.SchemeDelimiter, m_scrapePair.Url.Authority, resource);
						if (Uri.TryCreate(urlInput, UriKind.Absolute, out nextUrl))
						{
							await Scrape(onNewLinkFound, token, directoryTreeNode, nextUrl, basePath);
						}
						else
						{
							Console.Error.WriteLine("Link '{0}' was of incorrect form.", urlInput);
							continue;
						}
					}
					else
					{
						//File that is an outside resource
					}
				}
			}
			catch (WebException ex)
			{
				Console.WriteLine(ex.Message);
				throw;
			}
			finally
			{
				CleanUpDirectoryIfNecessary(basePath);
			}
		}

		public static async Task Start(ConcurrentQueue<ScrapePair> crawlQueue, Action<string> onNewLinkFound, bool isScraping, CancellationToken token)
		{
			Console.WriteLine("pwd:{0}", Directory.GetCurrentDirectory());
			while (!crawlQueue.IsEmpty)
			{
				ScrapePair scrapePair;
				if (crawlQueue.TryDequeue(out scrapePair))
				{
					if (isScraping && !Directory.Exists(scrapePair.Path.AbsolutePath))
					{
						Console.Error.WriteLine("The following path doesn't exist: {0}", scrapePair.Path);
						Directory.CreateDirectory(scrapePair.Path.AbsolutePath);
					}

					// TODO(cm): Add support of other schemes (Issue ID: 1)
					if (scrapePair.Url.Scheme != Uri.UriSchemeHttp)
					{
						Console.Error.WriteLine("Scheme Not Supported: uri '{0}' is not of the HTTP scheme.", scrapePair.Url.AbsoluteUri);
						continue;
					}

					SiteScraper scraper = new SiteScraper(scrapePair, isScraping);
					await scraper.Scrape(onNewLinkFound, token);
				}
				else
				{
					Console.Error.WriteLine("Unable to dequeue the next site.");
					Environment.Exit(-1);
				}
			}
		}


		async Task Scrape(Action<string> onNewLinkFound, CancellationToken token, DirectoryTreeNode current, Uri url, string path)
		{
			if (token.IsCancellationRequested)
				return;

			try
			{
				int depth;
				string dataPath;
				DirectoryTreeNode node = null;
				DirectoryTreeNode directoryTreeNode = null;
				if (url.AbsolutePath != "/")
				{
					string[] directory = url.AbsolutePath.Split('/').Where(x => x != "").ToArray();
					depth = directory.Length;
					dataPath = "";
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
						node = await GetUrl(url, dataPath);
					}

					onNewLinkFound(node.Path.AbsoluteUri);
					directoryTreeNode = m_directoryTree.AddLink(current, node.Path.AbsoluteUri, node.Status);
				}
				else
				{
					Console.Error.WriteLine("Attempting to retrieve root again.");
					return;
				}

				Uri data;
				if (!Uri.TryCreate(dataPath, UriKind.RelativeOrAbsolute, out data))
				{
					Console.Error.WriteLine("The path '{0}' is not well formed.", dataPath);
					return;
				}

				List<string> resources = GetLinks(data, depth);

				foreach (string resource in resources)
				{
					if (!string.IsNullOrEmpty(resource) && resource.First() == '/')
					{
						Uri nextUrl;
						string urlInput = string.Format("{0}{1}{2}{3}", url.Scheme, Uri.SchemeDelimiter, url.Authority, resource);
						if (Uri.TryCreate(urlInput, UriKind.Absolute, out nextUrl))
						{
							await Scrape(onNewLinkFound, token, current, nextUrl, path);
						}
						else
						{
							Console.Error.WriteLine("Link '{0}' was of incorrect form.", urlInput);
							continue;
						}
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

		async Task<DirectoryTreeNode> GetUrl(Uri url, string path)
		{
			HttpStatusCode status;
			HttpWebResponse response;
			try
			{
				HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
				request.UserAgent = "TurtleSpider/0.1";
				using (response = await request.GetResponseAsync() as HttpWebResponse)
				{
					status = response.StatusCode;
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
					}
				}
			}
			catch (WebException ex)
			{
				response = ex.Response as HttpWebResponse;
				status = response.StatusCode;
				if (response.StatusCode == HttpStatusCode.NotFound)
					Console.Error.WriteLine("Link '{0}' not found.", url.AbsoluteUri);
				else
					throw ex;
			}
			return new DirectoryTreeNode(url, status);
		}

		List<string> GetLinks(Uri urlData, int depth)
		{
			HtmlDocument doc = new HtmlDocument();
			List<string> resources = new List<string>();
			try
			{
				doc.Load(urlData.AbsolutePath);
				if (doc.ParseErrors == null || !doc.ParseErrors.Any())
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
							if (att.Value.Length > 2 && att.Value.StartsWith("//"))
								continue;
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
						doc.Save(urlData.AbsolutePath);
					}
				}
			}
			catch (IOException ex)
			{
				Console.WriteLine("'{0}' threw an error {1}", urlData.AbsoluteUri, ex.Message);
			}
			return resources;
		}

		public static bool FileOrDirectoryExists(string name)
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

		void CleanUpDirectoryIfNecessary(string path)
		{
			if (IsScraping || !FileOrDirectoryExists(path))
				return;

			Console.WriteLine("Cleanup up directory '{0}'", path);
			DirectoryInfo directory = new DirectoryInfo(path);

			foreach (FileInfo file in directory.GetFiles())
				file.Delete();

			foreach (DirectoryInfo subDirectory in directory.GetDirectories())
				subDirectory.Delete(true);

			directory.Delete();
		}

		public bool IsScraping { get { return m_isScraping; } }

		public const string c_noExtensionFile = ".html";
		readonly bool m_isScraping;
		readonly ScrapePair m_scrapePair;
		readonly DirectoryTree m_directoryTree;
	}
}

