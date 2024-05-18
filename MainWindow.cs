using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using RestSharp;

public class MainWindow : Window, IComponentConnector
{
	internal ProgressBar progressBar;

	internal TextBlock Progresstxt;

	internal TextBlock TitleTxt;

	private bool _contentLoaded;

	public MainWindow()
	{
		InitializeComponent();
	}

	public static async Task CloseNovaLauncherProcessesAsync()
	{
		string processName = "NovaLauncher";
		Process[] processes = Process.GetProcessesByName(processName);
		Process[] array = processes;
		foreach (Process process in array)
		{
			process.CloseMainWindow();
			await Task.Delay(1000);
			if (!process.HasExited)
			{
				process.Kill();
			}
		}
	}

	private async Task ManageLauncherAsync(TextBlock statusTitle)
	{
		await CloseNovaLauncherProcessesAsync();
		await Task.Delay(1000);
		string[] args = Environment.GetCommandLineArgs();
		if (args.Contains("-uninstall"))
		{
			TitleTxt.Dispatcher.Invoke(delegate
			{
				TitleTxt.Text = "Uninstalling Launcher...";
			});
			string rootFolderPath_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova");
			string launcherFolderPath_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova", "Launcher");
			string presidioFolderPath_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova", "Presidio");
			string novaLogFolderPath_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova", "Logs");
			string novaFolderPath_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nova");
			string[] files = Directory.GetFiles(rootFolderPath_);
			foreach (string file in files)
			{
				if (!file.Contains("NovaUpdater"))
				{
					try
					{
						File.Delete(file);
					}
					catch
					{
					}
				}
			}
			try
			{
				if (Directory.Exists(presidioFolderPath_))
				{
					Directory.Delete(presidioFolderPath_, recursive: true);
				}
			}
			catch
			{
			}
			try
			{
				if (Directory.Exists(launcherFolderPath_))
				{
					Directory.Delete(launcherFolderPath_, recursive: true);
				}
			}
			catch
			{
			}
			try
			{
				if (Directory.Exists(novaFolderPath_))
				{
					Directory.Delete(novaFolderPath_, recursive: true);
				}
			}
			catch
			{
			}
			try
			{
				if (Directory.Exists(novaLogFolderPath_))
				{
					Directory.Delete(novaLogFolderPath_, recursive: true);
				}
			}
			catch
			{
			}
			Environment.Exit(0);
		}
		else
		{
			await Task.Delay(3000);
			string launcherFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova", "Launcher");
			if (Directory.Exists(launcherFolderPath))
			{
				StopProcessesUsingFolder(launcherFolderPath);
				ForceDeleteFolder(launcherFolderPath);
			}
			await DownloadAndExtractLauncherAsync(await GetAPIInfo(999, statusTitle), statusTitle);
		}
	}

	private void StopProcessesUsingFolder(string folderPath)
	{
		string value = Path.Combine(folderPath, "NovaLauncher.exe");
		Process[] processesByName = Process.GetProcessesByName("NovaLauncher");
		foreach (Process process in processesByName)
		{
			process.Kill();
		}
		Process[] processes = Process.GetProcesses();
		foreach (Process process2 in processes)
		{
			try
			{
				if (process2.MainModule.FileName.Equals(value, StringComparison.OrdinalIgnoreCase))
				{
					process2.Kill();
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private void ForceDeleteFolder(string folderPath)
	{
		try
		{
			Directory.Delete(folderPath, recursive: true);
		}
		catch (Exception)
		{
		}
	}

	private async Task<JObject> GetAPIInfo(int maxRetries, TextBlock errorTextBlock)
	{
		TextBlock errorTextBlock2 = errorTextBlock;
		errorTextBlock2.Dispatcher.Invoke(delegate
		{
			errorTextBlock2.Text = "Contacting Services...";
		});
		RestClient client = new RestClient("http://apiv2.projectnovafn.com:80");
		RestRequest request = new RestRequest("/api/launcher-info")
		{
			Timeout = 1000
		};
		int retryCount = 0;
		while (retryCount < maxRetries)
		{
			RestResponse response = await client.ExecuteAsync(request);
			if (response.StatusCode == HttpStatusCode.OK)
			{
				string content = response.Content;
				return JObject.Parse(content);
			}
			retryCount++;
			int i;
			for (i = 10; i > 0; i--)
			{
				errorTextBlock2.Dispatcher.Invoke(delegate
				{
					errorTextBlock2.Text = $"Connection failed.{Environment.NewLine}Retrying in {i} seconds...";
				});
				await Task.Delay(1000);
			}
		}
		errorTextBlock2.Dispatcher.Invoke(delegate
		{
			errorTextBlock2.Text = "Failed to connect to the server.";
		});
		return null;
	}

	private async Task DownloadAndExtractLauncherAsync(JObject launcherInfo, TextBlock statusTitle)
	{
		try
		{
			statusTitle.Text = "Updating Launcher...";
			string launcherUrl = launcherInfo["Launcher"].ToString();
			string tempFolderPath = Path.GetTempPath();
			string tempZipFilePath = Path.Combine(tempFolderPath, "Launcher.zip");
			string extractFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nova");
			WebClient client = new WebClient();
			try
			{
				client.DownloadProgressChanged += Client_DownloadProgressChanged;
				await client.DownloadFileTaskAsync(new Uri(launcherUrl), tempZipFilePath);
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
			ZipFile.ExtractToDirectory(tempZipFilePath, extractFolderPath);
			await Task.Delay(500);
			if (File.Exists(extractFolderPath + "\\Launcher\\NovaLauncher.exe"))
			{
				ProcessStartInfo startInfo = new ProcessStartInfo
				{
					FileName = extractFolderPath + "\\Launcher\\NovaLauncher.exe",
					UseShellExecute = true
				};
				try
				{
					Process.Start(startInfo);
				}
				catch (Win32Exception)
				{
					MessageBox.Show("The Nova Launcher needs to be ran as admin.");
				}
			}
			Environment.Exit(0);
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			statusTitle.Text = "An error occurred: " + ex.Message;
		}
	}

	private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
	{
		DownloadProgressChangedEventArgs e2 = e;
		progressBar.Dispatcher.Invoke(delegate
		{
			progressBar.Value = e2.ProgressPercentage;
		});
		Progresstxt.Dispatcher.Invoke(delegate
		{
			Progresstxt.Text = $"{e2.ProgressPercentage}%";
		});
	}

	private void TitleTxt_Loaded(object sender, RoutedEventArgs e)
	{
		ManageLauncherAsync(TitleTxt);
	}

	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/NovaUpdater;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			progressBar = (ProgressBar)target;
			break;
		case 2:
			Progresstxt = (TextBlock)target;
			break;
		case 3:
			TitleTxt = (TextBlock)target;
			TitleTxt.Loaded += TitleTxt_Loaded;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
