using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;

namespace ProxyBind
{
	public static class Utils
	{
		//var
		public static string sExePath = Assembly.GetExecutingAssembly().Location;
		public static string sAppName = Path.GetFileNameWithoutExtension(sExePath);
		public static string sDirExe = Path.GetDirectoryName(sExePath) + @"\";
		public static string sDirLog = sDirExe + @"Log\";
		public static string sDirData = sDirExe + @"Data\";
		public static Logger cLog;

		//network
		public static bool NetworkInterfaceUp(string sName)
		{
			string sIpAddress;
			return NetworkInterfaceUp(sName, out sIpAddress);
		}
		public static bool NetworkInterfaceUp(string sName, out string sIpAddress)
		{
			NetworkInterfaceType enType;
			return NetworkInterfaceUp(sName, out sIpAddress, out enType);
		}
		public static bool NetworkInterfaceUp(string sName, out string sIpAddress, out NetworkInterfaceType enType)
		{
			bool bUp = false;
			sIpAddress = null;
			enType = NetworkInterfaceType.Unknown;

			NetworkInterface[] niAll = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

			for (int i = 0; i < niAll.Length; i++)
			{
				if (niAll[i].Name.Equals(sName, StringComparison.OrdinalIgnoreCase))
				{
					bUp = niAll[i].OperationalStatus == OperationalStatus.Up;
					enType = niAll[i].NetworkInterfaceType;

					if (bUp)
					{
						// Read the IP configuration for each network 
						IPInterfaceProperties properties = niAll[i].GetIPProperties();

						// Each network interface may have multiple IP addresses 
						foreach (IPAddressInformation address in properties.UnicastAddresses)
						{
							// We're only interested in IPv4 addresses for now 
							if (address.Address.AddressFamily != AddressFamily.InterNetwork)
								continue;

							// Ignore loopback addresses (e.g., 127.0.0.1) 
							if (IPAddress.IsLoopback(address.Address))
								continue;

							sIpAddress = address.Address.ToString();
							if (sIpAddress.Length < "0.0.0.0".Length)
							{
								Utils.cLog.Log("Problem: "  + sName + "'s IP=" + sIpAddress);
								bUp = false;
							}
						}
					}
					break;
				}
			}
			return bUp;
		}

		//exec
		public static int Execute(string sProgram, string sArgs, bool bWaitForExit)
		{
			Process proc = new System.Diagnostics.Process();
			proc.StartInfo.FileName = sProgram;
			proc.StartInfo.Arguments = sArgs;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; //prevent console window from popping up
			Console.WriteLine(sProgram + " " + proc.StartInfo.Arguments);
			proc.Start();

			cLog.Log("sProgram=" + proc.StartInfo.FileName);
			cLog.Log("sArgs=" + proc.StartInfo.Arguments);
			cLog.Log("bWaitForExit=" + bWaitForExit);
			cLog.Log("Pid=" + proc.Id + "\n");

			if (bWaitForExit) proc.WaitForExit();

			return proc.Id;
		}
		public static void Execute(string sCmd)
		{
			Process proc = new System.Diagnostics.Process();
			proc.StartInfo.FileName = "cmd.exe";
			proc.StartInfo.Arguments = "/c start /min \"\" " + sCmd;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; //prevent console window from popping up
			Console.WriteLine(proc.StartInfo.Arguments);
			proc.Start();
			cLog.Log(proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
		}
		public static string[] WriteSafeReadAllLines(String path)
		{
			using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var sr = new StreamReader(csv))
			{
				List<string> file = new List<string>();
				while (!sr.EndOfStream)
				{
					file.Add(sr.ReadLine());
				}

				return file.ToArray();
			}
		}

		//kill pricess
		public static bool ProcessExist(int iPid)
		{
			Process[] pProcessList = Process.GetProcesses();
			int[] iPidList = new int[pProcessList.Length];
			for (int i = 0; i < pProcessList.Length; i++)
				iPidList[i] = pProcessList[i].Id;

			return iPidList.Contains(iPid);
		}
		public static void ProcessKill(int iPid)
		{
			do
			{
				Execute("taskkill.exe", "/f /pid " + iPid, true);
				Thread.Sleep(100);
			} while (ProcessExist(iPid));
		}
	}
}
