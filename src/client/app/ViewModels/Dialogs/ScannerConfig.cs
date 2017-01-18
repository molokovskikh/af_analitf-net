using System;
using System.Collections.Generic;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class ScannerConfig : Screen, ICancelable
	{
		public ScannerConfig()
		{
			WasCancelled = true;
			DisplayName = "Тест сканера";
		}

		public NotifyValue<string> Code { get; } = new NotifyValue<string>();
		public int? Prefix { get; set; }
		public int? Sufix { get; set; }
		public bool WasCancelled { get; private set; }
		public NotifyValue<bool> CanOK { get; } = new NotifyValue<bool>();

		public void OK()
		{
			WasCancelled = false;
			TryClose();
		}

		public void Input(IList<string> chars)
		{
			if (chars.Count < 3)
				return;
			if (chars[0][0] < 32) {
				Prefix = chars[0][0];
				chars[0] = "[" + (int)chars[0][0] + "]";
			} else {
				Prefix = null;
			}

			if (chars[chars.Count - 1][0] < 32) {
				Sufix = chars[chars.Count - 1][0];
				chars[chars.Count - 1] = "[" + (int)chars[chars.Count - 1][0] + "]";
			} else {
				Sufix = null;
			}
			CanOK.Value = Prefix != null && Sufix != null;
			Code.Value = String.Concat(chars);
		}
	}
}