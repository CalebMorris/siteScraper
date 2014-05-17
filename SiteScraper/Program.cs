using System;
using Gtk;

namespace SiteScraper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.Init();
			MainWindow win = new MainWindow();
			Application.Run();
		}
	}
}
