using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Dialogs;
using Caliburn.Micro;
using ReactiveUI;
using NHibernate;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.ViewModels.Inventory
{
	public class Tags : BaseScreen2
	{
		private PriceTag priceTag;
		private PriceTag rackingMap;

		private Tags()
		{
			Lines = new ReactiveCollection<TagPrintable>();
			Session.FlushMode = FlushMode.Never;
		}

		public Tags(List<TagPrintable> lines) : this()
		{
			DisplayName = "Ярлыки";
			Lines.AddRange(lines);
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.PriceTag))
				.Subscribe(x => priceTag = x);
			RxQuery(s => PriceTag.LoadOrDefault(s.Connection, TagType.RackingMap))
				.Subscribe(x => rackingMap = x);
		}

		public ReactiveCollection<TagPrintable> Lines { get; set; }

		public IResult PrintPriceTags()
		{
			return new DialogResult(new PrintPreviewViewModel
			{
				DisplayName = "Ценники",
				Document = new PriceTagDocument(PrintableLines(), Settings, priceTag).Build()
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
	}
}