using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Helpers
{
	public class KeyboardHelper
	{
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetKeyboardState(IntPtr lpKeyState);

		[DllImport("user32.dll")]
		private static extern int ToUnicode(uint wVirtKey, uint wScanCode, IntPtr lpKeyState, IntPtr pwszBuff,
			int cchBuff, uint wFlags);

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

	public class BarcodeHandler
	{
		private bool isBarcode;
		private StringBuilder code = new StringBuilder();
		private NotifyValue<Settings> settings;

		public Subject<string> Barcode = new Subject<string>();

		public BarcodeHandler(UserControl control, NotifyValue<Settings> settings)
		{
			this.settings = settings;
			control.PreviewKeyDown += (sender, args) => {
				args.Handled = KeyboardInput(KeyboardHelper.KeyToUnicode(args.Key));
			};
		}

		protected bool KeyboardInput(string key)
		{
			if (string.IsNullOrEmpty(key))
				return false;
			if (settings.Value.BarCodePrefix == null || settings.Value.BarCodeSufix == null)
				return false;

			if (!isBarcode) {
				if (key[0] == settings.Value.BarCodePrefix) {
					isBarcode = true;
					return true;
				}
			} else if (key[0] == settings.Value.BarCodeSufix) {
				Barcode.OnNext(code.ToString());
				code.Clear();
				isBarcode = false;
				return true;
			} else {
				code.Append(key);
				return true;
			}
			return false;
		}
	}
}