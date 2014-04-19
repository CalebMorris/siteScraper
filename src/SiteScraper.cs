using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace SiteScraper
{
	public class SiteScraper
	{
		public SiteScraper(string[] args)
		{
			m_usage = string.Format("Usage: {0} [[url path],]",System.AppDomain.CurrentDomain.FriendlyName);
			if (args.Length == 0 || args.Length % 2 == 1)
			{
				System.Console.Error.WriteLine(m_usage);
				Environment.Exit(-1);
			}
			m_args = args;
			m_urlQueue = new ConcurrentQueue<Tuple<string,string>>();
			for (int i = 0; i < args.Length; i+=2)
				m_urlQueue.Enqueue(new Tuple<string,string>(args[i],args[i+1]));
		}

		public void Scrape()
		{
			System.Console.WriteLine("pwd:{0}", Directory.GetCurrentDirectory());
			Tuple<string, string> urlPathTuple;
			while (!m_urlQueue.IsEmpty)
			{
				m_urlQueue.TryDequeue(out urlPathTuple);
				if (!Directory.Exists(urlPathTuple.Item2))
				{
					System.Console.Error.WriteLine("The following path doesn't exist: {0}", urlPathTuple.Item2);
					System.IO.Directory.CreateDirectory(urlPathTuple.Item2);
				}
				try
				{
					Uri uri = null;
					if (!urlPathTuple.Item1.StartsWith("http"))
						uri = new Uri(string.Format("http://{0}",urlPathTuple.Item1));
					else
						uri = new Uri(urlPathTuple.Item1);

					if (uri.Scheme != Uri.UriSchemeHttp)
						throw new UriFormatException("Scheme Not Supported: uri is not of the HTTP scheme.");

					string siteDirectory = Path.Combine(urlPathTuple.Item2, uri.Host.Split('.').Reverse().Skip(1).First());
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
						urlPathTuple.Item1,
						siteDirectory);
				}
				catch (UriFormatException e)
				{
					System.Console.Error.WriteLine(e.Message);
					Environment.Exit(-1);
				}
			}
		}

		string[] m_args;
		ConcurrentQueue<Tuple<string, string>> m_urlQueue;
		readonly string m_usage;
	}
}

