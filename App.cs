using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using NovaUpdater;

public class App : Application
{
	private bool _contentLoaded;

	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			Uri resourceLocator = new Uri("/NovaUpdater;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
