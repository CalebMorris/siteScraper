using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiteScraper
{
	public sealed class ScrapePair
	{
		public ScrapePair(Uri url, Uri path)
		{
			m_url = url;
			m_path = path;
		}

		public Uri Url { get { return m_url; } }
		public Uri Path { get { return m_path; } }

		readonly Uri m_url;
		readonly Uri m_path;
	}
}
