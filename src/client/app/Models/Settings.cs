﻿using System;
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
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
using System.Runtime.Serialization;

namespace AnalitF.Net.Client.Models
{
	public enum DiffCalcMode
	{
		[Description("От предыдущего предложения")] PrevOffer,
		[Description("От минимальной цены")] MinCost,
		[Description("От минимальной цены в основных поставщиках")] MinBaseCost,
	}

	public enum ModePKU
	{
		[Description("Не реагировать на препараты ПКУ")] Resolve,
		[Description("Предупреждать о заказе препаратов ПКУ")] Warning,
		[Description("Запретить заказ препаратов ПКУ")] Deny,
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

		public virtual string FullName => $"{Name}, {Address}";
	}

	public enum ProxyType
	{
		[Description("Использовать настройки системы")] System,
		[Description("Не использовать")] None,
		[Description("Использовать собственные настройки")] User,
	}

	public class Settings : BaseNotify
	{
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
		private int? _barCodePrefix;
		private int? _barCodeSufix;
		private bool _hasMarkupGlobalConfig;

		public Settings(int token = 0, params Address[] addresses)
			: this(addresses)
		{
			MappingToken = token;
		}

		public Settings(params Address[] addresses) : this()
		{
			addresses.SelectMany(MarkupConfig.Defaults).Each(AddMarkup);
			addresses.SelectMany(Models.PriceTagSettings.Defaults).Each(AddPriceTag);
		}

		public Settings(int token = 0) : this()
		{
			MappingToken = token;
		}

		public Settings()
		{
			Rounding = Rounding.To0_10;
			DiffCalcMode = DiffCalcMode.MinCost;
			WarnIfOrderedYesterday = true;
			CountDayForWarnOrdered = 1;
			UseSupplierPriceWithNdsForMarkup = false;
			OverCountWarningFactor = 5;
			OverCostWarningPercent = 5;
			ConfirmDeleteOldOrders = true;
			FreeSale = true;
			DeleteOrdersOlderThan = 35;
			ConfirmDeleteOldWaybills = true;
			DeleteWaybillsOlderThan = 150;
			TrackRejectChangedDays = 90;
			JunkPeriod = 6;
			OpenRejects = true;
			HighlightUnmatchedOrderLines = true;
			RackingMap = new RackingMapSettings();
			PriceTags = new List<PriceTagSettings>();
			Markups = new List<MarkupConfig>();
			Waybills = new List<WaybillSettings>();
			ModePKU = ModePKU.Warning;
			RegistryDoc = new RegistryDocumentSettings();
		}

		public virtual int Id { get; set; }
		public virtual bool TabbedUI { get; set; }

