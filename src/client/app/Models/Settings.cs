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
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Views;
using Common.Tools;
using Common.Tools.Calendar;
using DotRas;
using log4net;
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
		public static MemoryStream ImageCache;

		private ILog log = LogManager.GetLogger(typeof(Settings));
		private bool groupWaybillBySupplier;
		private bool _useProxy;
		private string _waybillDir;
		private string _reportDir;
		private string _rejectDir;
		private string _docDir;
		private string _userName;
		private string _password;
		private string _proxyUserName;
		private string _proxyPassword;
		private string _proxyHost;

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
			DiffCalcMode = DiffCalcMode.MinCost;
			WarnIfOrderedYesterday = true;
			UseSupplierPriceWithNdsForMarkup = false;
			OverCountWarningFactor = 5;
			OverCostWarningPercent = 5;
			ConfirmDeleteOldOrders = true;
			DeleteOrdersOlderThan = 35;
			ConfirmDeleteOldWaybills = true;
			DeleteWaybillsOlderThan = 150;
			TrackRejectChangedDays = 90;
			JunkPeriod = 6;
			OpenRejects = true;
			HighlightUnmatchedOrderLines = true;
			RackingMap = new RackingMapSettings();
			PriceTag = new PriceTagSettings();
			Markups = new List<MarkupConfig>();
			Waybills = new List<WaybillSettings>();
		}

		public virtual int Id { get; set; }

		public virtual bool WarnIfOrderedYesterday { get; set; }
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

		public virtual string UserName
		{
			get { return _userName; }
			set { _userName = value?.Trim(); }
		}

		public virtual string Password
		{
			get { return _password; }
			set { _password = value?.Trim(); }
		}

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

		public virtual string ProxyHost
		{
			get { return _proxyHost; }
			set { _proxyHost = value?.Trim(); }
		}

		public virtual int? ProxyPort { get; set; }

		public virtual string ProxyUserName
		{
			get { return _proxyUserName; }
			set { _proxyUserName = value?.Trim(); }
		}

		public virtual string ProxyPassword
		{
			get { return _proxyPassword; }
			set { _proxyPassword = value?.Trim(); }
		}

		public virtual bool IsValid
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(UserName); }
		}

		public virtual WaybillDocumentSettings WaybillDoc { get; set; }
		public virtual RegistryDocumentSettings RegistryDoc { get; set; }
		//врорая версия токена приложения, хеш guid и путь, первая версия просто guid
		public virtual string ClientTokenV2 { get; set; }

		public virtual string DocDir
		{
			get { return _docDir; }
			set
			{
				_docDir = value;
				OnPropertyChanged();
			}
		}

		public virtual string WaybillDir
		{
			get { return _waybillDir; }
			set
			{
				_waybillDir = value;
				OnPropertyChanged();
			}
		}

		public virtual string RejectDir
		{
			get { return _rejectDir; }
			set
			{
				_rejectDir = value;
				OnPropertyChanged();
			}
		}

		public virtual string ReportDir
		{
			get { return _reportDir; }
			set
			{
				_reportDir = value;
				OnPropertyChanged();
			}
		}

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

		public virtual Stream AdStream
		{
			get
			{
				var file = Ad;
				if (String.IsNullOrEmpty(file))
					return null;
				if (ImageCache != null)
					return ImageCache;
				return ImageCache = new MemoryStream(File.ReadAllBytes(file));
			}
		}

		/// <summary>
		/// Количество месяцев до истечения срока годности когда препараты будут отмечаться как уцененные
		/// </summary>
		public virtual int JunkPeriod { get; set; }

		public virtual IWebProxy GetProxy()
		{
			if (!UseProxy)
				return null;
			if (String.IsNullOrEmpty(ProxyHost) || ProxyPort.GetValueOrDefault() <= 0)
				return null;
			var proxy = new WebProxy(ProxyHost?.Trim(), ProxyPort.GetValueOrDefault());
			if (!String.IsNullOrEmpty(ProxyUserName))
				proxy.Credentials = new NetworkCredential(ProxyUserName?.Trim(), ProxyPassword?.Trim());
			return proxy;
		}

		public virtual string MapPath(string name)
		{
			var root = GetVarRoot();
			if (name.Match("Waybills"))
				return String.IsNullOrEmpty(WaybillDir) ? Path.Combine(root, "Накладные") : WaybillDir;

			if (name.Match("Docs"))
				return String.IsNullOrEmpty(DocDir) ? Path.Combine(root, "Документы") : DocDir;

			if (name.Match("Rejects"))
				return String.IsNullOrEmpty(RejectDir) ? Path.Combine(root, "Отказы") : RejectDir;

			if (name.Match("Orders"))
				return Path.Combine(root, "Накладные", "Заявки");

			if (name.Match("Reports"))
				return String.IsNullOrEmpty(ReportDir) ? Path.Combine(root, "Отчеты") : ReportDir;
			return null;
		}

		public virtual string InitAndMap(string name)
		{
			var dir = MapPath(name);
			Directory.CreateDirectory(dir);
			return dir;
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
			Validate();
		}

		public virtual string Validate()
		{
			if (JunkPeriod < 6)
				return "Срок уценки не может быть менее 6 месяцев";

			return MarkupConfig.Validate(Markups);
		}

		public virtual ICredentials GetCredential()
		{
			return new NetworkCredential(UserName?.Trim(), Password?.Trim());
		}

		public virtual DelegatingHandler[] Handlers()
		{
			return new[] {
				new RasHandler(RasConnection),
			};
		}

		public virtual void CheckToken()
		{
			try {
				if (String.IsNullOrEmpty(GetClientToken())) {
					var data = Guid.NewGuid().ToByteArray();
					ClientTokenV2 = Convert.ToBase64String(ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser));
				}
			}
			catch(Exception e) {
				log.Error("Не удалось сгенерировать токен приложения", e);
			}
		}

		public virtual string GetClientToken(string dir = null)
		{
			try {
				dir = dir ?? Path.GetDirectoryName(typeof(Settings).Assembly.Location);
				if (String.IsNullOrEmpty(ClientTokenV2))
					return "";
				var encoding = Encoding.UTF8;
				var id = new Guid(ProtectedData.Unprotect(Convert.FromBase64String(ClientTokenV2), null, DataProtectionScope.CurrentUser));
				var tokenString = id + "|" + dir;
				//что бы токен выглядел мистически вычисляем его хеш
				var tokenSource = SHA1.Create().ComputeHash(encoding.GetBytes(tokenString));
				return String.Join("", tokenSource.Select(x => x.ToString("X2")));
			}
			catch(Exception e) {
				log.Error(String.Format("Ошибка при получение токена приложения, токен = {0}", ClientTokenV2), e);
				return null;
			}
		}

		public virtual void CopyMarkups(Address src, Address dst)
		{
			if (Markups.Any(x => x.Address == dst))
				return;
			Markups.AddEach(Markups
				.Where(x => x.Address == src)
				.Select(x => new MarkupConfig(x, dst)));
		}
	}
}