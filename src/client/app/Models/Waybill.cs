using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Print;
using Common.Tools;
using log4net;

namespace AnalitF.Net.Client.Models
{
	public class Contractor
	{
		public virtual string Name { get; set; }
		public virtual string Address { get; set; }
		public virtual string Inn { get; set; }
		public virtual string Kpp { get; set; }
	}

	public interface IDataErrorInfo2 : IDataErrorInfo
	{
		string[] FieldsForValidate { get; }
	}

	public enum DocType
	{
		Waybill = 1,
		Reject = 2,
	}

	//цифры важны они используются при миграции настроек
	public enum Rounding
	{
		[Description("не округлять")] None = 0,
		[Description("до 10 коп.")] To0_10 = 1,
		[Description("до 50 коп.")] To0_50 = 2,
		[Description("до 1 руб.")] To1_00 = 3,
	}

	//на текущий момент эта модель может представлять как заголовок накладной так и заголовок отказа
	//однако существует отдельная модель для заголовка отказа
	//что бы можно было создать отдельную модель для общего отображения а модели развести по своим углам
	public class Waybill : BaseStatelessObject, IDataErrorInfo2
	{
		private ILog _log = LogManager.GetLogger(typeof(Waybill));

		private bool _vitallyImportant;

		public Waybill()
		{
			WaybillSettings = new WaybillSettings();
			Lines = new List<WaybillLine>();
		}

		public Waybill(Address address, Supplier supplier)
			: this()
		{
			Address = address;
			Supplier = supplier;
			DocType = Models.DocType.Waybill;
			WriteTime = DateTime.Now;
			DocumentDate = DateTime.Now;
			UserSupplierName = supplier.FullName;
		}

		public Waybill(Address address)
			: this()
		{
				Address = address;
				DocType = Models.DocType.Waybill;
				WriteTime = DateTime.Now;
				DocumentDate = DateTime.Now;
				IsCreatedByUser = true;
		}

