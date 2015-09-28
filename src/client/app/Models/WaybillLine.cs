using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class CertificateFile
	{
		public CertificateFile()
		{
		}

		public CertificateFile(string localFileName)
		{
			LocalFileName = localFileName;
		}

		public virtual uint Id { get; set; }

		public virtual string LocalFileName { get; set; }
	}

	public class WaybillLine : Loadable, IEditableObject
	{
		private decimal? _retailCost;
		private decimal? _realRetailMarkup;
		private decimal? _retailMarkup;
		private decimal? _maxRetailMarkup;
		private decimal? _maxSupplierMarkup;
		private bool _print;
		private Waybill _waybill;
		private bool isCertificateNotFound;

		public WaybillLine()
		{
			Print = true;
			CertificateFiles = new List<CertificateFile>();
		}

		public WaybillLine(Waybill waybill)
			: this()
		{
			Waybill = waybill;
		}

		public virtual uint Id { get; set; }

		public virtual Waybill Waybill
		{
			get { return _waybill; }
			set
			{
				if (_waybill == value)
					return;
				if (value != null)
					value.PropertyChanged += WaybillChanged;
				if (_waybill != null)
					_waybill.PropertyChanged -= WaybillChanged;
				_waybill = value;
			}
		}

		public virtual uint? ProductId { get; set; }
		public virtual string Product { get; set; }
		public virtual uint? ProducerId { get; set; }
		public virtual string Producer { get; set; }

		public virtual string CountryCode { get; set; }
		public virtual string Country { get; set; }

		public virtual bool Print
		{
			get { return _print; }
			set
			{
				_print = value;
				OnPropertyChanged();
			}
		}

		public virtual string Period { get; set; }
		public virtual DateTime? Exp { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }

		public virtual string Unit { get; set; }
		public virtual decimal? ExciseTax { get; set; }
		public virtual string BillOfEntryNumber { get; set; }

		public virtual bool? VitallyImportant { get; set; }

		public virtual decimal? ProducerCost { get; set; }
		public virtual decimal? RegistryCost { get; set; }

		public virtual decimal? SupplierPriceMarkup { get; set; }
		public virtual decimal? SupplierCostWithoutNds { get; set; }
		public virtual decimal? SupplierCost { get; set; }

		public virtual int? Nds { get; set; }
		public virtual decimal? NdsAmount { get; set; }

		public virtual decimal? Amount { get; set; }

		public virtual int? Quantity { get; set; }

		public virtual string EAN13 { get; set; }

		public virtual bool Edited { get; set; }

		public virtual IList<CertificateFile> CertificateFiles { get; set; }

		public virtual decimal? MaxRetailMarkup
		{
			get { return _maxRetailMarkup; }
			set
			{
				_maxRetailMarkup = value;
				OnPropertyChanged();
			}
		}

		public virtual decimal? RetailMarkup
		{
			get { return _retailMarkup; }
			set
			{
				if (_realRetailMarkup == value)
					return;

				Edited = true;
				_retailMarkup = value;
				RecalculateFromRetailMarkup();
				Waybill.CalculateRetailSum();
			}
		}

		public virtual decimal? RealRetailMarkup
		{
			get { return _realRetailMarkup; }
			set
			{
				if (_realRetailMarkup == value)
					return;

				Edited = true;
				_realRetailMarkup = value;
				RecalculateFromRealRetailMarkup();
				Waybill.CalculateRetailSum();
			}
		}

		public virtual decimal? RetailCost
		{
			get { return _retailCost; }
			set
			{
				if (_retailCost == value)
					return;

				Edited = true;
				_retailCost = value;
				RecalculateMarkups(RetailCost);
				Waybill.CalculateRetailSum();
				OnPropertyChanged("RetailSum");
				OnPropertyChanged();
			}
		}

		public virtual uint? RejectId { get; set; }

		[Style(Description = "Сертификат не был найден", Columns = new[] { "CertificateLink" })]
		public virtual bool IsCertificateNotFound
		{
			get { return isCertificateNotFound; }
			set
			{
				isCertificateNotFound = value;
				OnPropertyChanged();
			}
		}

		[Style(Description = "Новая разбракованная позиция")]
		public virtual bool IsRejectCanceled { get; set; }

		[Style(Description = "Новая забракованная позиция")]
		public virtual bool IsRejectNew { get; set; }

		[Style(Description = "Забракованная позиция")]
		public virtual bool IsReject => !IsRejectNew && RejectId != null;

		[Style("Nds", Description = "НДС: не установлен для ЖНВЛС")]
		public virtual bool IsNdsInvalid => ActualVitallyImportant && Nds.GetValueOrDefault(10) != 10;

		[Style("RetailMarkup", "MaxRetailMarkup",
			Description = "Розничная наценка: превышение максимальной розничной наценки")]
		public virtual bool IsMarkupToBig => RetailMarkup > MaxRetailMarkup;

		[Style("RetailMarkup", "RetailCost", "RetailSum", "RealRetailMarkup",
			Description = "Розничная цена: не рассчитана")]
		public virtual bool IsMarkupInvalid => RetailMarkup == null;

		[Style("SupplierPriceMarkup", Description = "Торговая наценка оптовика: превышение наценки оптовика")]
		public virtual bool IsSupplierPriceMarkupInvalid => SupplierPriceMarkup > _maxSupplierMarkup;

		public virtual decimal? RetailSum => Quantity * RetailCost;

		public virtual decimal? AmountExcludeTax => Amount - NdsAmount;

		[Style(Name = "VitallyImportant")]
		public virtual bool ActualVitallyImportant => (Waybill?.VitallyImportant).GetValueOrDefault()
			|| VitallyImportant.GetValueOrDefault();

		public virtual decimal? ProducerCostWithTax => ProducerCost * (1 + (decimal?) Nds / 100);

		[Ignore]
		public virtual bool IsMigrate { get; set; }

		private decimal TaxFactor
		{
			get
			{
				var nds = Nds.GetValueOrDefault(10);
				if (Waybill.WaybillSettings.Taxation == Taxation.Envd
					&& ((ActualVitallyImportant && !Waybill.WaybillSettings.IncludeNdsForVitallyImportant)
						|| !ActualVitallyImportant && !Waybill.WaybillSettings.IncludeNds)) {
					nds = 0;
				}
				return (1 + nds / 100m);
			}
		}

		public virtual decimal? TaxPerUnit => SupplierCost - SupplierCostWithoutNds;

		public virtual string Details
		{
			get
			{
				if (IsDownloaded)
					return "Нажмите что бы открыть";
				if (IsDownloading)
					return "Нажмите для отмены";
				if (IsCertificateNotFound)
					return "Сертификат не найден, нажмите что бы повторить поиск";
				if (IsError)
					return ErrorDetails;
				return "Нажмите для загрузки сертификатов";
			}
		}

		public override uint GetId()
		{
			return Id;
		}

		public override string GetLocalFilename(string archEntryName, Config.Config config)
		{
			return Path.GetFullPath(Path.Combine(config.RootDir, "certificates", archEntryName));
		}

		public override JournalRecord UpdateLocalFile(string localFileName)
		{
			IsDownloaded = true;
			CertificateFiles.Add(new CertificateFile(localFileName));
			return new JournalRecord(this) {
				Name = String.Format("Сертификаты для {0} серия {1}", Product, SerialNumber),
				Filename = localFileName,
			};
		}

		public override IEnumerable<string> GetFiles()
		{
			return CertificateFiles.Select(f => f.LocalFileName);
		}

		public override void Completed()
		{
			base.Completed();
			if (CertificateFiles.Count == 0) {
				IsCertificateNotFound = true;
			}
		}

		private void WaybillChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "VitallyImportant") {
				OnPropertyChanged("IsNdsInvalid");
				OnPropertyChanged("ActualVitallyImportant");
			}
		}

		private void RecalculateMarkups(decimal? rawRetailCost)
		{
			UpdateMarkups(rawRetailCost);
			OnPropertyChanged("RealRetailMarkup");
			OnPropertyChanged("RetailMarkup");
			OnPropertyChanged("IsMarkupInvalid");
			OnPropertyChanged("IsMarkupToBig");
		}

		private void RecalculateFromRetailMarkup()
		{
			decimal? rawCost;
			_retailCost = CalculateRetailCost(RetailMarkup, out rawCost);
			RecalculateMarkups(rawCost);
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
		}

		private void RecalculateFromRealRetailMarkup()
		{
			_retailCost = CalculateFromRealMarkup(RealRetailMarkup);
			RecalculateMarkups(RetailCost);
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
		}

		public virtual void CalculateForMigrated(Settings settings)
		{
			IsMigrate = true;
			if (Edited) {
				if (RetailCost == null) {
					RecalculateFromRetailMarkup();
				}
				else {
					RecalculateMarkups(RetailCost);
				}
			}
			else {
				Calculate(settings);
			}
		}

		public virtual void Calculate(Settings settings)
		{
			if (!IsCalculable()) {
				_retailCost = null;
				_retailMarkup = null;
				_realRetailMarkup = null;
				_maxSupplierMarkup = null;
				MaxRetailMarkup = null;
				OnPropertyChanged("RetailCost");
				OnPropertyChanged("RetailSum");
				OnPropertyChanged("RetailMarkup");
				OnPropertyChanged("RealRetailMarkup");
				OnPropertyChanged("IsMarkupInvalid");
				OnPropertyChanged("IsMarkupToBig");
				OnPropertyChanged("IsSupplierPriceMarkupInvalid");
				return;
			}

			var sourceCost = SupplierCostWithoutNds.GetValueOrDefault();
			if (ActualVitallyImportant) {
				sourceCost = ProducerCost.GetValueOrDefault();
				if (RegistryCost.GetValueOrDefault() > 0 && (sourceCost == 0 || sourceCost > RegistryCost.GetValueOrDefault())) {
					sourceCost = RegistryCost.GetValueOrDefault();
				}
				if (settings.UseSupplierPriceWithNdsForMarkup) {
					sourceCost = sourceCost * (1 + (decimal)Nds.GetValueOrDefault() / 100);
				}
			}

			if (sourceCost == 0)
				return;
			var markupType = ActualVitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			var markup = MarkupConfig.Calculate(settings.Markups, markupType, sourceCost);
			if (markup == null)
				return;

			MaxRetailMarkup = markup.MaxMarkup;
			_maxSupplierMarkup = markup.MaxSupplierMarkup;
			//пересчет производится при каждом входе в накладную что бы отобразить актуальные данные если наценки были изменены
			//для позиций которые редактировал пользователь пересчитывать ничего не нужно иначе данные могут измениться
			//в результате ошибок округления
			if (Edited)
				return;

			_retailMarkup = markup.Markup;
			decimal? rawCost;
			_retailCost = CalculateRetailCost(RetailMarkup, out rawCost);
			//это лишено смысла но тем не менее analitf считает наценку от не округленной цены
			//что бы получить все выглядело идентично делаем тоже самое
			//тк RetailCost может быть округлена до большего то и наценка может увеличиться и превысить значение наценки которое
			//применялась в расчетах
			//наверное правильно всегда округлять до меньшего но analitf делает не так, делаем тоже что analitf
			UpdateMarkups(rawCost);
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
			OnPropertyChanged("RetailMarkup");
			OnPropertyChanged("RealRetailMarkup");
			OnPropertyChanged("IsMarkupInvalid");
			//после пересчета состояние флагов валидации могло измениться
			OnPropertyChanged("IsMarkupToBig");
			OnPropertyChanged("IsSupplierPriceMarkupInvalid");
		}

		private void UpdateMarkups(decimal? cost)
		{
			if (!IsCalculable())
				return;

			var retailCost = cost;
			if (ActualVitallyImportant)
				_retailMarkup = NullableHelper.Round((retailCost - SupplierCost) / (ProducerCost * TaxFactor) * 100, 2);
			else
				_retailMarkup = NullableHelper.Round((retailCost - SupplierCost) / (SupplierCostWithoutNds * TaxFactor) * 100, 2);

			_realRetailMarkup = NullableHelper.Round((retailCost - SupplierCost) / SupplierCost * 100, 2);
		}

		private bool IsCalculable()
		{
			if (SupplierCost.GetValueOrDefault() == 0)
				return false;
			if (ActualVitallyImportant && ProducerCost.GetValueOrDefault() == 0)
				return false;
			return true;
		}

		private decimal? CalculateFromRealMarkup(decimal? realMarkup)
		{
			return RoundCost(realMarkup / 100 * SupplierCost + SupplierCost);
		}

		private decimal? CalculateRetailCost(decimal? markup, out decimal? rawCost)
		{
			var baseCost = ProducerCost;
			if (!ActualVitallyImportant)
				baseCost = SupplierCostWithoutNds;

			var value = SupplierCost + baseCost * markup / 100 * TaxFactor;
			//безумие продолжается если округляем до десятых то тогда считаем от округленного значения
			//это миграция с analitf?
			if (IsMigrate)
				rawCost = value;
			else
				rawCost = Round(value);
			return RoundCost(value);
		}

		private decimal? RoundCost(decimal? value) => Round(NullableHelper.Round(value, 2));

		private decimal? Round(decimal? value)
		{
			if (Waybill.Rounding != Rounding.None) {
				var @base = 10;
				var factor = 1;
				if (Waybill.Rounding == Rounding.To1_00) {
					@base = 1;
				}
				else if (Waybill.Rounding == Rounding.To0_50) {
					@factor = 5;
				}
				var normalized = ((int?)(value * @base));
				return (normalized - normalized % factor) / (decimal)@base;
			}
			return value;
		}

		public virtual void BeginEdit()
		{
		}

		public virtual void EndEdit()
		{
			if (Waybill != null && Waybill.IsCreatedByUser) {
				Amount = Quantity * SupplierCost;
				NdsAmount = NullableHelper.Round(Nds / 100m * Amount, 2);
				Calculate(Waybill.Settings);
				foreach (var property in typeof(WaybillLine).GetProperties()) {
					OnPropertyChanged(property.Name);
				}
				Waybill.Calculate(Waybill.Settings);
			}
		}

		public virtual void CancelEdit()
		{
		}
	}
}