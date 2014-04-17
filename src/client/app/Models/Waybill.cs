using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class Contractor
	{
		public virtual string Name { get; set; }
		public virtual string Address { get; set; }
		public virtual string Inn { get; set; }
		public virtual string Kpp { get; set; }
	}

	public class Waybill : BaseStatelessObject, IDataErrorInfo
	{
		private log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Waybill));

		private bool _vitallyImportant;

		public Waybill()
		{
			WaybillSettings = new WaybillSettings();
			Lines = new List<WaybillLine>();
			RoundTo1 = true;
		}

		public Waybill(Address address, Supplier supplier)
			: this()
		{
			Address = address;
			Supplier = supplier;
			WriteTime = DateTime.Now;
			DocumentDate = DateTime.Now;
		}

		public override uint Id { get; set; }
		public virtual string ProviderDocumentId { get; set; }
		public virtual DateTime DocumentDate { get; set; }
		public virtual DateTime WriteTime { get; set; }
		public virtual Address Address { get; set; }
		public virtual Supplier Supplier { get; set; }

		public virtual decimal Sum { get; set; }
		public virtual decimal RetailSum { get; set; }
		public virtual decimal TaxSum { get; set; }

		public virtual Contractor Seller { get; set; }
		public virtual Contractor Buyer { get; set; }
		public virtual string ShipperNameAndAddress { get; set; }
		public virtual string ConsigneeNameAndAddress { get; set; }
		public virtual string UserSupplierName { get; set; }

		//для биндинга
		public virtual bool IsReadOnly
		{
			get { return !IsCreatedByUser; }
		}

		[Style(Description = "Накладная, созданная пользователем")]
		public virtual bool IsCreatedByUser { get; set; }

		[Style(Description = "Накладная с забраковкой")]
		public virtual bool IsRejectChanged { get; set; }

		public virtual IList<WaybillLine> Lines { get; set; }

		[Ignore]
		public virtual bool RoundTo1 { get; set; }

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
				Calculate(Settings);
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

		public virtual decimal SumWithoutTax
		{
			get { return Sum - TaxSum; }
		}

		public virtual decimal Markup
		{
			get { return Sum > 0 ? Math.Round(MarkupSum / Sum * 100, 2) : 0; }
		}

		public virtual decimal MarkupSum
		{
			get { return RetailSum - Sum; }
		}

		public virtual string Type
		{
			get { return "Накладная"; }
		}

		public virtual string SupplierName
		{
			get { return Supplier == null ? UserSupplierName : Supplier.FullName; }
		}

		[Ignore]
		public virtual WaybillSettings WaybillSettings { get; set; }

		[Ignore]
		public virtual Settings Settings { get; set; }

		public virtual void Calculate(Settings settings)
		{
			if (settings == null)
				return;
			Settings = settings;
			WaybillSettings = settings.Waybills
				.FirstOrDefault(s => s.BelongsToAddress != null
					&& Address != null
					&& s.BelongsToAddress.Id == Address.Id)
				?? WaybillSettings;

			foreach (var waybillLine in Lines)
				waybillLine.Calculate(Settings, Settings.Markups);

			Sum = Lines.Sum(l => l.SupplierCost * l.Quantity).GetValueOrDefault();
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
			var files = Directory.GetFiles(settings.MapPath("Waybills"), Id + "_*");
			try {
				files.Each(f => File.Delete(f));
			}
			catch(Exception e) {
				_log.Warn(String.Format("Ошибка при удалении документа {0}", Id), e);
			}
		}

		public virtual void RemoveLine(WaybillLine line)
		{
			line.Waybill = null;
			Lines.Remove(line);
			Calculate(Settings);
		}

		public virtual void AddLine(WaybillLine line)
		{
			line.Waybill = this;
			Lines.Add(line);
			Calculate(Settings);
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
	}
}