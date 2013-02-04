using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Documents;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class PriceOfferViewModel : BaseOfferViewModel, IPrintable
	{
		private string[] filters = new[] {
			"Прайс-лист (F4)",
			"Заказы (F5)",
			"Лучшие предложения (F6)",
		};

		private string currentFilter;
		private string activeSearchTerm;

		public PriceOfferViewModel(Price price, bool showLeaders)
		{
			SearchText = new NotifyValue<string>();
			DisplayName = "Заявка поставщику";
			Price = price;

			Filters = filters;
			currentProducer = Consts.AllProducerLabel;
			currentFilter = filters[0];
			if (showLeaders)
				currentFilter = filters[2];

			this.ObservableForProperty(m => m.CurrentFilter)
				.Merge(this.ObservableForProperty(m => m.CurrentProducer))
				.Subscribe(e => Update());
		}

		public Price Price { get; set; }

		public string[] Filters { get; set; }

		public string ActiveSearchTerm
		{
			get { return activeSearchTerm; }
			set
			{
				activeSearchTerm = value;
				NotifyOfPropertyChange("ActiveSearchTerm");
			}
		}

		public string CurrentFilter
		{
			get { return currentFilter; }
			set
			{
				currentFilter = value;
				NotifyOfPropertyChange("CurrentFilter");
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();

			Update();
			UpdateProducers();
		}

		protected override void Query()
		{
			var query = StatelessSession.Query<Offer>().Where(o => o.Price.Id == Price.Id);
			if (CurrentProducer != Consts.AllProducerLabel) {
				query = query.Where(o => o.Producer == CurrentProducer);
			}
			if (CurrentFilter == filters[2]) {
				query = query.Where(o => o.LeaderPrice == Price);
			}
			if (currentFilter == filters[1]) {
				query = query.Where(o => o.OrderLine != null);
			}
			if (!String.IsNullOrEmpty(ActiveSearchTerm)) {
				query = query.Where(o => o.ProductSynonym.Contains(ActiveSearchTerm));
			}

			Offers = query.Fetch(o => o.Price).ToList();
			CurrentOffer = offers.FirstOrDefault();
		}

		public NotifyValue<string> SearchText { get; set; }

		public IResult Search()
		{
			if (string.IsNullOrEmpty(SearchText.Value) || SearchText.Value.Length < 3) {
				return HandledResult.Skip();
			}

			ActiveSearchTerm = SearchText;
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public IResult ClearSearch()
		{
			if (String.IsNullOrEmpty(ActiveSearchTerm))
				return HandledResult.Skip();

			ActiveSearchTerm = "";
			SearchText.Value = "";
			Update();
			return HandledResult.Handled();
		}

		public bool CanPrint
		{
			get { return true; }
		}

		public PrintResult Print()
		{
			var doc = new PriceOfferDocument(offers, Price, Address).BuildDocument();
			return new PrintResult(doc, DisplayName);
		}
	}
}