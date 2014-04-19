using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace SiteScraper
{
	public static class HtmlAgilityPackUtility
	{
		public static IEnumerable<HtmlNode> EmptyOrNotNull(this HtmlNodeCollection collection)
		{
			return collection ?? Enumerable.Empty<HtmlNode>();
		}
	}
}

