using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using ReactiveUI;
using NHibernate;
using System.Windows.Documents;
using System.Collections.ObjectModel;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Tags : BaseScreen2, IPrintable
	{
		private PriceTag priceTag;
		private PriceTag rackingMap;
		private Address address;
		private PriceTagSettings priceTagSettings;

		private Tags()
		{
			Lines = new ReactiveCollection<TagPrintable>();
			Session.FlushMode = FlushMode.Never;
			PrintMenuItems = new ObservableCollection<MenuItem>();
			IsView = true;
		}

		public Tags(Address address, List<TagPrintable> lines) : this()
		{
			DisplayName = "Ярлыки";
			Lines.AddRange(lines);
			this.address = address;
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			address = address ?? Address;
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.PriceTag, address))
				.Subscribe(x => priceTag = x);
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.RackingMap, null))
				.Subscribe(x => rackingMap = x);
			this.priceTagSettings = Settings.Value.PriceTags.First(r => r.Address?.Id == address?.Id);
		}

		public ReactiveCollection<TagPrintable> Lines { get; set; }

		public IResult PrintPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ценники",
				Document = new PriceTagDocument(PrintableLines(), priceTagSettings, priceTag).Build()
			}, fullScreen: true);
		}

		public IResult PrintRackingMaps()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Постеллажная карта",
				Document = new RackingMapDocument(PrintableLines(), Settings, rackingMap).Build()
			}, fullScreen: true);
		}

		public IResult PrintBarcode()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Штрихкоды",
				Document = new BarcodeDocument(PrintableLines(), Settings).Build()
			}, fullScreen: true);
		}

		private IList<TagPrintable> PrintableLines()
		{
			return Lines.Where(x => x.Selected).ToList();
		}

		public void SetMenuItems()
		{
			var item = new MenuItem { Header = "Ценники" };
			PrintMenuItems.Add(item);

			item = new MenuItem { Header = "Постеллажная карта" };
			PrintMenuItems.Add(item);

			item = new MenuItem { Header = "Штрихкоды" };
			PrintMenuItems.Add(item);
		}

		PrintResult IPrintable.Print()
		{
			var docs = new List<BaseDocument>();

			if (!IsView) {
				foreach (var item in PrintMenuItems.Where(i => i.IsChecked)) {
					if ((string)item.Header == "Ценники")
						PrintFixedDoc(new PriceTagDocument(PrintableLines(), priceTagSettings, priceTag).Build().DocumentPaginator, "Ценники");
					if ((string)item.Header == "Постеллажная карта")
						PrintFixedDoc(new RackingMapDocument(PrintableLines(), Settings, rackingMap).Build().DocumentPaginator, "Постеллажная карта");
					if ((string)item.Header == "Штрихкоды")
						PrintFixedDoc(new BarcodeDocument(PrintableLines(), Settings).Build().DocumentPaginator, "Штрихкоды");
				}
				return new PrintResult(DisplayName, docs, PrinterName);
			}

			if (string.IsNullOrEmpty(LastOperation) || LastOperation == "Ценники")
				PrintPriceTags().Execute(new ActionExecutionContext());
			if (LastOperation == "Постеллажная карта")
				PrintRackingMaps().Execute(new ActionExecutionContext());
			if (LastOperation == "Штрихкоды")
				PrintBarcode().Execute(new ActionExecutionContext());
			return null;
		}

		private void PrintFixedDoc(DocumentPaginator doc, string name)
		{
			var dialog = new PrintDialog();
			if (!string.IsNullOrEmpty(PrinterName))
			{
				dialog.PrintQueue = new PrintQueue(new PrintServer(), PrinterName);
				dialog.PrintDocument(doc, name);
			}
			else if (dialog.ShowDialog() == true)
				dialog.PrintDocument(doc, name);
		}
		public ObservableCollection<MenuItem> PrintMenuItems { get; set; }
		public string LastOperation { get; set; }
		public string PrinterName { get; set; }
		public bool IsView { get; set; }

		public bool CanPrint
		{
			get { return true; }
		}

	}
}