		public virtual bool WarnIfOrderedYesterday { get; set; }
		public virtual bool UseSupplierPriceWithNdsForMarkup { get; set; }
		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool EditAddresses { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual uint TrackRejectChangedDays { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual int CountDayForWarnOrdered { get; set; }

		public virtual decimal OverCountWarningFactor { get; set; }

		public virtual decimal OverCostWarningPercent { get; set; }

		public virtual decimal MaxOverCostOnRestoreOrder { get; set; }

		public virtual DiffCalcMode DiffCalcMode { get; set; }

		public virtual ModePKU ModePKU { get; set; }

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

		//в жизни часы клиента и сервера не будут совпадать
		//обновления по часам клиента
		public virtual DateTime LastSync { get; set; }
		//обновление по часам сервера
		public virtual DateTime ServerLastSync { get; set; }
		public virtual DateTime? LastUpdate { get; set; }

		//дата вычисления лидеров если включена опция отсрочка платежа
		//нужно что бы определить когда нужно пересчитывать лидеров
		//тк отсрочки на каждый день могут быть разные
		public virtual DateTime LastLeaderCalculation { get; set; }

		public virtual bool LookupMarkByProducerCost { get; set; }

		public virtual RackingMapSettings RackingMap { get; set; }

		public virtual IList<PriceTagSettings> PriceTags { get; set; }

		public virtual IList<MarkupConfig> Markups { get; set; }

		public virtual IList<WaybillSettings> Waybills { get; set; }

		public virtual bool ConfirmDeleteOldOrders { get; set; }

		public virtual int DeleteOrdersOlderThan { get; set; }

		public virtual bool ConfirmDeleteOldWaybills { get; set; }

		public virtual bool FreeSale { get; set; }

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
		public virtual bool HasMarkupGlobalConfig
		{
			get { return _hasMarkupGlobalConfig; }
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

		public virtual bool IsValid => !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(UserName);

		public virtual bool IsWaybillDirValid
		{
			get
			{
				try
				{
					Directory.CreateDirectory(WaybillDir);
					return true;
				}
				catch { return false; }
			}
		}

		public virtual bool IsRejectDirValid
		{
			get
			{
				try
				{
					Directory.CreateDirectory(RejectDir);
					return true;
				}
				catch { return false; }
			}
		}

		public virtual bool IsReportDirValid
		{
			get
			{
				try
				{
					Directory.CreateDirectory(ReportDir);
					return true;
				}
				catch { return false; }
			}
		}

		public virtual bool IsDirectoryValid()
		{
			//не проверяем DocDir так как путь данной папки не редактируется в ручную
			if (IsWaybillDirValid && IsRejectDirValid && IsReportDirValid)
			{
				OnPropertyChanged("IsWaybillDirValid");

				OnPropertyChanged("IsRejectDirValid");
				OnPropertyChanged("IsReportDirValid");
				return true;
			}
			OnPropertyChanged("IsWaybillDirValid");
			OnPropertyChanged("IsRejectDirValid");
			OnPropertyChanged("IsReportDirValid");
			return false;

		}

		public virtual WaybillDocumentSettings WaybillDoc { get; set; }
		public virtual WaybillActDocumentSettings WaybillActDoc { get; set; }
		public virtual WaybillProtocolDocumentSettings WaybillProtocolDoc { get; set; }
		public virtual RegistryDocumentSettings RegistryDoc { get; set; }
		//вторая версия токена приложения, хеш guid и путь, первая версия просто guid
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

		public virtual IEnumerable<string> DocumentDirs => new[] {
			MapPath("waybills"),
			MapPath("rejects"),
			MapPath("docs"),
		};

		public virtual bool UseRas { get; set; }

		public virtual string RasConnection { get; set; }

		public virtual string[] RasConnections
		{
			get
			{
				var connections = new List<string>();
				foreach (var path in RasHelper.GetPhoneBooks()) {
					using(var book = new RasPhoneBook()) {
						book.Open(path);
						connections.AddRange(book.Entries.Select(x => x.Name));
					}
				}
				return connections.Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
			}
		}

		public virtual string SbisUsername { get; set;}
		public virtual string SbisPassword { get; set; }
		public virtual string SbisCert { get; set; }

		public virtual string DiadokUsername { get; set;}
		public virtual string DiadokPassword { get; set; }
		public virtual string DiadokCert { get; set; }
		public virtual string DiadokSignerJobTitle { get; set; }

		public virtual string[] DiadokCerts
		{
			get
			{
				var store = new X509Store();
				try {
					store.Open(OpenFlags.ReadOnly);
					return store.Certificates.OfType<X509Certificate2>()
						.Select(x => String.Format("{0} - {1}",
							x.GetNameInfo(X509NameType.SimpleName, false),
							x.GetNameInfo(X509NameType.SimpleName, true)))
						.ToArray();
				}
				finally {
					store.Close();
				}
			}
		}

		public virtual X509Certificate2 GetCert(string name)
		{
			if (String.IsNullOrEmpty(name))
					throw new EndUserError("Сертификат для подписи не настроен.");
			var store = new X509Store();
			try {
				store.Open(OpenFlags.ReadOnly);
				var cert = store.Certificates.OfType<X509Certificate2>()
					.FirstOrDefault(x => String.Format("{0} - {1}",
						x.GetNameInfo(X509NameType.SimpleName, false),
						x.GetNameInfo(X509NameType.SimpleName, true)) == name);
				if (cert == null)
					throw new EndUserError("Сертификат для подписи не найден.");
				return cert;
			}
			finally {
				store.Close();
			}
		}

		public virtual double DebugTimeout { get; set; }
		public virtual bool DebugFault { get; set; }
		public virtual bool DebugLoud { get; set; }
		public virtual bool DebugUseTestSign { get; set; }
		public virtual string DebugDiadokSignerINN { get; set; }

		/// <summary>
		/// Количество месяцев до истечения срока годности когда препараты будут отмечаться как уцененные
		/// </summary>
		public virtual int JunkPeriod { get; set; }

		/// <summary>
		/// Способ округления при расчете цен в накладных
		/// </summary>
		public virtual Rounding Rounding { get; set; }

		public virtual int? BarCodePrefix
		{
			get { return _barCodePrefix; }
			set
			{
				if (_barCodePrefix != value) {
					_barCodePrefix = value;
					OnPropertyChanged();
				}
			}
		}

		public virtual int? BarCodeSufix
		{
			get { return _barCodeSufix; }
			set
			{
				if (_barCodeSufix != value) {
					_barCodeSufix = value;
					OnPropertyChanged();
				}
			}
		}

		public virtual string CheckPrinter { get; set; }

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

		/// <param name="name">
		/// доступные значения - Waybills, Docs, Rejects, Orders, Reports
		/// </param>
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
				prices.Each(p => p.Name = $"{p.SupplierName} {p.PriceName}");
			}
			else {
				var groups = prices.GroupBy(p => p.SupplierId).ToDictionary(g => g.Key, g => g.Count());
				prices.Each(p => {
					if (groups[p.SupplierId] > 1)
						p.Name = $"{p.SupplierName} {p.PriceName}";
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

		public virtual List<string[]> Validate(bool validateMarkups = true)
		{
			if (JunkPeriod < 6) {
				var error = new List<string[]>();
				error.Add( new string[] { "JunkPeriod", "Срок уценки не может быть менее 6 месяцев" });
				return error;
			}
			if (validateMarkups) {
				return MarkupConfig.Validate(Markups);
			}

			return null;
		}

		public virtual void AddPriceTag(PriceTagSettings item)
		{
			if (PriceTags.Contains(item))
				return;

			item.Settings = this;
			PriceTags.Add(item);
		}

		public virtual ICredentials GetCredential()
		{
			return new NetworkCredential(UserName?.Trim(), Password?.Trim());
		}

		public virtual DelegatingHandler[] Handlers()
		{
			if (UseRas)
				return new[] { new RasHandler(RasConnection) };

			return new DelegatingHandler[0];
		}

		public virtual void CheckToken()
		{
			try {
				if (String.IsNullOrEmpty(GetClientToken())) {
					var data = Guid.NewGuid().ToByteArray();
					ClientTokenV2 = Convert.ToBase64String(ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine));
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
				var id = new Guid(ProtectedData.Unprotect(Convert.FromBase64String(ClientTokenV2), null, DataProtectionScope.LocalMachine));
				var tokenString = id + "|" + dir;
				//что бы токен выглядел мистически вычисляем его хеш
				var tokenSource = SHA1.Create().ComputeHash(encoding.GetBytes(tokenString));
				return String.Join("", tokenSource.Select(x => x.ToString("X2")));
			}
			catch(Exception e) {
				log.Error($"Ошибка при получение токена приложения, токен = {ClientTokenV2}", e);
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

		public virtual void SetGlobalMarkupsSettingsForAddress(List<Address> addressList,
			List<MarkupGlobalConfig> markupGlobalConfigList)
		{
			if (markupGlobalConfigList.Count == 0) {
				_hasMarkupGlobalConfig = false;
				return;
			}
			Markups.Clear();
			_hasMarkupGlobalConfig = true;
			foreach (var address in addressList) {
				markupGlobalConfigList.Select(
					s =>
						new MarkupConfig(address, s.Begin, s.End, s.Markup, s.Type) {
							MaxMarkup = s.MaxMarkup,
							MaxSupplierMarkup = s.MaxSupplierMarkup
						})
					.Each(AddMarkup);
			}
		}

		public virtual HttpClient GetHttpClient(Config.Config config,
			ref ProgressMessageHandler progress,
			ref HttpClientHandler handler)
		{
			var version = typeof(AppBootstrapper).Assembly.GetName().Version;
			handler = new HttpClientHandler {
				Credentials = GetCredential(),
				PreAuthenticate = true,
				Proxy = GetProxy()
			};
			if (handler.Credentials == null)
				handler.UseDefaultCredentials = true;
			progress = new ProgressMessageHandler();
			var handlers = Handlers().Concat(new[] { progress }).ToArray();
			var client = HttpClientFactory.Create(handler, handlers);
			client.DefaultRequestHeaders.Add("Version", version.ToString());
			client.DefaultRequestHeaders.Add("Client-Token", GetClientToken());
			var exe = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
			if (exe.Match("AnalitF.Net.Client"))
				client.DefaultRequestHeaders.Add("Branch", "master");
			else if (exe.Match("AnalitF"))
				client.DefaultRequestHeaders.Add("Branch", "migration");
			//признак по которому запросы можно объединить, нужно что бы в интерфейсе связать лог и запрос
			client.DefaultRequestHeaders.Add("Request-Token", Guid.NewGuid().ToString());
			try {
				client.DefaultRequestHeaders.Add("OS-Version", Environment.OSVersion.VersionString);
			} catch (Exception) { }
#if DEBUG
			client.DefaultRequestHeaders.Add("Debug-UserName", UserName);
			if (DebugTimeout > 0)
				client.DefaultRequestHeaders.Add("debug-timeout", DebugTimeout.ToString());
			if (DebugFault)
				client.DefaultRequestHeaders.Add("debug-fault", "true");
#endif
			client.BaseAddress = config.BaseUrl;
			return client;
		}
	}
}