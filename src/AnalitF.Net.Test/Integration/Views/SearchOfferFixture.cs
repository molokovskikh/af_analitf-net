﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.Views;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Views
{
	[TestFixture, RequiresSTA]
	public class SearchOfferFixture : BaseViewFixture
	{
		[Test]
		public void Check_view()
		{
			var model = Init(new SearchOfferViewModel());
			var view = InitView<SearchOfferView>(model);

			var offer = session.Query<Offer>().First();
			model.SearchText = offer.ProductSynonym.Slice(3);
			model.Search();

			ForceBinding(view);
		}
	}
}