using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	class SelectStockPeriod : BaseScreen, ICancelable
	{
		private string _name;

		public SelectStockPeriod(List<Stock> stocks, string name)
		{
			Items = stocks;
			_name = name;

			DisplayName = "Параметры отбора";
			WasCancelled = true;
			Start = DateTime.Now;
			End = DateTime.Now;
		}

		public bool WasCancelled { get; private set; }
		public bool IsStart { get; set; }
		public bool IsEnd { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public List<Stock> Items { get; set; }

		private IEnumerable<IResult> Preview(string name, BaseDocument doc)
		{
			var docSettings = doc.Settings;
			if (docSettings != null)
			{
				yield return new DialogResult(new SimpleSettings(docSettings));
			}
			yield return new DialogResult(new PrintPreviewViewModel(new PrintResult(name, doc)), fullScreen: true);
		}

		public IEnumerable<IResult> OK()
		{
			WasCancelled = false;
			TryClose();

			string title = "Товары со сроком годности ";

			if (IsStart == true && IsEnd == false)
			{
				title += "с " + Start.ToShortDateString();

				var items = Items.Where(s => Convert.ToDateTime(s.Period) >= Start).ToArray();
				return Preview(title, new StockLimitMonthDocument(items, title, _name));
			}
			else if (IsStart == false && IsEnd == true)
			{
				title += "по " + End.ToShortDateString();

				var items = Items.Where(s => Convert.ToDateTime(s.Period) <= End).ToArray();
				return Preview(title, new StockLimitMonthDocument(items, title, _name));
			}
			else if (IsStart == true && IsEnd == true)
			{
				title += "с " + Start.ToShortDateString() + " по " + End.ToShortDateString();
				var items = Items.Where(s => Convert.ToDateTime(s.Period) >= Start && Convert.ToDateTime(s.Period) <= End).ToArray();
				return Preview(title, new StockLimitMonthDocument(items, title, _name));
			}
			return Preview(title, new StockLimitMonthDocument(Items.ToArray(), title, _name));
		}
	}
}
