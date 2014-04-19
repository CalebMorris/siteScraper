using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace SiteScraper
{
	public class SiteScraper
	{
		public SiteScraper(string[] args)
		{
			Options options = new Options();
			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				if (options.Scrape)
				{
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
				for (int i = 0; i < options.Urls.Length; i += 2)
					m_urlQueue.Enqueue(new ScrapePair(options.Urls[i], options.Output != null ? options.Output : options.Paths[i]));
			}
			else
			{
				Environment.Exit(-1);
			}
		}

		public void Scrape()
		{
			System.Console.WriteLine("pwd:{0}", Directory.GetCurrentDirectory());
			ScrapePair urlPathTuple;
			while (!m_urlQueue.IsEmpty)
			{
				m_urlQueue.TryDequeue(out urlPathTuple);
				if (!Directory.Exists(urlPathTuple.Path))
				{
					System.Console.Error.WriteLine("The following path doesn't exist: {0}", urlPathTuple.Path);
					System.IO.Directory.CreateDirectory(urlPathTuple.Path);
				}
				try
				{
					Uri uri = null;
					if (!urlPathTuple.Url.StartsWith("http"))
						uri = new Uri(string.Format("http://{0}", urlPathTuple.Url));
					else
						uri = new Uri(urlPathTuple.Url);

					if (uri.Scheme != Uri.UriSchemeHttp)
						throw new UriFormatException("Scheme Not Supported: uri is not of the HTTP scheme.");

					string siteDirectory = Path.Combine(urlPathTuple.Path, uri.Host.Split('.').Reverse().Skip(1).First());
					if (!SiteScraperUtility.FileOrDirectoryExists(siteDirectory))
						Directory.CreateDirectory(siteDirectory);
					else
					{
						int i = 2;
						while (SiteScraperUtility.FileOrDirectoryExists(string.Format("{0}({1})", siteDirectory, i)))
							i++;
						siteDirectory = string.Format("{0}({1})", siteDirectory, i);
						Directory.CreateDirectory(siteDirectory);
					}
					SiteScraperUtility.Scrape(
						urlPathTuple.Url,
						siteDirectory);
				}
				catch (UriFormatException e)
				{
					System.Console.Error.WriteLine(e.Message);
					Environment.Exit(-1);
				}
			}
		}

		ConcurrentQueue<ScrapePair> m_urlQueue;

		sealed class ScrapePair
		{
			public ScrapePair(string url, string path)
			{
				m_url = url;
				m_path = path;
			}

			public string Url { get { return m_url; } }
			public string Path { get { return m_path; } }

			string m_url;
			string m_path;
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

