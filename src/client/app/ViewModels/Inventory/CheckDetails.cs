using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using System.Collections.Generic;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class CheckDetails : BaseScreen2
	{
		private uint id;

		public CheckDetails()
		{
			DisplayName = "Чек";
		}

		public CheckDetails(Check header)
			: this()
		{
			Header.Value = header;
		}

		public NotifyValue<Check> Header { get; set; }
		public NotifyValue<CheckLine> CurrentLine { get; set; }
		public NotifyValue<IList<CheckLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			Lines.Value = Header.Value.Lines;
		}
		public IEnumerable<IResult> PrintCheckDetails()
		{
			return Preview("Чеки", new CheckDetailsDocument(Lines.Value.ToArray(), Header.Value));
		}

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null)
			{
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}
	}
}
