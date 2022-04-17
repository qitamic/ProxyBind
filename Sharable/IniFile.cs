using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

public enum IniVarName
{
	sTapInterfaceName,
	sIpInternal,
	sIpDnsServer,
	sPortProxy,
	sPortSocks,
	sAcwInterface,
	sAcwSsid,
	bDirCleanUp,
	iLogDays,
}

public class IniFile
{
	private string sPath;
	private string sDefaultSection = "General";
	private int iBufferSize = 8192;
	private string sEmpty = "zero length string check";

	public IniFile(string sPath)
	{
		if (!sPath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
			sPath += ".ini";

		this.sPath = sPath;
	}

	private string Read(string Key)
	{
		return Read(Key, null);
	}
	private string Read(string Key, string Section)
	{
		var RetVal = new StringBuilder(iBufferSize);
		GetPrivateProfileString(Section == null ? sDefaultSection : Section, Key, "", RetVal, iBufferSize, sPath);
		return RetVal.ToString();
	}
	private void Write(string Key, string Value)
	{
		Write(Key, Value, null);
	}
	private void Write(string Key, string Value, string Section)
	{
		WritePrivateProfileString(Section ?? sDefaultSection, Key, Value, sPath);
	}

	public string Get(IniVarName Set, string Default)
	{
		string Key = Set.ToString();
		string s;

		if (KeyExists(Key, null))
			s = Read(Key, null);
		else
		{
			Write(Key, Default);
			s = Default;
		}

		return s;
	}
	public int Get(IniVarName Set, int Default)
	{
		string s = Get(Set, Default.ToString());
		return Convert.ToInt32(s);
	}
	public bool Get(IniVarName Set, bool Default)
	{
		string s = Get(Set, Default ? "1" : "0");
		bool b;

		if (bool.TryParse(s, out b)) return b;
		else return s == "1";
	}
	public double Get(IniVarName Set, double Default)
	{
		string s = Get(Set, Default.ToString());
		return Convert.ToDouble(s);
	}

	public void Set(IniVarName Item, string Value)
	{
		Write(Item.ToString(), Value);
	}
	public void Set(IniVarName Item, int Value)
	{
		Set(Item, Value.ToString());
	}
	public void Set(IniVarName Item, bool Value)
	{
		Set(Item, Value ? "1" : "0");
	}
	public void Set(IniVarName Item, double Value)
	{
		Set(Item, Value.ToString());
	}

	public bool KeyExists(string Key, string Section)
	{
		string ReadOut = sEmpty;
		StringBuilder sRetVal = new StringBuilder(iBufferSize);
		GetPrivateProfileString(Section ?? sDefaultSection, Key, ReadOut, sRetVal, iBufferSize, sPath);

		return ReadOut != sRetVal.ToString(); ;
	}
	public void DeleteKey(string Key, string Section)
	{
		Write(Key, null, Section ?? sDefaultSection);
	}
	public void DeleteSection(string Section)
	{
		Write(null, null, Section ?? sDefaultSection);
	}

	[DllImport("kernel32", CharSet = CharSet.Unicode)]
	private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);
	[DllImport("kernel32", CharSet = CharSet.Unicode)]
	private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
}