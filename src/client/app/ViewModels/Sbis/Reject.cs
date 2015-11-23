using System;
using AnalitF.Net.Client.Models.Results;

namespace AnalitF.Net.Client.ViewModels.Sbis
{
	public class Reject : BaseScreen, ICancelable
	{
		public Reject()
		{
			WasCancelled = true;
		}

		public string Comment {get; set; }

		public void Save()
		{
			if (String.IsNullOrWhiteSpace(Comment)) {
				Manager.Error("Нужно указать причину.");
				return;
			}
			WasCancelled = false;
			TryClose();
		}

		public bool WasCancelled { get; set; }
	}
}