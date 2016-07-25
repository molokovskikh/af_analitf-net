using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Inventory;
using NHibernate.Linq;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	class Checks : BaseScreen2
	{
		private Main main;

		public Checks()
		{
			Begin.Value = DateTime.Today.AddDays(-7);
			End.Value = DateTime.Today;
			Items = new NotifyValue<IList<Check>>(new List<Check>());
		}

		public Checks(Main main)
			: this()
		{
			this.main = main;
		}

		public NotifyValue<DateTime> Begin { get; set; }
		public NotifyValue<DateTime> End { get; set; }
		public NotifyValue<IList<Check>> Items { get; set; }
		public NotifyValue<Check> CurrentItem { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			TempFillItemsList();
		}
		public void EnterItem()
		{
			if (CurrentItem.Value == null)
				return;
			main.ActiveItem = new CheckDetails(CurrentItem.Value);
		}

		private void TempFillItemsList()
		{
			var check = new Check(0);
			check.Lines = new List<CheckLine>();
			check.Lines.Add(new CheckLine());
			Items.Value.Add(check);
		}

		public IEnumerable<IResult> PrintChecks()
		{
			return Preview("Чеки", new CheckDocument(Items.Value.ToArray()));
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
