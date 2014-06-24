using System;
using System.Runtime.InteropServices;

namespace AnalitF.Net.Client.Helpers
{
	public class WinApi
	{
		public const uint WM_COMMAND = 0x0111;
		public const uint WM_CLOSE = 0x0010;

		public const int BN_CLICKED = 245;
		public const int IDOK = 1;

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(IntPtr ZeroOnly, string lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className,  string  windowTitle);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern IntPtr SetCapture(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ReleaseCapture(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern IntPtr GetCapture();
	}
}