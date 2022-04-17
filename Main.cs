using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Reflection;
using ProxyBind.Properties;

namespace ProxyBind
{
	public class Main : ApplicationContext
	{
		//var
		private List<ProxyBind> pbList = new List<ProxyBind>();
		private NotifyIcon niTrayIcon;

		//ctor, start, stop
		public Main()
		{
			//prepare menu item
			MenuItem[] mi = new MenuItem[] 
			{
				new MenuItem("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()),
				new MenuItem("-"),
				new MenuItem("Open app folder", OpenAppFolder),
				new MenuItem("Exit", Exit),
			};
			mi[0].Enabled = false;
			

			// Initialize Tray Icon
			niTrayIcon = new NotifyIcon();
			niTrayIcon.Icon = Resources.MainIcon;
			niTrayIcon.ContextMenu = new ContextMenu(mi);
			niTrayIcon.Visible = true;

			//start
			Start();
		}
		private void Start()
		{
			//init all setting
			IniFile cIni = new IniFile(Utils.sDirData + "proxybind_settings.ini");
			string sTapInterfaceName = cIni.Get(IniVarName.sTapInterfaceName, @"Tap1, Tap2, Tap3, Wi-Fi 1, Wi-Fi 2");
			string sIpInternal = cIni.Get(IniVarName.sIpInternal, @"0.0.0.0, 0.0.0.0, 0.0.0.0, 0.0.0.0");
			string sIpDnsServer = cIni.Get(IniVarName.sIpDnsServer, "1.1.1.1, 8.8.8.8, 9.9.9.9, 9.9.9.10");
			string sPortProxy = cIni.Get(IniVarName.sPortProxy, @"16801, 16803, 16805, 16807, 16809");
			string sPortSocks = cIni.Get(IniVarName.sPortSocks, @"16802, 16804, 16806, 16808, 16810");
			string sAcwInterface = cIni.Get(IniVarName.sAcwInterface, @"Wi-Fi 1, Wi-Fi 2");
			string sAcwSsid = cIni.Get(IniVarName.sAcwSsid, @"AP1, AP2_5Ghz");
			bool bDirCleanUp = cIni.Get(IniVarName.bDirCleanUp, true);

			//debug log
			Utils.cLog = new Logger(Utils.sDirLog + Utils.sAppName);
			Utils.cLog.KeepDays(cIni.Get(IniVarName.iLogDays, 0));

			//multi tap 1
			string[] sInterfaceSlpit = sTapInterfaceName.Split(',');
			string[] sIpInternalSlpit = sIpInternal.Split(',');
			string[] sIpDnsServerSplit = sIpDnsServer.Split(',');
			string[] sPortProxySlpit = sPortProxy.Split(',');
			string[] sPortSocksSlpit = sPortSocks.Split(',');
			string[] sAcwInterfaceSlpit = sAcwInterface.Split(',');
			string[] sAcwSsidSlpit = sAcwSsid.Split(',');

			//network change event
			tmrConnectionChanged = new System.Timers.Timer();
			tmrConnectionChanged.Interval = 1;
			tmrConnectionChanged.Elapsed += tmrConnectionChanged_Elapsed;
			tmrConnectionChanged.Start();
			NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

			//sleep wake event
			SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

			string sIpInternalPrev = "0.0.0.0";
			string sIpDnsServerPrev = "9.9.9.9";
			for (int i = 0; i < sInterfaceSlpit.Length; i++)
			{
				ProxyConfig stCfg = new ProxyConfig();
				stCfg.iId = i;
				stCfg.bConnUp = false;
				stCfg.bDirCleanUp = bDirCleanUp;
				stCfg.sInterface = sInterfaceSlpit[i].Trim();
				stCfg.sPortProxy = sPortProxySlpit[i].Trim();
				stCfg.sPortSocks = sPortSocksSlpit[i].Trim();

				//remember previously assigned
				stCfg.sIpInternal = i < sIpInternalSlpit.Length ?
					(sIpInternalSlpit[i].Length > 0 ? sIpInternalSlpit[i].Trim() : sIpInternalPrev)
					: sIpInternalPrev;
				sIpInternalPrev = stCfg.sIpInternal;
				stCfg.sIpDnsServer = i < sIpDnsServerSplit.Length ?
					(sIpDnsServerSplit[i].Length > 0 ? sIpDnsServerSplit[i].Trim() : sIpDnsServerPrev)
					: sIpDnsServerPrev;
				sIpDnsServerPrev = stCfg.sIpDnsServer;

				//auto connect wifi
				stCfg.sAcwInterface = i < sAcwInterfaceSlpit.Length ? sAcwInterfaceSlpit[i].Trim() : "";
				stCfg.sAcwSsid = i < sAcwSsidSlpit.Length ? sAcwSsidSlpit[i].Trim() : "";

				//network change event
				stCfg.evConnectionChanged = evConnectionChanged;

				//connection status changed
				stCfg.dlgConnectionStatusChanged = new Action(OnConnectionStatusChanged);

				ProxyBind pb = new ProxyBind(stCfg);
				pbList.Add(pb);
			}
		}
		private void Stop()
		{
			NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
			tmrConnectionChanged.Stop();
			SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

			for (int i = 0; i < pbList.Count; i++)
			{
				if (pbList[i] != null)
				{
					pbList[i].Stop();
				}
			}
		}

		//ui
		private void Exit(object sender, EventArgs e)
		{
			Stop();

			// Hide tray icon, otherwise it will remain shown until user mouses over it
			niTrayIcon.Visible = false;
			Application.Exit();
		}
		private void OpenAppFolder(object sender, EventArgs e)
		{
			string sExeName = System.AppDomain.CurrentDomain.BaseDirectory + Path.GetFileNameWithoutExtension(System.AppDomain.CurrentDomain.FriendlyName);
			string sDirectory = Path.GetDirectoryName(sExeName);
			Process.Start(sDirectory);
		}

		//connection status consolidte
		private void OnConnectionStatusChanged()
		{
			List<string> sToolTip = new List<string>();
			for (int i = 0; i < pbList.Count; i++)
			{
				sToolTip.Add(pbList[i].cfg.sInterface.Split(' ')[0] + ": " + (pbList[i].cfg.bConnUp ? "▲" : "X"));
			}

			string sToolTipAll = string.Join("\r\n", sToolTip.ToArray());
			niTrayIcon.Text = sToolTipAll;
		}

		//connection changed event
		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			Utils.cLog.Log("NetworkAddressChanged");
			ConnectionChangedCi();
		}
		private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			bool bInterested;

			switch (e.Mode)
			{
				case PowerModes.Suspend:
					bInterested = false;
					break;

				case PowerModes.StatusChange:
				case PowerModes.Resume:
					bInterested = true;
					break;

				default:
					bInterested = true;
					break;
			}

			Utils.cLog.Log("PowerModeChanged: " + e.Mode.ToString() + (bInterested ? "" : " (ignored)"));
			if (bInterested) ConnectionChangedCi();
		}
		
		//connection check timer
		private ManualResetEvent evConnectionChanged = new ManualResetEvent(false);
		private System.Timers.Timer tmrConnectionChanged;
		private void ConnectionChangedCi()
		{
			lock (tmrConnectionChanged)
			{
				tmrConnectionChanged.Stop();
				tmrConnectionChanged.Start();
			}
		}
		private void tmrConnectionChanged_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			tmrConnectionChanged.Stop();
			tmrConnectionChanged.Interval = 5000;

			evConnectionChanged.Set();
			Thread.Sleep(500);
			evConnectionChanged.Reset();
		}
	}
}
