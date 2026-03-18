using System;
using System.Linq;
using System.Runtime.InteropServices;
using MMXOnline;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace WindowsAPI;

public class WinApi : NativeApi {
#if !NOTWINDOWS
	public enum GWL : int {
		EXSTYLE = -20,
		HINSTANCE = -6,
		HWNDPARENT = -8,
		ID = -21,
		STYLE = -16,
		USERDATA = -21,
		WNDPROC = -4,
	}

	public enum WS : uint {
		BORDER = 0x00800000,
		CAPTION = 0x00C00000,
		CHILD = 0x40000000,
		CHILDWINDOW = 0x40000000,
		CLIPCHILDREN = 0x02000000,
		CLIPSIBLINGS = 0x04000000,
		DISABLED = 0x08000000,
		DLGFRAME = 0x00400000,
		GROUP = 0x00020000,
		HSCROLL = 0x00100000,
		ICONIC = 0x20000000,
		MAXIMIZE = 0x01000000,
		MAXIMIZEBOX = 0x00010000,
		MINIMIZE = 0x20000000,
		MINIMIZEBOX = 0x00020000,
		OVERLAPPED = 0x00000000,
		OVERLAPPEDWINDOW = (OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX),
		POPUP = 0x80000000,
		POPUPWINDOW = (POPUP | BORDER | SYSMENU),
		SIZEBOX = 0x00040000,
		SYSMENU = 0x00080000,
		TABSTOP = 0x00010000,
		THICKFRAME = 0x00040000,
		TILED = 0x00000000,
		TILEDWINDOW = (OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX),
		VISIBLE = 0x10000000,
		VSCROLL = 0x00200000,
	}

	public enum WSEX : uint {
		APPWINDOW = 0x00040000,
		CLIENTEDGE = 0x00000200,
		COMPOSITED = 0x02000000,
		CONTEXTHELP = 0x00000400,
		CONTROLPARENT = 0x00010000,
		DLGMODALFRAME = 0x00000001,
		LAYERED = 0x00080000,
		LAYOUTRT = 0x00400000,
		LEFT = 0x00000000,
		LEFTSCROLLBAR = 0x00004000,
		LTRREADING = 0x00000000,
		MDICHILD = 0x00000040,
		NOACTIVATE = 0x08000000,
		NOINHERITLAYOUT = 0x00100000,
		NOPARENTNOTIFY = 0x00000004,
		NOREDIRECTIONBITMAP = 0x00200000,
		OVERLAPPEDWINDOW = (WINDOWEDGE | CLIENTEDGE),
		PALETTEWINDOW = (WINDOWEDGE | TOOLWINDOW | TOPMOST),
		RIGHT = 0x00001000,
		RIGHTSCROLLBAR = 0x00000000,
		RTLREADING = 0x00002000,
		STATICEDGE = 0x00020000,
		TOOLWINDOW = 0x00000080,
		TOPMOST = 0x00000008,
		TRANSPARENT = 0x00000020,
		WINDOWEDGE = 0x00000100,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WinRect {
		public int left;
		public int up;
		public int right;
		public int down;
	}

	// -------------------------- //
	// Overriden hooks start here //
	// -------------------------- //

	public override void ShowMessageBox(string message, string caption) {
		ShowMessageBox(Global.window, message, caption, 0, Options.main.fullScreen);
	}

	public override bool ShowMessageBoxYesNo(string message, string caption) {
		bool dialogResult = ShowMessageBox(
			Global.window, message, caption, 4, Options.main.fullScreen
		);
		return dialogResult;
	}

	public override bool KeyState(int keyCode) {
		return ((ushort)GetKeyState(keyCode) & 0xffff) != 0;
	}

	public override bool AllocNewConsole() {
		return AllocConsole();
	}

	// ------------------------ //
	// Native things start here //
	// ------------------------ //

	public void SetWindowStyle(RenderWindow window, WS ws, bool enable) {
		IntPtr handle = window.NativeHandle;
		uint currentStyle = GetWindowLong(handle, GWL.STYLE);
		uint uws = (uint)ws;
		if (!enable) {
			uws = ~uws;
		}
		SetWindowLong(handle, GWL.STYLE, currentStyle & uws);
	}

	public void ReplaceWindowStyle(RenderWindow window, WS ws) {
		IntPtr handle = window.NativeHandle;
		SetWindowLong(handle, GWL.STYLE, (uint)ws);
	}

	public void SetWindowExStyle(RenderWindow window, WSEX ws, bool enable) {
		IntPtr handle = window.NativeHandle;
		uint currentStyle = GetWindowLong(handle, GWL.EXSTYLE);
		uint uws = (uint)ws;
		if (!enable) {
			SetWindowLong(handle, GWL.EXSTYLE, currentStyle & ~uws);
		} else {
			SetWindowLong(handle, GWL.EXSTYLE, currentStyle | uws);
		}
	}

	public void ReplaceWindowExStyle(RenderWindow window, WSEX ws) {
		IntPtr handle = window.NativeHandle;
		uint uws = (uint)ws;
		SetWindowLong(handle, GWL.EXSTYLE, uws);
	}

	public void SetPosClientArea(Window window, Vector2u pos, Vector2u size) {
		window.Size = size;
		nint handle = window.NativeHandle;
		WinRect rcClient = new(), rcWind = new();
		GetClientRect(handle, ref rcClient);
		GetWindowRect(handle, ref rcWind);
		Vector2i borderSize = (
			(rcWind.right - rcWind.left) - rcClient.right,
			(rcWind.down - rcWind.up) - rcClient.down
		);
		borderSize /= 2;
		window.Position = -borderSize;
	}

	public void PrintWindowStyles(RenderWindow window) {
		IntPtr handle = window.NativeHandle;
		uint currentStyle = GetWindowLong(handle, GWL.STYLE);
		WS[] wsl = Enum.GetValues(typeof(WS)) as WS[] ?? [];
		wsl = wsl.Distinct().ToArray();
		foreach (WS ws in wsl) {
			if ((currentStyle & (uint)ws) != 0) {
				Console.WriteLine($"{ws} enabled.");
			}
		}
	}

	public bool CheckWindowStyle(RenderWindow window, WS ws) {
		IntPtr handle = window.NativeHandle;
		uint currentStyle = GetWindowLong(handle, GWL.STYLE);
		return (currentStyle & (uint)ws) != 0;
	}

	public bool ShowMessageBox(
		Window? window, string message,
		string caption, int type, bool isFullscreen
	) {
		if (window != null && isFullscreen) {
			window.SetMouseCursorVisible(true);
		}
		int result = MessageBox(nint.Zero, message, caption, 0);
		if (window != null && isFullscreen) {
			window.SetMouseCursorVisible(false);
		}
		return result == 6;
	}

	[DllImport(
		"user32.dll", CharSet = CharSet.Auto,
		ExactSpelling = true,
		CallingConvention = CallingConvention.Winapi)
	]
	private static extern short GetKeyState(int keyCode);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AllocConsole();

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetWindowLong(nint hwnd, GWL index);

	[DllImport("user32.dll")]
	private static extern uint SetWindowLong(nint hwnd, GWL index, uint newStyle);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(nint hWnd, ref WinRect lpRect);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetClientRect(nint hWnd, ref WinRect lpRect);

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
#endif
}