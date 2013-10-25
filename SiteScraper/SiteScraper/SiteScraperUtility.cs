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
using System.Collections;
using System.Net;
using System.Linq;
using HtmlAgilityPack;

namespace SiteScraper
{
	public class SiteScraperUtility
	{
		public static void Scrape(string url, string path)
		{
			System.Console.WriteLine("Scraping url:{0}\n\tto Path:{1}", url, path);
			string data = GetUrl(url);
			GetLinks(data);
		}

		static string GetUrl(string url)
		{
			using(WebClient client = new WebClient())
			{
				return client.DownloadString(url);
			}
		}

		static string[] GetLinks(string urlData)
		{
			HtmlDocument doc = new HtmlDocument();

			doc.LoadHtml(urlData);
			if (doc.ParseErrors == null || doc.ParseErrors.Count() == 0)
			{
				if (doc.DocumentNode != null)
				{
					foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//link[@href]"))
					{
						HtmlAttribute att = link.Attributes["href"];
						System.Console.WriteLine(att.Value);
					}
					System.Console.WriteLine();
					foreach (HtmlNode imgLink in doc.DocumentNode.SelectNodes("//img[@src]"))
					{
						HtmlAttribute att = imgLink.Attributes["src"];
						System.Console.WriteLine(att.Value);
					}
					System.Console.WriteLine();
					foreach (HtmlNode hyperLink in doc.DocumentNode.SelectNodes("//a[@href]"))
					{
						HtmlAttribute att = hyperLink.Attributes["href"];
						System.Console.WriteLine(att.Value);
					}
				}
			}

			return null;
		}
	}
}

