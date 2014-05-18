using System;
using Gtk;
using System.Collections.Concurrent;
using SiteScraper;
using LibSiteScraper;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public sealed class MainWindow: Gtk.Window
{
	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		m_queue = new ConcurrentQueue<ScrapePair>();
		m_tokenSource = new CancellationTokenSource();

		VBox vbox = new VBox();
		{
			HBox hbox = new HBox();
			{
				m_urlEntry = new Entry("http://www.google.com");
				{
					m_urlEntry.Changed += OnUrlEntryChanged;

					hbox.PackStart(m_urlEntry, true, true, 4);
				}

				m_startButton = new Button(c_startButtonText);
				{
					m_startButton.SetSizeRequest(60, 16);
					m_startButton.TooltipText = "Start crawling the site.";

					m_startButton.Clicked += OnStartButtonClicked;

					hbox.PackEnd(m_startButton, false, true, 4);
				}

				vbox.PackStart(hbox, false, true, 8);
			}

			ScrolledWindow scrolledWindow = new ScrolledWindow();
			{
				TreeView treeview = new TreeView();
				{
					treeview.SetSizeRequest(200, 200);
					treeview.HeadersVisible = false;

					TreeViewColumn colOne = new TreeViewColumn();
					{
						CellRendererText cellRendererText = new CellRendererText();
						{
							colOne.PackStart(cellRendererText, true);
							colOne.AddAttribute(cellRendererText, "text", 0);
						}

						treeview.AppendColumn(colOne);
					}

					m_listStore = new ListStore(typeof(string));
					{
						m_listStore.AppendValues("test1");
						m_listStore.AppendValues("test2");

						treeview.Model = m_listStore;
					}

					scrolledWindow.Add(treeview);
				}

				vbox.Add(scrolledWindow);
			}

			this.Add(vbox);
		}

		this.DeleteEvent += OnDeleteEvent;

		this.ShowAll();
	}

	void OnUrlEntryChanged(object sender, EventArgs e)
	{
		if (m_isUrlIncorrect)
		{
			m_urlEntry.ModifyText(StateType.Normal, s_normalUrlColor);
			m_urlEntry.TooltipText = string.Empty;
			m_isUrlIncorrect = false;
		}
	}

	void OnStartButtonClicked(object sender, EventArgs e)
	{
		if (!m_isProcessing)
		{
			m_tokenSource.Dispose();
			m_tokenSource = new CancellationTokenSource();
			m_listStore.Clear();
			Uri url;
			if (Uri.TryCreate(m_urlEntry.Text, UriKind.Absolute, out url))
			{
				m_urlEntry.Sensitive = false;
				m_startButton.Label = c_cancelButtonText;
				m_isProcessing = true;

				m_queue.Enqueue(new ScrapePair(url, null));
				DoWork();
			}
			else
			{
				m_urlEntry.ModifyText(StateType.Normal, s_incorrectUrlColor);
				m_urlEntry.TooltipText = c_urlErrorTooltipText;
				m_isUrlIncorrect = true;
			}
		}
		else
		{
			m_urlEntry.Sensitive = true;
			m_startButton.Label = c_startButtonText;
			m_isProcessing = false;
			m_tokenSource.Cancel();
		}
	}

	void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	void ProcessingExploredLink(int newLink)
	{
		m_listStore.AppendValues(newLink.ToString());
	}

	async void DoWork()
	{
		await testFn(ProcessingExploredLink, m_tokenSource.Token);
		/*
		Console.WriteLine("Callback Triggered");

		SynchronizationContext syncContext = SynchronizationContext.Current;
		if (syncContext != null)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				////LibSiteScraper.SiteScraper.Start(m_queue, false);
				syncContext.Post(delegate
				{
					
				}, null);
			});
		}
		else
		{
			Console.WriteLine("Context was null");
			throw new NotImplementedException();
		}
		*/
	}

	static async Task testFn(Action<int> onItemCompletion, CancellationToken token)
	{
		await Task.Delay(2000);
		if (token.IsCancellationRequested)
			return;
		onItemCompletion(0);
		await Task.Delay(2000);
		if (token.IsCancellationRequested)
			return;
		for (int i = 1; i < 10000; ++i)
		{
			if (token.IsCancellationRequested)
				return;
			onItemCompletion(i);
		}
	}

	const string c_cancelButtonText = "Cancel";
	const string c_startButtonText = "Crawl";
	const string c_urlErrorTooltipText = "Incorrect Url format. Please try Again.";

	static Gdk.Color s_incorrectUrlColor = new Gdk.Color(255, 0, 0);
	static Gdk.Color s_normalUrlColor = new Gdk.Color(0, 0, 0);

	ConcurrentQueue<ScrapePair> m_queue;
	CancellationTokenSource m_tokenSource;
	ListStore m_listStore;
	Entry m_urlEntry;
	Button m_startButton;

	bool m_isProcessing;
	bool m_isUrlIncorrect;
}