		public override uint Id { get; set; }
		public virtual string ProviderDocumentId { get; set; }
		public virtual DateTime DocumentDate { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual Address Address { get; set; }
		public virtual Supplier Supplier { get; set; }
		public virtual DocType? DocType { get; set; }

		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual decimal TaxSum { get; set; }

		public virtual string InvoiceId { get; set; }
		public virtual DateTime? InvoiceDate { get; set; }
		public virtual Contractor Seller { get; set; }
		public virtual Contractor Buyer { get; set; }
		public virtual string ShipperNameAndAddress { get; set; }
		public virtual string ConsigneeNameAndAddress { get; set; }
		public virtual string UserSupplierName { get; set; }
		//накладная перенесена из analitf и при первом вычислении должен применяться особый механизм вычисления цены
		public virtual bool IsMigrated { get; set; }

		/// <summary>
		/// наименование файла в случае если используется механизм переопределения имен файлов
		/// если используется стандартное именование файла тогда null
		/// </summary>
		public virtual string Filename { get; set; }

		//для биндинга
		public virtual bool IsReadOnly => !IsCreatedByUser;

		[Style(Description = "Накладная, созданная пользователем")]
		public virtual bool IsCreatedByUser { get; set; }

		[Style(Description = "Накладная с забраковкой")]
		public virtual bool IsRejectChanged { get; set; }

		[Style(Description = "Накладная с розничной ценой, установленной поставщиком")]
		public virtual bool IsRetailCostFixed { get; set; }

		[Style]
		public virtual bool IsNew { get; set; }

		public virtual IList<WaybillLine> Lines { get; set; }

		public virtual Rounding Rounding => (Settings?.Rounding).GetValueOrDefault(Rounding.To0_10);

		[Ignore]
		public virtual bool VitallyImportant
		{
			get { return _vitallyImportant; }
			set
			{
				if (!CanBeVitallyImportant)
					return;
				if (_vitallyImportant == value)
					return;

				_vitallyImportant = value;
				Recalculate();
				OnPropertyChanged();
			}
		}

		[Ignore]
		public virtual bool CanBeVitallyImportant
		{
			get
			{
				return Lines.All(l => l.VitallyImportant == null);
			}
		}

		public virtual decimal SumWithoutTax => Sum - TaxSum;

		public virtual decimal Markup => Sum > 0 ? Math.Round(MarkupSum / Sum * 100, 2) : 0;

		public virtual decimal MarkupSum => RetailSum - Sum;

		public virtual string Type
		{
			get
			{
				if (DocType.GetValueOrDefault(Models.DocType.Waybill) == Models.DocType.Reject)
					return "Отказ";
				return "Накладная";
			}
		}

		public virtual string SupplierName => SafeSupplier == null ? UserSupplierName : Supplier.FullName;

		public virtual Supplier SafeSupplier
		{
			get
			{
				return NHHelper.IsExists(() => String.IsNullOrEmpty(Supplier?.Name)) ? Supplier : null;
			}
		}

		public virtual string AddressName => IsAddressExists()
			? Address.Name
			: "Адрес отключен/удален из системы";

		[Ignore]
		public virtual WaybillSettings WaybillSettings { get; set; }

		[Ignore]
		public virtual Settings Settings { get; set; }

		[Style("AddressName"), Ignore]
		public virtual bool IsCurrentAddress { get; set; }

		public virtual bool IsAddressExists()
		{
			return NHHelper.IsExists(() => String.IsNullOrEmpty(Address?.Name));
		}

		public virtual void SaveLocalName()
		{
			if (UserSupplierName == null && Supplier != null)
				UserSupplierName = Supplier.FullName;
		}

		public virtual void Calculate(Settings settings, IList<uint> specialMarkupProducts)
		{
			Settings = settings;
			WaybillSettings = settings.Waybills.FirstOrDefault(s => s.BelongsToAddress != null
					&& Address != null
					&& s.BelongsToAddress.Id == Address.Id)
				?? WaybillSettings;
			//специальный механизм должен отработать только один раз
			foreach (var line in Lines)
				line.SpecialMarkUp = specialMarkupProducts.Contains(line.ProductId.GetValueOrDefault());

			Recalculate();
		}

		public virtual void Recalculate()
		{
			if (Settings == null)
				return;
			var isMigrated = IsMigrated && Sum == 0;
			foreach (var line in Lines) {
				if (isMigrated)
					line.CalculateForMigrated(Settings);
				else
					line.Calculate(Settings);
			}

			Sum = Lines.Sum(l => l.SupplierCost*l.Quantity).GetValueOrDefault();
			CalculateRetailSum();
		}

		public virtual void CalculateRetailSum()
		{
			RetailSum = Lines.Sum(l => l.RetailCost * l.Quantity).GetValueOrDefault();
			if (IsCreatedByUser) {
				TaxSum = Lines.Sum(l => l.NdsAmount).GetValueOrDefault();
				Sum = Lines.Sum(l => l.SupplierCost * l.Quantity).GetValueOrDefault();
			}
		}

		public virtual void DeleteFiles(Settings settings)
		{
			try {
				if (String.IsNullOrEmpty(Filename)) {
					var files = Directory.GetFiles(settings.MapPath("Waybills"), Id + "_*");
					files.Each(f => File.Delete(f));
				}
				else {
					File.Delete(Path.Combine(settings.MapPath("Waybills"), Filename));
				}
			}
			catch(Exception e) {
				_log.Warn($"Ошибка при удалении документа {Id}", e);
			}
		}

		public virtual void RemoveLine(WaybillLine line)
		{
			line.Waybill = null;
			Lines.Remove(line);
			Recalculate();
		}

		public virtual void AddLine(WaybillLine line)
		{
			line.Waybill = this;
			Lines.Add(line);
			Recalculate();
		}

		public virtual string this[string columnName]
		{
			get
			{
				if (!IsCreatedByUser)
					return "";
				if (columnName == "ProviderDocumentId") {
					if (String.IsNullOrEmpty(ProviderDocumentId)) {
						return "Не установлен номер накладной.";
					}
				}
				else if (columnName == "UserSupplierName") {
					if (String.IsNullOrEmpty(UserSupplierName)) {
						return "Не установлено название поставщика";
					}
				}
				return "";
			}
		}

		public virtual string Error { get; protected set; }

		public virtual string[] FieldsForValidate => new[] { "ProviderDocumentId", "UserSupplierName" };

		public virtual WaybillDocumentSettings GetWaybillDocSettings()
		{
			if (Settings.WaybillDoc == null)
				Settings.WaybillDoc = new WaybillDocumentSettings();
			return Settings.WaybillDoc.Setup(this);
		}

		public virtual RegistryDocumentSettings GetRegistryDocSettings()
		{
			if (Settings.RegistryDoc == null)
				Settings.RegistryDoc = new RegistryDocumentSettings();
			return Settings.RegistryDoc.Setup(this);
		}

		public virtual void CalculateStyle(Address address)
		{
			IsCurrentAddress = IsAddressExists() && Address.Id == address.Id;
		}

		public virtual string TryGetFile(Settings settings)
		{
			var path = settings.MapPath("Waybills");
			if (!Directory.Exists(path))
				return null;
			return new DirectoryInfo(path).GetFiles($"{Id}_*")
				.Select(x => x.FullName).FirstOrDefault();
		}
	}
}