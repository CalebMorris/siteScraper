//
//  SiteScraper.cs
//
//  Author:
//       Caleb Morris <caleb.morris.g@gmail.com>
//
//  Copyright (c) 2013 Caleb Morris
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
					Environment.Exit(-1);
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

