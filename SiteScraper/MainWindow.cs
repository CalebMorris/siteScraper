using System;
using Gtk;

public sealed class MainWindow: Gtk.Window
{
	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		VBox vbox = new VBox();
		{
			HBox hbox = new HBox();
			{
				m_urlEntry = new Entry("http://www.google.com");
				{
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

				ListStore tmpStore = new ListStore(typeof(string));
				{
					tmpStore.AppendValues("test1");
					tmpStore.AppendValues("test2");

					treeview.Model = tmpStore;
				}

				vbox.Add(treeview);
			}

			this.Add(vbox);
		}

		this.DeleteEvent += OnDeleteEvent;

		this.ShowAll();
	}

	void OnStartButtonClicked(object sender, EventArgs e)
	{
		if (!m_isProcessing)
		{
			m_urlEntry.Sensitive = false;
			m_startButton.Label = c_cancelButtonText;
			m_isProcessing = true;
		}
		else
		{
			m_urlEntry.Sensitive = true;
			m_startButton.Label = c_startButtonText;
			m_isProcessing = false;
		}
	}

	void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	const string c_cancelButtonText = "Cancel";
	const string c_startButtonText = "Crawl";

	bool m_isProcessing;

	Entry m_urlEntry;
	Button m_startButton;
}
