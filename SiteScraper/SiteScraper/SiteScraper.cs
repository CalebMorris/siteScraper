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

namespace SiteScraper
{
	public class SiteScraper
	{
		public SiteScraper(string[] args)
		{
			m_usage = string.Format("Usage: {0} [url,]",System.AppDomain.CurrentDomain.FriendlyName);
			if (args.Length == 0)
			{
				System.Console.Error.WriteLine(m_usage);
				Environment.Exit(-1);
			}
			m_args = args;
			m_urlQueue = new ConcurrentQueue<string>(args);
		}

		public void Scrape()
		{
			string url;
			while (!m_urlQueue.IsEmpty)
			{
				m_urlQueue.TryDequeue(out url);
				SiteScraperUtility.Scrape(url, "");
			}
		}

		string[] m_args;
		ConcurrentQueue<string> m_urlQueue;
		readonly string m_usage;
	}
}

