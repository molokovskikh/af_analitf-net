using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AnalitF.Net.Client.Helpers
{
	public class KeyboardHelper
	{
		private const int RIDEV_INPUTSINK = 0x00000100;
		private const int RIDEV_REMOVE = 0x00000001;
		private const int RID_INPUT = 0x10000003;

		private const int FAPPCOMMAND_MASK = 0xF000;
		private const int FAPPCOMMAND_MOUSE = 0x8000;
		private const int FAPPCOMMAND_OEM = 0x1000;

		private const int RIM_TYPEMOUSE = 0;
		private const int RIM_TYPEKEYBOARD = 1;
		private const int RIM_TYPEHID = 2;

		private const int RIDI_DEVICENAME = 0x20000007;

		private const int WM_KEYUP = 0x0101;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_SYSKEYDOWN = 0x0104;
		private const int WM_INPUT = 0x00FF;
		private const int VK_OEM_CLEAR = 0xFE;
		private const int VK_LAST_KEY = VK_OEM_CLEAR; // this is a made up value used as a sentinal

		private const int PM_REMOVE = 0x01;

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICELIST
		{
			public readonly IntPtr hDevice;

			[MarshalAs(UnmanagedType.U4)] public readonly int dwType;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct RAWINPUT
		{
			[FieldOffset(0)] public RAWINPUTHEADER header;

			[FieldOffset(16)] public readonly RAWMOUSE mouse;

			[FieldOffset(16)] public RAWKEYBOARD keyboard;

			[FieldOffset(16)] public readonly RAWHID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTHEADER
		{
			[MarshalAs(UnmanagedType.U4)] public readonly int dwType;

			[MarshalAs(UnmanagedType.U4)] public readonly int dwSize;

			public readonly IntPtr hDevice;

			[MarshalAs(UnmanagedType.U4)] public readonly int wParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWHID
		{
			[MarshalAs(UnmanagedType.U4)] public readonly int dwSizHid;

			[MarshalAs(UnmanagedType.U4)] public readonly int dwCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BUTTONSSTR
		{
			[MarshalAs(UnmanagedType.U2)] public readonly ushort usButtonFlags;

			[MarshalAs(UnmanagedType.U2)] public readonly ushort usButtonData;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct RAWMOUSE
		{
			[MarshalAs(UnmanagedType.U2)] [FieldOffset(0)] public readonly ushort usFlags;

			[MarshalAs(UnmanagedType.U4)] [FieldOffset(4)] public readonly uint ulButtons;

			[FieldOffset(4)] public readonly BUTTONSSTR buttonsStr;

			[MarshalAs(UnmanagedType.U4)] [FieldOffset(8)] public readonly uint ulRawButtons;

			[FieldOffset(12)] public readonly int lLastX;

			[FieldOffset(16)] public readonly int lLastY;

			[MarshalAs(UnmanagedType.U4)] [FieldOffset(20)] public readonly uint ulExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWKEYBOARD
		{
			[MarshalAs(UnmanagedType.U2)] public readonly ushort MakeCode;

			[MarshalAs(UnmanagedType.U2)] public readonly ushort Flags;

			[MarshalAs(UnmanagedType.U2)] public readonly ushort Reserved;

			[MarshalAs(UnmanagedType.U2)] public readonly ushort VKey;

			[MarshalAs(UnmanagedType.U4)] public readonly uint Message;

			[MarshalAs(UnmanagedType.U4)] public readonly uint ExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICE
		{
			[MarshalAs(UnmanagedType.U2)] public ushort usUsagePage;

			[MarshalAs(UnmanagedType.U2)] public ushort usUsage;

			[MarshalAs(UnmanagedType.U4)] public int dwFlags;

			public IntPtr hwndTarget;
		}

		[DllImport("User32.dll")]
		private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);

		[DllImport("User32.dll")]
		private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

		[DllImport("User32.dll")]
		private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

		[DllImport("User32.dll")]
		private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize,
			uint cbSizeHeader);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetKeyboardState(IntPtr lpKeyState);

		[DllImport("user32.dll")]
		private static extern int ToUnicode(uint wVirtKey, uint wScanCode, IntPtr lpKeyState, IntPtr pwszBuff,
			int cchBuff, uint wFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool PeekMessage(out MSG lpmsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax,
			uint wRemoveMsg);

		const uint MAPVK_VK_TO_VSC = 0x00;
		const uint MAPVK_VSC_TO_VK = 0x01;
		const uint MAPVK_VK_TO_CHAR = 0x02;
		const uint MAPVK_VSC_TO_VK_EX = 0x03;
		const uint MAPVK_VK_TO_VSC_EX = 0x04;

		[DllImport("user32.dll")]
		static extern uint MapVirtualKey(uint uCode, uint uMapType);

		public static string KeyToUnicode(Key key)
		{
			var mLocalBuffer = IntPtr.Zero;
			var mKeyboardState = IntPtr.Zero;
			try {
				mKeyboardState = Marshal.AllocHGlobal(256);
				mLocalBuffer = Marshal.AllocHGlobal(129);
				var vkey = (uint)KeyInterop.VirtualKeyFromKey(key);
				var scanCode = MapVirtualKey(vkey, MAPVK_VK_TO_VSC);
				if (GetKeyboardState(mKeyboardState)) {
					var output = ToUnicode(vkey, scanCode, mKeyboardState, mLocalBuffer, 64, 0);
					return Marshal.PtrToStringUni(mLocalBuffer, output);
				}
				return null;
			} finally {
				if (mLocalBuffer != IntPtr.Zero)
					Marshal.FreeHGlobal(mLocalBuffer);
				if (mKeyboardState != IntPtr.Zero)
					Marshal.FreeHGlobal(mKeyboardState);
			}
		}
	}
}