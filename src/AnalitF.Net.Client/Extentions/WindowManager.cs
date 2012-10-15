using System;
using System.Collections.Generic;
using System.Windows;

namespace AnalitF.Net.Client.Extentions
{
	public class WindowManager : Caliburn.Micro.WindowManager
	{
		public bool UnderTest;
		public List<Window> Windows = new List<Window>();

		public override bool? ShowDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			if (UnderTest) {
				Windows.Add(CreateWindow(rootModel, true, context, settings));
				return true;
			}
			return base.ShowDialog(rootModel, context, settings);
		}

		protected override Window EnsureWindow(object model, object view, bool isDialog)
		{
			var window = base.EnsureWindow(model, view, isDialog);
			if (isDialog && window.Owner != null) {
				window.SizeToContent = SizeToContent.Manual;
				window.Height = window.Owner.Height * 2 / 3;
				window.Width = window.Owner.Width * 2 / 3;
			}
			return window;
		}

		public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
		{
			if (UnderTest) {
				return MessageBoxResult.OK;
			}

			return MessageBox.Show(text, caption, buttons, icon);
		}
	}
}