using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class MnnViewModel : BaseScreen
	{
		private bool showWithoutOffers;
		private List<Mnn> mnns;

		public MnnViewModel()
		{
			Update();

			this.ObservableForProperty(m => m.ShowWithoutOffers)
				.Subscribe(_ => Update());
		}

		public List<Mnn> Mnns
		{
			get { return mnns; }
			set
			{
				mnns = value;
				RaisePropertyChangedEventImmediately("Mnns");
			}
		}

		public Mnn CurrentMnn { get; set; }

		public bool ShowWithoutOffers
		{
			get { return showWithoutOffers; }
			set
			{
				showWithoutOffers = value;
				RaisePropertyChangedEventImmediately("ShowWithoutOffers");
			}
		}

		public void EnterMnn()
		{
			if (CurrentMnn == null)
				return;

			Shell.ActivateItem(new CatalogViewModel {
				FiltredMnn = CurrentMnn
			});
		}

		public void Update()
		{
			var query = Session.Query<Mnn>();
			if (!ShowWithoutOffers) {
				query = query.Where(m => m.HaveOffers);
			}

			Mnns = query.OrderBy(m => m.Name).ToList();
		}
	}
}