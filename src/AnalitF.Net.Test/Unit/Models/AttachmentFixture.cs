using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class AttachmentFixture
	{
		[Test]
		public void Load_icon()
		{
			var icon = Load("C:\\1.exe");
			Assert.IsNotNull(icon);
		}

		// Constants that we need in the function call
		private const int SHGFI_ICON = 0x100;
		private const int SHGFI_SMALLICON = 0x1;
		private const int SHGFI_LARGEICON = 0x0;

		// This structure will contain information about the file
		public struct SHFILEINFO
		{
			// Handle to the icon representing the file
			public IntPtr hIcon;
			// Index of the icon within the image list
			public int iIcon;
			// Various attributes of the file
			public uint dwAttributes;
			// Path to the file
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szDisplayName;
			// File type
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		[DllImport("Shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

		private object Load(string txt)
		{
			SHFILEINFO shinfo = new SHFILEINFO();
			var hImgLarge = SHGetFileInfo(txt, 0, ref shinfo, Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
			// Get the large icon from the handle
			var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
			// Display the large icon
			//return icon.ToBitmap();
			return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
				new Int32Rect(0, 0, icon.Width, icon.Height),
				BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
		}
	}
}