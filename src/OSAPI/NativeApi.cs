using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace WindowsAPI;

public class NativeApi {
	public static NativeApi Main = new();

	public enum OS {
		Error,
		Windows,
		OSX,
		Linux,
		FreeBSD
	}

	public NativeApi() {
		currentOS = GetOS();
	}

	public OS currentOS;

	public static OS GetOS() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			return OS.Windows;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			return OS.Linux;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			return OS.OSX;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
			return OS.FreeBSD;
		}
		return OS.Error;
	}

	public virtual string GetCpuName() {
		string cpuName = "Unknown";
		if (currentOS == OS.Windows) {
			// For Windows OS.
			cpuName = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
				@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\"
			)?.GetValue(
				"ProcessorNameString"
			) as string ?? "Windows";
		} else if (currentOS == OS.Linux) {
			if (!File.Exists("/proc/cpuinfo")) {
				return "Unix";
			}
			// Read all lines from /proc/cpuinfo
			string[] lines = File.ReadAllLines("/proc/cpuinfo");
			// Find the line containing "model name"
			string? modelNameLine = lines.FirstOrDefault(
				line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase)
			);
			if (modelNameLine != null) {
				// Extract the model name part after the colon and trim whitespace
				lines = modelNameLine.Split(':');
				if (lines.Length >= 2) {
					cpuName = lines[1];
				}
			}
		} else {
			cpuName = "Darwin";
		}
		// Fix simbols.
		cpuName = cpuName.Replace("(R)", "®");
		cpuName = cpuName.Replace("(C)", "©");
		cpuName = cpuName.Replace("(TM)", "©"); //Todo, implement proper trademark simbol.
		return cpuName;
	}

	public virtual void ShowMessageBox(string message, string caption) {
		Console.WriteLine($"{caption}: {message}");
	}
	public virtual bool ShowMessageBoxYesNo(string message, string caption) {
		Console.WriteLine($"{caption}: {message}");
		return true;
	}
	public virtual bool KeyState(int keyCode) => false;
	public virtual bool AllocNewConsole() => false;
}