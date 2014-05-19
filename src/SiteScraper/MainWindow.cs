using System;
using Gtk;
using System.Collections.Concurrent;
using SiteScraper;
using LibSiteScraper;
using System.Threading;

public sealed class MainWindow: Gtk.Window
{
	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		m_scrapeViewModel = new ScrapeViewModel();

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

			Notebook notebook = new Notebook();
			{
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

						treeview.Model = m_scrapeViewModel.ExploredLinks;

						scrolledWindow.Add(treeview);
					}

					notebook.AppendPage(scrolledWindow, new Label("Explored Link"));
				}

				vbox.Add(notebook);
			}

			this.Add(vbox);
		}

		this.DeleteEvent += OnDeleteEvent;

		this.ShowAll();
	}

	void OnUrlEntryChanged(object sender, EventArgs e)
	{
		if (!m_scrapeViewModel.IsUrlWellFormed)
		{
			m_urlEntry.ModifyText(StateType.Normal, s_normalUrlColor);
			m_urlEntry.TooltipText = string.Empty;
		}
	}

	void OnStartButtonClicked(object sender, EventArgs e)
	{
		if (!m_scrapeViewModel.IsProcessing)
		{
			m_scrapeViewModel.ExploredLinks.Clear();
			if (m_scrapeViewModel.StartScraping(m_urlEntry.Text))
			{
				m_urlEntry.Sensitive = false;
				m_startButton.Label = c_cancelButtonText;
			}
			else
			{
				m_urlEntry.ModifyText(StateType.Normal, s_incorrectUrlColor);
				m_urlEntry.TooltipText = c_urlErrorTooltipText;
			}
		}
		else
		{
			m_urlEntry.Sensitive = true;
			m_startButton.Label = c_startButtonText;
			m_scrapeViewModel.Cancel();
		}
	}

	void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	const string c_cancelButtonText = "Cancel";
	const string c_startButtonText = "Crawl";
	const string c_urlErrorTooltipText = "Incorrect Url format. Please try Again.";

	static Gdk.Color s_incorrectUrlColor = new Gdk.Color(255, 0, 0);
	static Gdk.Color s_normalUrlColor = new Gdk.Color(0, 0, 0);

	ScrapeViewModel m_scrapeViewModel;

	Entry m_urlEntry;
	Button m_startButton;
}
