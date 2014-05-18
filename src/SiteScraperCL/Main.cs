using System;
using System.Collections.Concurrent;
using CommandLine;
using CommandLine.Text;
using LibSiteScraper;

namespace SiteScraperCL
{
	public class MainClass
	{
		public static void Main(string[] args)
		{
			ConcurrentQueue<ScrapePair> crawlQueue = new ConcurrentQueue<ScrapePair>();
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
				if (options.Output != null)
				{
					Uri output;

					if (Uri.TryCreate(options.Output, UriKind.Absolute, out output))
					{
						for (int i = 0; i < options.Urls.Length; ++i)
						{
							Uri url;
							if (Uri.TryCreate(options.Urls[i], UriKind.Absolute, out url))
								crawlQueue.Enqueue(new ScrapePair(url, output));
							else
								Console.Error.WriteLine("Your url '{0}' was of incorrect form.", options.Urls[i]);
						}
					}
					else
					{
						Console.Error.WriteLine("Your output path '{0}' was of incorrect form.", options.Output);
						Environment.Exit(-1);
					}
				}
				else if (options.Paths != null)
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
						crawlQueue.Enqueue(new ScrapePair(url, path));
					}
				}
				else
				{
					for (int i = 0; i < options.Urls.Length; ++i)
					{
						Uri url;
						if (!Uri.TryCreate(options.Urls[i], UriKind.Absolute, out url))
						{
							Console.Error.WriteLine("Your url '{0}' was of incorrect form.", options.Urls[i]);
							continue;
						}
						crawlQueue.Enqueue(new ScrapePair(url, null));
					}
				}
			}
			else
			{
				Environment.Exit(-1);
			}

			SiteScraper.Start(crawlQueue, options.Scrape);
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
