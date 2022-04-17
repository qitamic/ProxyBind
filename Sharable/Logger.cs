using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

public class Logger
{
	//var general
	private string sExt = "log";
	private TimeSpan tsAutoDelete = new TimeSpan(0, 1, 0); //1 minute
	
	//ctor ss
	public Logger()
	{
		string sPath = System.AppDomain.CurrentDomain.BaseDirectory + Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location);

		sDirectory = Path.GetDirectoryName(sPath);
		sPrefix = Path.GetFileNameWithoutExtension(sPath) + "_";

		Init(sDirectory, sPrefix);
	}
	public Logger(string sPath)
	{
		sDirectory = Path.GetDirectoryName(sPath);
		sPrefix = Path.GetFileNameWithoutExtension(sPath) + "_";

		Init(sDirectory, sPrefix);
	}

	//init
	private string sDirectory;
	private string sPrefix;
	private bool bPreciseTime;
	private void Init(string sDir, string sPrx)
	{
		this.sDirectory = sDir;
		this.sPrefix = sPrx;

		Version vOs = System.Environment.OSVersion.Version;
		if (vOs.Major > 6) //win10
			bPreciseTime = true;
		else if (vOs.Major == 6 && vOs.Minor >= 2) //win8
			bPreciseTime = true;
		else
			bPreciseTime = false;

		wkWriter = new Worker<stCapsule>();
		wkWriter.Work += wkWriter_Work;

		swAutoDetele = new Stopwatch();
		swAutoDetele.Start();
	}

	//config after ctor
	private volatile bool bEnabled;
	private volatile int iKeepDay = int.MaxValue;
	public void KeepDays(int iKeepDay)
	{
		this.bEnabled = iKeepDay > 0;
		this.iKeepDay = iKeepDay;
	}

	//file writing
	private struct stCapsule
	{
		public DateTime dt;
		public string s;
		public Exception ex;
	}
	private Worker<stCapsule> wkWriter;
	private void wkWriter_Work(Logger.stCapsule st)
	{
		if (st.ex != null)
		{
			//ex's call stack
			string sInner = "Execption: " + st.s + ": ";
			Exception exInner = st.ex;
			do
			{
				sInner = sInner + exInner.Message + "\n" + exInner.StackTrace + "\n";
				exInner = exInner.InnerException;
			} while (exInner != null);

			//LogExecption(Exception ex)'s call stack
			string sCS = "-->" + System.Environment.StackTrace.Substring(3);
			sInner = sInner + sCS + "\n";

			//remove all newline
			sInner = sInner.Replace("\r\n", "<nl>").Replace("\r", "<nl>").Replace("\n", "<nl>");

			st.s = sInner;
		}

		Directory.CreateDirectory(sDirectory);
		string sTimeFmt = bPreciseTime ? "{0:yy-MM-dd HH:mm:ss.ffffff}" : "{0:yy-MM-dd HH:mm:ss.fff}";
		string sTimeStamp = string.Format(sTimeFmt, st.dt);
		string sLogLine = sTimeStamp + "\t" + st.s + "\r\n";
		string sFilePath = GetFilename();

		File.AppendAllText(sFilePath, sLogLine, Encoding.UTF8);

		AutoDetele();
	}

	//auto delete
	private Stopwatch swAutoDetele;
	private void AutoDetele()
	{
		//perform auto delete only after X time
		if(swAutoDetele.Elapsed < tsAutoDelete)
			return;

		//reset stopwatch, come in here after X time
		swAutoDetele.Reset();

		try
		{
			DirectoryInfo diDir = new DirectoryInfo(sDirectory);
			FileInfo[] fiFiles = diDir.GetFiles(sPrefix + "*." + sExt, SearchOption.TopDirectoryOnly);

			for (int i = 0; i < fiFiles.Length; i++)
			{
				DateTime dtModified = fiFiles[i].LastWriteTime;
				TimeSpan dtElapsed = DateTime.Now - dtModified;
				if (dtElapsed.Days > iKeepDay)
				{
					fiFiles[i].IsReadOnly = false;
					fiFiles[i].Delete();
				}
			}
		}
		catch (Exception ex)
		{
			if (Debugger.IsAttached)
				Debugger.Break();
		}
	}

	//public
	public void Log(string sMessage)
	{
		//enable disable
		if (!bEnabled) return;

		//prepare item to be queued
		stCapsule st = new stCapsule();
		if (bPreciseTime)
			st.dt = GetTimeStamp();
		else
			st.dt = DateTime.Now;
		st.s = sMessage;

		wkWriter.Do(st);
	}
	public void Log(Exception ex)
	{
		Log(ex, "");
	}
	public void Log(Exception ex, string sRemark)
	{
		//enable disable
		if (!bEnabled) return;

		//prepare item to be queued
		stCapsule st = new stCapsule();
		if (bPreciseTime)
			st.dt = GetTimeStamp();
		else
			st.dt = DateTime.Now;
		st.s = sRemark;
		st.ex = ex;

		wkWriter.Do(st);
	}
	public string GetFilename()
	{
		string sPostfix = string.Format("{0:yyMMdd}", DateTime.Now);
		string sFilename = sPrefix + sPostfix + "." + sExt;
		string sFilePath = Path.Combine(sDirectory, sFilename);

		return sFilePath;
	}

	//Interop
	[SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll")]
	private static extern void GetSystemTimePreciseAsFileTime(out FileTime pFileTime);
	[StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	private struct FileTime
	{
		public const long FILETIME_TO_DATETIMETICKS = 504911232000000000;   // 146097 = days in 400 year Gregorian calendar cycle. 504911232000000000 = 4 * 146097 * 86400 * 1E7
		public uint TimeLow;    // least significant digits
		public uint TimeHigh;   // most sifnificant digits
		public long TimeStamp_FileTimeTicks { get { return TimeHigh * 4294967296 + TimeLow; } }     // ticks since 1-Jan-1601 (1 tick = 100 nanosecs). 4294967296 = 2^32
		public DateTime dateTime { get { return new DateTime(TimeStamp_FileTimeTicks + FILETIME_TO_DATETIMETICKS); } }
	}
	private DateTime GetTimeStamp()
	{
		FileTime ft;
		GetSystemTimePreciseAsFileTime(out ft);
		return ft.dateTime.ToLocalTime();
	}
}
