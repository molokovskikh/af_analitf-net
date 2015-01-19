using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Views;
using Common.Tools;
using Common.Tools.Calendar;
using DotRas;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public enum DiffCalcMode
	{
		[Description("От предыдущего предложения")] PrevOffer,
		[Description("От минимальной цены")] MinCost,
		[Description("От минимальной цены в основных поставщиках")] MinBaseCost,
	}

	public enum Taxation
	{
		[Description("ЕНВД")] Envd,
		[Description("НДС")] Nds,
	}

	public class WaybillSettings
	{
		public WaybillSettings()
		{
			IncludeNds = true;
			IncludeNdsForVitallyImportant = true;
			Taxation = Taxation.Envd;
		}

		public WaybillSettings(User user, Address address)
			: this()
		{
			Name = user.FullName;
			Address = address.Name;
			BelongsToAddress = address;
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }
		public virtual string Address { get; set; }
		public virtual string Director { get; set; }
		public virtual string Accountant { get; set; }
		public virtual Address BelongsToAddress { get; set; }
		public virtual Taxation Taxation { get; set; }
		public virtual bool IncludeNds { get; set; }
		public virtual bool IncludeNdsForVitallyImportant { get; set; }

		public virtual string FullName
		{
			get { return String.Format("{0}, {1}", Name, Address); }
		}
	}

	public enum ProxyType
	{
		[Description("Использовать настройки системы")] System,
		[Description("Не использовать")] None,
		[Description("Использовать собственные настройки")] User,
	}

	public class Settings : BaseNotify
	{
		private bool groupWaybillBySupplier;
		private bool _useProxy;

		public Settings(bool defaults, int token = 0) : this()
		{
			if (defaults) {
				foreach (var markup in MarkupConfig.Defaults()) {
					AddMarkup(markup);
				}
			}
			MappingToken = token;
		}

		public Settings()
		{
			UseSupplierPriceWithNdsForMarkup = false;
			OverCountWarningFactor = 5;
			OverCostWarningPercent = 5;
			ConfirmDeleteOldOrders = true;
			DeleteOrdersOlderThan = 35;
			ConfirmDeleteOldWaybills = true;
			DeleteWaybillsOlderThan = 150;
			TrackRejectChangedDays = 90;
			OpenRejects = true;
			HighlightUnmatchedOrderLines = true;
			RackingMap = new RackingMapSettings();
			PriceTag = new PriceTagSettings();
			Markups = new List<MarkupConfig>();
			Waybills = new List<WaybillSettings>();
		}

		public virtual int Id { get; set; }

		public virtual bool UseSupplierPriceWithNdsForMarkup { get; set; }
		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual uint TrackRejectChangedDays { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual decimal OverCountWarningFactor { get; set; }

		public virtual decimal OverCostWarningPercent { get; set; }

		public virtual decimal MaxOverCostOnRestoreOrder { get; set; }

		public virtual DiffCalcMode DiffCalcMode { get; set; }

		public virtual string UserName { get; set; }

		public virtual string Password { get; set; }

		public virtual DateTime? LastUpdate { get; set; }

		//дата вычисления лидеров если включена опция отсрочка платежа
		//нужно что бы определить когда нужно пересчитывать лидеров
		//тк отсрочки на каждый день могут быть разные
		public virtual DateTime LastLeaderCalculation { get; set; }

		public virtual bool LookupMarkByProducerCost { get; set; }

		public virtual RackingMapSettings RackingMap { get; set; }

		public virtual PriceTagSettings PriceTag { get; set; }

		public virtual IList<MarkupConfig> Markups { get; set; }

		public virtual IList<WaybillSettings> Waybills { get; set; }

		public virtual bool ConfirmDeleteOldOrders { get; set; }

		public virtual int DeleteOrdersOlderThan { get; set; }

		public virtual bool ConfirmDeleteOldWaybills { get; set; }

		public virtual int DeleteWaybillsOlderThan { get; set; }

		public virtual bool OpenRejects { get; set; }

		public virtual bool OpenWaybills { get; set; }

		public virtual int MappingToken { get; set; }

		public virtual bool ConfirmSendOrders { get; set; }

		public virtual bool PrintOrdersAfterSend { get; set; }

		public virtual bool HighlightUnmatchedOrderLines { get; set; }

		public virtual bool GroupWaybillsBySupplier
		{
			get { return groupWaybillBySupplier; }
			set
			{
				groupWaybillBySupplier = value;
				OnPropertyChanged();
			}
		}

		public virtual bool UseProxy
		{
			get { return _useProxy; }
			set
			{
				_useProxy = value;
				OnPropertyChanged();
			}
		}

		public virtual string ProxyHost { get; set; }

		public virtual int? ProxyPort { get; set; }

		public virtual string ProxyUserName { get; set; }

		public virtual string ProxyPassword { get; set; }

		public virtual bool IsValid
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(UserName); }
		}

		public virtual WaybillDocumentSettings WaybillDoc { get; set; }
		public virtual RegistryDocumentSettings RegistryDoc { get; set; }


		public virtual IEnumerable<string> DocumentDirs
		{
			get
			{
				return new[] {
					MapPath("waybills"),
					MapPath("rejects"),
					MapPath("docs"),
				};
			}
		}

		public virtual bool UseRas { get; set; }

		public virtual string RasConnection { get; set; }

		public virtual string[] RasConnections
		{
			get
			{
				using(var user = new RasPhoneBook())
				using(var all = new RasPhoneBook()) {
					user.Open(RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User));
					all.Open(RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers));
					return user.Entries.Concat(all.Entries).Select(p => p.Name).Distinct().ToArray();
				}
			}
		}

		public virtual double DebugTimeout { get; set; }
		public virtual bool DebugFault { get; set; }

		public virtual string Ad
		{
			get
			{
				var filename = FileHelper.MakeRooted(@"ads\2block.gif");
				if (File.Exists(filename))
					return filename;
				return "";
			}
		}

		public virtual IWebProxy GetProxy()
		{
			if (!UseProxy)
				return null;
			if (String.IsNullOrEmpty(ProxyHost) || ProxyPort.GetValueOrDefault() <= 0)
				return null;
			var proxy = new WebProxy(ProxyHost, ProxyPort.Value);
			if (!String.IsNullOrEmpty(ProxyUserName))
				proxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPassword);
			return proxy;
		}

		public virtual string MapPath(string name)
		{
			var root = GetVarRoot();
			if (name.Match("Waybills"))
				return Path.Combine(root, "Накладные");
			if (name.Match("Docs"))
				return Path.Combine(root, "Документы");
			if (name.Match("Rejects"))
				return Path.Combine(root, "Отказы");
			return null;
		}

		public virtual string GetVarRoot()
		{
			var root = ConfigurationManager.AppSettings["ClientDocPath"] ??
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

			root = FileHelper.MakeRooted(root);
			root = Path.Combine(root, "АналитФАРМАЦИЯ");
			return root;
		}

		public virtual void ApplyChanges(ISession session)
		{
			var prices = session.Query<Price>().ToList();

			foreach (var price in prices)
				price.BasePrice = price.Category >= BaseFromCategory;

			UpdatePriceNames(prices);
		}

		public virtual void UpdatePriceNames(List<Price> prices)
		{
			if (ShowPriceName) {
				prices.Each(p => p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName));
			}
			else {
				var groups = prices.GroupBy(p => p.SupplierId).ToDictionary(g => g.Key, g => g.Count());
				prices.Each(p => {
					if (groups[p.SupplierId] > 1)
						p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName);
					else
						p.Name = p.SupplierName;
				});
			}
		}

		public virtual string CheckUpdateCondition()
		{
			if (LastUpdate == null)
				return "База данных программы не заполнена. Выполнить обновление?";

			if (LastUpdate < DateTime.Now.AddHours(-8))
				return "Вы работаете с устаревшим набором данных. Выполнить обновление?";

			return null;
		}

		public virtual void AddMarkup(MarkupConfig markup)
		{
			if (Markups.Contains(markup))
				return;

			markup.Settings = this;
			Markups.Add(markup);
			ValidateMarkups();
		}

		public virtual string ValidateMarkups()
		{
			return MarkupConfig.Validate(Markups);
		}

		public virtual ICredentials GetCredential()
		{
			return new NetworkCredential(UserName, Password);
		}

		public virtual DelegatingHandler[] Handlers()
		{
			return new[] {
				new RasHandler(RasConnection),
			};
		}
	}
}