using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ProxyBind
{
	public class ProxyConfig
	{
		public string sInterface; //for proxy to bind
		public string sIpInternal; //default is 0.0.0.0
		public string sPortProxy;
		public string sPortSocks;
		public string sIpDnsServer;

		//auto connect wifi
		public string sAcwInterface; //for proxy client
		public string sAcwSsid;

		//connection status
		public int iId;
		public bool bConnUp;

		//delele temp setting file
		public bool bDirCleanUp;

		//network change event
		public ManualResetEvent evConnectionChanged;

		//connection status changed
		public Action dlgConnectionStatusChanged;
	}

	public class ProxyBind
	{
		//var
		public ProxyConfig cfg;

		//ctor ss
		public ProxyBind(ProxyConfig cfg)
		{
			this.cfg = cfg;

			wkConnectionMaker = new Worker();
			wkConnectionMaker.Work += wkConnectionMaker_Work;
			wkConnectionMaker.Do();
		}
		public void Stop()
		{
			ProxyStop();
		}

		//connection maker var
		private string sConnectedIp;
		private string sConnectedIp_;
		private bool bConnected;
		private bool bConnected_;

		//connection maker
		private Worker wkConnectionMaker;
		private void wkConnectionMaker_Work()
		{
			while (true)
			{
				try
				{
					//just to make sure all threads are released
					cfg.evConnectionChanged.WaitOne();
					while (cfg.evConnectionChanged.WaitOne(0))
						Thread.Sleep(1);

					//actual work
					Utils.cLog.Log("ConnectionCheck: " + cfg.sInterface);
					string sConnName = cfg.sInterface;

					bConnected_ = bConnected;
					sConnectedIp_ = sConnectedIp;
					bConnected = Utils.NetworkInterfaceUp(sConnName, out sConnectedIp);

					//connectional ip change check
					if (!bConnected_ && bConnected)
						Up();
					else if (bConnected_ && !bConnected)
						Down();
					else if (sConnectedIp_ != sConnectedIp)
					{
						//ip changed
						Down();
						Thread.Sleep(2000);
						Up();
					}

					//reconnect wifi
					if (cfg.sAcwSsid.Length > 0 && cfg.sAcwInterface.Length > 0 && !Utils.NetworkInterfaceUp(cfg.sAcwInterface))
						ConnectToWifi(cfg.sAcwSsid, cfg.sAcwInterface);
				}
				catch (Exception ex)
				{
					Utils.cLog.Log(ex);
				}
			}
		}
		private void ConnectToWifi(string sSsid, string sInterface)
		{
			string sCmd = "netsh wlan connect ssid=\"" + sSsid + "\" name=\"" + sSsid + "\" interface=\"" + sInterface + "\"";
			Console.WriteLine(sCmd);
			Utils.Execute(sCmd);
		}
		private void Up()
		{
			//start proxy
			ProxyStart();

			//notify
			cfg.bConnUp = true;
			if (cfg.dlgConnectionStatusChanged != null)
				cfg.dlgConnectionStatusChanged();
		}
		private void Down()
		{
			//stop proxy
			ProxyStop();

			//notify
			cfg.bConnUp = false;
			if (cfg.dlgConnectionStatusChanged != null)
				cfg.dlgConnectionStatusChanged();
		}

		//ss proxy
		private int iPid3proxy;
		private int iPidUdpfwdc;
		private void ProxyStart()
		{
			//ATTENTION: DO NOT USE ForceBindIP, it casues 3proxy to crash during high throughput!!

			Utils.cLog.Log("Start: " + cfg.sInterface);

			string sCfgPathAutoBind = Utils.sDirExe + @"Data\proxybind_3proxy_auto_" + cfg.sInterface + ".cfg";

			string sExePath3proxy = Utils.sDirExe + @"Data\proxybind_3proxy.exe";
			string sExePathUdpfwdc = Utils.sDirExe + @"Data\udpfwdc.exe";
			List<string> cfgWrite = new List<string>();
			string sArg;

			//bind
			cfgWrite.Clear();
			cfgWrite.Add("daemon");
			cfgWrite.Add("external " + sConnectedIp);
			cfgWrite.Add("internal " + cfg.sIpInternal);
			cfgWrite.Add("nserver " + sConnectedIp);
			cfgWrite.Add("nscache 65535");
			cfgWrite.Add("proxy -p" + cfg.sPortProxy);
			cfgWrite.Add("socks -p" + cfg.sPortSocks);
			File.WriteAllLines(sCfgPathAutoBind, cfgWrite.ToArray());
			iPid3proxy = Utils.Execute(sExePath3proxy, "\"" + sCfgPathAutoBind + "\"", false);

			//dns: only 1 dns server is enough, queries are barely being sent to secondary anyway even when primary is down
			iPidUdpfwdc = Utils.Execute(sExePathUdpfwdc, sConnectedIp + " 53 " + cfg.sIpDnsServer + " 53 " + sConnectedIp + " 10000 d", false);

			//clean up
			if (cfg.bDirCleanUp)
			{
				Thread.Sleep(1000);
				File.Delete(sCfgPathAutoBind);
			}
		}
		private void ProxyStop()
		{
			Utils.cLog.Log("Stop: " + cfg.sInterface);

			bool bStarted = iPidUdpfwdc > 0 || iPid3proxy > 0;

			if (bStarted)
			{
				Utils.Execute("taskkill /f /pid " + iPid3proxy);
				Utils.Execute("taskkill /f /pid " + iPidUdpfwdc);
			}
			else
			{
				Utils.cLog.Log("Stop: " + cfg.sInterface + ": not started");
			}
		}
	}
}
