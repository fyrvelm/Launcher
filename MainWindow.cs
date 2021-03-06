﻿using System;
using Gtk;
using System.Net;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using Ionic.Zip;

public partial class MainWindow: Gtk.Window
{	
	public static String LauncherVersion = "0.00";
	private bool firstRun = true;
	private bool startThread = false;
	private bool updateGame = false;
	private bool updateLauncher = false;
	private bool letInstall = false;
	private bool letLaunch = false;
	private Thread thread;
	private Thread time;
	private WebClient wc;
	private int updateProgress = 0;
	private List<string> installed = new List<string>();
	private List<Double> versions = new List<Double>();
	private string[] applications = new string[5];
    private PlatformID pid = Environment.OSVersion.Platform;
    private bool Windows = false;
    private bool processActive;
    private string WorkingDirectory;
	
	private string website = "http://www.nada.kth.se/~csundlof/";
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
       
		//Log.text
		//System.Threading.Thread thread = new System.Threading.Thread(CheckLauncherVersion(main, Log));
		Build ();
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
		LaunchButton.Sensitive = false;
		SelectedGame.Sensitive = false;
		applications[0] = "Game1";
		applications[1] = "test";
        DetectOS();
		DetectInstalledApplications();
		if(!installed.Contains(SelectedGame.ActiveText))
		{
			LaunchButton.Label = "Install " + SelectedGame.ActiveText;
			letInstall = true;
		}
		else
		{
			LaunchButton.Label = "Launch " + SelectedGame.ActiveText;
			letLaunch = true;
		}
		if(firstRun){
			thread = new Thread(CheckLauncherVersion);
			thread.IsBackground = true;
			DetectLauncherVersion();
			Log.Buffer.Text = "Current version of the Launcher is " + LauncherVersion + ".\n" + Log.Buffer.Text;
	    	DownloadProgress.Text = (DownloadProgress.Fraction * 100).ToString() + "%";
			thread.Start();
			firstRun = false;
		}
		LaunchButton.Clicked += new EventHandler(LaunchButtonClicked);
		time = new Thread(timer);
		time.IsBackground =  true;
		//time.Start();
	}
	
	private void timer(){
		while(true){
			Gtk.Application.Invoke (delegate {
			Timer.Text = Convert.ToInt32(Timer.Text) + 1 + "";
			});
			Thread.Sleep(1000);
		}
	}
	#region launcher update
	private void CheckLauncherVersion(){
		wc = new WebClient();
		string url = website + "version.txt"; 
		Gtk.Application.Invoke (delegate {
		Log.Buffer.Text = "Checking for updates to the Launcher..\n" + Log.Buffer.Text;
		DownloadProgress.Fraction = 0.50;
	    DownloadProgress.Text = (DownloadProgress.Fraction * 100).ToString() + "%";
		});
		try{
			byte[] content = wc.DownloadData(url);
			string version = System.Text.Encoding.Default.GetString(content);
			if(Convert.ToDouble(version, System.Globalization.NumberFormatInfo.InvariantInfo)>Convert.ToDouble(LauncherVersion, System.Globalization.NumberFormatInfo.InvariantInfo)){
				Gtk.Application.Invoke (delegate {
	    			DownloadProgress.Text = "Found a new version of the Launcher(v" + version + ")";
					Log.Buffer.Text = "A new version of the Launcher was found(v" + version + ")!\n" + Log.Buffer.Text;
				});
				DownloadLauncher(version);
			}
			if(Convert.ToDouble(version, System.Globalization.NumberFormatInfo.InvariantInfo)==Convert.ToDouble(LauncherVersion, System.Globalization.NumberFormatInfo.InvariantInfo)){
				Gtk.Application.Invoke (delegate {
					DownloadProgress.Fraction = 1.00;
					DownloadProgress.Text = "Your Launcher is up to date";
					Log.Buffer.Text = "Your Launcher is up to date\n" + Log.Buffer.Text;
				});
				Thread.Sleep(1000);
				if(installed.Contains(SelectedGame.ActiveText))
					CheckGameVersion();
				else{
					letInstall = true;
					LaunchButton.Sensitive = true;
				}
			}
		}
		catch{
			Gtk.Application.Invoke (delegate {
			Log.Buffer.Text = "Could not check for the latest version online, are you connected to the internet?\n" + Log.Buffer.Text;
			DownloadProgress.Text = "An error occurred.";
			DownloadProgress.Fraction = 1.00;
			});
		}
	}
	
	private void DownloadLauncher(string version){
	    String url = website + version.Replace("\n", "") + ".zip";
		Uri uri = new Uri(url);
		Gtk.Application.Invoke (delegate {
			Log.Buffer.Text = "Downloading new version of the Launcher..\n" + Log.Buffer.Text;
		});
        wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
		updateLauncher = true;
        string zip = "new.zip";
        if (Windows)
            zip = WorkingDirectory + "\\" + zip;
        else
            zip = "./" + SelectedGame.ActiveText + "/" + zip;
		wc.DownloadFileAsync(uri, zip);
	}
	
	#endregion
	#region game update
	private void CheckGameVersion()
	{
		string url = website + SelectedGame.ActiveText + "/version.txt";
		int vIndex = 0;
		if(!letInstall){
			vIndex = installed.IndexOf(SelectedGame.ActiveText);
			Gtk.Application.Invoke (delegate {
				DownloadProgress.Fraction = 0.00;
				DownloadProgress.Text = "Checking for updates to " + SelectedGame.ActiveText;
				Log.Buffer.Text = "Current version of application " + SelectedGame.ActiveText + " is " + versions[vIndex] + " ..\n" + Log.Buffer.Text;
				Log.Buffer.Text = "Checking for updates to " + SelectedGame.ActiveText + " ..\n" + Log.Buffer.Text;
		});
		}
		else
		{
			Gtk.Application.Invoke (delegate {
				DownloadProgress.Fraction = 0.00;
				DownloadProgress.Text = "Checking for latest version of " + SelectedGame.ActiveText;
				Log.Buffer.Text = "Checking for latest version of " + SelectedGame.ActiveText + " ..\n" + Log.Buffer.Text;
		});
		}
			try{
				byte[] content = wc.DownloadData(url);
				string version = System.Text.Encoding.Default.GetString(content);
				if(letInstall || Convert.ToDouble(version, System.Globalization.NumberFormatInfo.InvariantInfo)>versions[vIndex]){
					Gtk.Application.Invoke (delegate {
				    	DownloadProgress.Text = "Found a new version of the selected application(v" + version + ")";
						Log.Buffer.Text = "A new version of " + SelectedGame.ActiveText + " was found(v" + version + ")!\n" + Log.Buffer.Text;
					});
                    DownloadGame(version);
				}
                else if (Convert.ToDouble(version, System.Globalization.NumberFormatInfo.InvariantInfo) == versions[vIndex])
                {
					Gtk.Application.Invoke (delegate {
						DownloadProgress.Fraction = 1.00;
						DownloadProgress.Text = SelectedGame.ActiveText + " is up to date";
						Log.Buffer.Text = SelectedGame.ActiveText + " is up to date\n" + Log.Buffer.Text;
						LaunchButton.Sensitive = true;
						letLaunch = true;
					});
				}
			}
			catch{
				Gtk.Application.Invoke (delegate {
					Log.Buffer.Text = "Could not check for the latest version online, are you connected to the internet?\n" + Log.Buffer.Text;
					DownloadProgress.Text = "An error occurred.";
					DownloadProgress.Fraction = 1.00;
				});
			}
		
	}
	
	private void DownloadGame(string version){
	    String url = website + SelectedGame.ActiveText + "/" + version.Replace("\n", "") +".zip";
		Uri uri = new Uri(url);
		Gtk.Application.Invoke (delegate {
		Log.Buffer.Text = "Downloading new version of" + SelectedGame.ActiveText + " ..\n" + Log.Buffer.Text;
		});
        wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
		updateGame = true;
        string zip = "new.zip";
        if (Windows)
            zip = WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\" + zip;
        else
            zip = "./" + SelectedGame.ActiveText + "/" + zip;
		wc.DownloadFileAsync(uri, zip);
	}
	
	private void updateGameFiles()
	{
		Gtk.Application.Invoke (delegate {
			Log.Buffer.Text = "File was succesfully downloaded, unpacking files..\n" + Log.Buffer.Text;
			DownloadProgress.Text = "Unpacking files";
		});
		Process p = new Process();
		ProcessStartInfo startInfo = new ProcessStartInfo();
        if (!Windows)
        {
            startInfo.FileName = "Scripts/Linux/patchLinuxGame";
            startInfo.Arguments = SelectedGame.ActiveText;
            p = Process.Start(startInfo);
            p.WaitForExit();
        }
        else
        {
            startInfo.FileName = WorkingDirectory + "\\Scripts\\Windows\\unzip.exe";
            startInfo.Arguments = "unzip " + WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\new.zip "; //+ WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\";
            string path = WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\new.zip";
                using (ZipFile zip = ZipFile.Read(path))
                {
                    zip.ExtractAll(WorkingDirectory + "\\" + SelectedGame.ActiveText, ExtractExistingFileAction.OverwriteSilently);
                }
        }
		Gtk.Application.Invoke (delegate {
			DownloadProgress.Text = SelectedGame.ActiveText + " is up to date";
			Log.Buffer.Text = SelectedGame.ActiveText + "was successfully updated\n" + Log.Buffer.Text;
		});
		LaunchButton.Sensitive = true;
		letLaunch = true;
        letInstall = false;
        LaunchButton.Label = "Launch " + SelectedGame.ActiveText;
	}
	#endregion
	#region load data

    private void DetectOS()
    {
        switch (pid)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                Windows = true;
                WorkingDirectory = Directory.GetCurrentDirectory();
                break;
            case PlatformID.Unix:
                Windows = false;
                break;
            default:
                Windows = false;
                break;
        }
    }

	private void DetectInstalledApplications()
	{
		Log.Buffer.Text = "Getting installed applications\n" + Log.Buffer.Text;
		foreach(string s in applications)
		{
            string file;
            if(!Windows)
			    file = "./" + s + "/version.txt";
            else
                file = WorkingDirectory + "\\" + s + "\\version.txt";
			if(File.Exists(file))
			{
				string[] ver = new string[5];
				ver = File.ReadAllLines(file);
                double v = Convert.ToDouble(ver[0], System.Globalization.NumberFormatInfo.InvariantInfo);
				installed.Add(s);
				Log.Buffer.Text = s + " v" + ver[0] + " installed.\n" + Log.Buffer.Text;
				versions.Add(v);
			}
		}
		if(installed.Count==0)
			Log.Buffer.Text = "No installed applications found\n" + Log.Buffer.Text;
	}
	
	private void DetectLauncherVersion()
	{
			string file = "./Lversion.txt";
			if(File.Exists(file))
			{
				string[] ver = new string[5];
				ver = File.ReadAllLines(file);
				LauncherVersion = ver[0];
			}
	}
	#endregion
	#region events
	
	private void LaunchButtonClicked (object sender, EventArgs a)
	{
		Gtk.Application.Invoke (delegate {
			Log.Buffer.Text = "clicked\n" + Log.Buffer.Text;
		});
		if(letInstall){
			string path = "./" + SelectedGame.ActiveText;
			System.IO.Directory.CreateDirectory(path);
			thread = new Thread(CheckGameVersion);
			thread.IsBackground = true;
			thread.Start();
			LaunchButton.Sensitive = false;
		}
        else if (letLaunch)
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!Windows)
            {
                startInfo.FileName = "./" + SelectedGame.ActiveText + "/" + SelectedGame.ActiveText + ".exe";
                p = Process.Start(startInfo);
                this.Hide();
                p.WaitForExit();
                this.Show();
            }
            else
            {
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.WorkingDirectory = WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\";
                startInfo.FileName = WorkingDirectory + "\\" + SelectedGame.ActiveText + "\\" + SelectedGame.ActiveText + ".exe";
                p = Process.Start(startInfo);
                this.Hide();
                p.WaitForExit();
                this.Show();
            }
        }
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		time.Abort();
		thread.Abort();
		Application.Quit ();
		a.RetVal = true;
	}
	
	private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
	{
	    // Displays the operation identifier, and the transfer progress.
	    Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...", 
	        (string)e.UserState, 
	        e.BytesReceived, 
	        e.TotalBytesToReceive,
	        e.ProgressPercentage);
		if(updateProgress%200==0){
			Gtk.Application.Invoke (delegate {
                DownloadProgress.Fraction = (double)e.ProgressPercentage/100;
				DownloadProgress.Text = "Downloading data " + e.ProgressPercentage.ToString () + "% " + (float)e.BytesReceived/1048576 +"/" + (float)e.TotalBytesToReceive/1048576 + " MB";
			});
		}
		else
			updateProgress++;
		if(e.ProgressPercentage==100 && updateLauncher)
		{
			Gtk.Application.Invoke (delegate {
				Log.Buffer.Text = "File was succesfully downloaded, applying patch in 5 seconds..\n" + Log.Buffer.Text;
			});                                                      
			Thread.Sleep(5000);
            if(!Windows)
			    Process.Start("Scripts/Linux/patchLinuxLauncher");
            else{
                string path = WorkingDirectory + "\\new.zip";
                using (ZipFile zip = ZipFile.Read(path))
                {
                    zip.ExtractAll(WorkingDirectory + "\\", ExtractExistingFileAction.OverwriteSilently);
                }
            }
			Application.Quit ();
		}
		
		if(e.ProgressPercentage==100 && updateGame)
		{
			updateProgress = 0;
            if (!processActive)
            {
                processActive = true;
                thread = new Thread(updateGameFiles);
                thread.Start ();
            }
			//mer
		}
	}
	#endregion
}
