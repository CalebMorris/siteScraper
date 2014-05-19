using Gtk;
using LibSiteScraper;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SiteScraper
{
	class ScrapeViewModel
	{
		public ScrapeViewModel()
		{
			m_exploredLinks = new ListStore(typeof(string));
			m_queue = new ConcurrentQueue<ScrapePair>();
			m_tokenSource = new CancellationTokenSource();
			m_exploredLinks.AppendValues("Test1");
			m_exploredLinks.AppendValues("Test2");
		}

		public bool StartScraping(string crawlUrl)
		{
			Uri url;

			m_tokenSource.Dispose();
			m_tokenSource = new CancellationTokenSource();
			m_isUrlWellFormed = Uri.TryCreate(crawlUrl, UriKind.Absolute, out url);

			if (m_isUrlWellFormed)
			{
				m_isProcessing = true;

				Queue.Enqueue(new ScrapePair(url, null));
				DoWork();
			}

			return m_isUrlWellFormed;
		}

		public void Cancel()
		{
			m_isProcessing = false;
			m_tokenSource.Cancel();
		}

		async void DoWork()
		{
			await LibSiteScraper.SiteScraper.Start(m_queue, ProcessingExploredLink, false, m_tokenSource.Token);
		}

		void ProcessingExploredLink(string newLink)
		{
			m_exploredLinks.AppendValues(newLink);
		}

		public bool IsUrlWellFormed
		{
			get { return m_isUrlWellFormed; }
			set { m_isUrlWellFormed = value; }
		}

		public ConcurrentQueue<ScrapePair> Queue
		{
			get { return m_queue; }
		}

		public ListStore ExploredLinks
		{
			get { return m_exploredLinks; }
		}

		public bool IsProcessing
		{
			get { return m_isProcessing; }
			set { m_isProcessing = value; }
		}

		readonly ListStore m_exploredLinks;
		ConcurrentQueue<ScrapePair> m_queue;
		CancellationTokenSource m_tokenSource;
		bool m_isUrlWellFormed;
		bool m_isProcessing;
	}
}
