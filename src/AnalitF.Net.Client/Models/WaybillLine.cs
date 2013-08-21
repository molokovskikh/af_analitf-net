using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class StyleAttribute : Attribute
	{
		public string[] Columns;
		public string Description;

		public StyleAttribute(params string[] columns)
		{
			Columns = columns;
		}
	}

	public class WaybillLine : BaseNotify
	{
		private decimal? _retailCost;
		private decimal? _realRetailMarkup;
		private decimal? _retailMarkup;
		private decimal? _maxRetailMarkup;
		private decimal? _maxSupplierMarkup;

		public WaybillLine()
		{
			Print = true;
		}

		public WaybillLine(Waybill waybill)
			: this()
		{
			Waybill = waybill;
		}

		public virtual uint Id { get; set; }
		public virtual Waybill Waybill { get; set; }
		public virtual string Product { get; set; }
		public virtual string Producer { get; set; }
		public virtual string Country { get; set; }
		public virtual bool Print { get; set; }
		public virtual string Period { get; set; }
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }
		public virtual bool LoadCertificate { get; set; }

		public virtual string Unit { get; set; }
		public virtual decimal? ExciseTax { get; set; }
		public virtual string BillOfEntryNumber { get; set; }

		[Style]
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

		public virtual bool Edited { get; set; }

		public virtual decimal? MaxRetailMarkup
		{
			get { return _maxRetailMarkup; }
			set
			{
				_maxRetailMarkup = value;
				OnPropertyChanged("MaxRetailMarkup");
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
				RecalculateMarkups();
				Waybill.CalculateRetailSum();
				OnPropertyChanged("RetailSum");
				OnPropertyChanged("RetailCost");
			}
		}

		[Style("Nds", Description = "НДС: не установлен для ЖНВЛС")]
		public virtual bool IsNdsInvalid
		{
			get { return ActualVitallyImportant && Nds.GetValueOrDefault(10) != 10;}
		}

		[Style("RetailMarkup", "MaxRetailMarkup",
			Description = "Розничная наценка: превышение максимальной розничной наценки")]
		public virtual bool IsMarkupToBig
		{
			get { return RetailMarkup > MaxRetailMarkup; }
		}

		//todo: если null?
		[Style("RetailMarkup", "RetailCost", "RetailSum", "RealRetailMarkup",
			Description = "Розничная цена: не рассчитана")]
		public virtual bool IsMarkupInvalid
		{
			get { return RetailMarkup  == null; }
		}

		[Style("SupplierPriceMarkup", Description = "Торговая наценка оптовика: превышение наценки оптовика")]
		public virtual bool IsSupplierPriceMarkupInvalid
		{
			get { return SupplierPriceMarkup > _maxSupplierMarkup; }
		}

		public virtual decimal? RetailSum
		{
			get { return Quantity * RetailCost; }
		}

		public virtual decimal? AmountExcludeTax
		{
			get { return Amount - NdsAmount; }
		}

		private bool ActualVitallyImportant
		{
			get { return Waybill != null && Waybill.VitallyImportant || VitallyImportant.GetValueOrDefault(); }
		}

		public virtual decimal? ProducerCostWithTax
		{
			get { return ProducerCost * (1 + (decimal?) Nds / 100); }
		}

		private void RecalculateMarkups()
		{
			UpdateMarkups();
			OnPropertyChanged("RealRetailMarkup");
			OnPropertyChanged("RetailMarkup");
			OnPropertyChanged("IsMarkupInvalid");
			OnPropertyChanged("IsMarkupToBig");
		}

		private void RecalculateFromRetailMarkup()
		{
			_retailCost = CalculateRetailCost(RetailMarkup);
			RecalculateMarkups();
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
		}

		private void RecalculateFromRealRetailMarkup()
		{
			_retailCost = CalculateFromRealMarkup(RealRetailMarkup);
			RecalculateMarkups();
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
		}

		public virtual void Calculate(Settings settings, IEnumerable<MarkupConfig> markups)
		{
			if (!IsCalculable())
				return;

			var vitallyImportant = ActualVitallyImportant;
			var lookByProducerCost = vitallyImportant && settings.LookupMarkByProducerCost;
			var sourceCost = (lookByProducerCost ? ProducerCost : SupplierCostWithoutNds).GetValueOrDefault();
			if (sourceCost == 0)
				return;
			var markupType = vitallyImportant ? MarkupType.VitallyImportant : MarkupType.Over;
			var markup = MarkupConfig.Calculate(markups, markupType, sourceCost);
			if (markup == null)
				return;

			MaxRetailMarkup = markup.MaxMarkup;
			_maxSupplierMarkup = markup.MaxSupplierMarkup;
			if (!Edited) {
				_retailMarkup = markup.Markup;
			}

			_retailCost = CalculateRetailCost(RetailMarkup);
			UpdateMarkups();
			OnPropertyChanged("RetailCost");
			OnPropertyChanged("RetailSum");
			OnPropertyChanged("RetailMarkup");
			OnPropertyChanged("RealRetailMarkup");
			OnPropertyChanged("IsMarkupInvalid");
			//после пересчета состояние флагов валидации могло измениться
			OnPropertyChanged("IsMarkupToBig");
			OnPropertyChanged("IsSupplierPriceMarkupInvalid");
		}

		private void UpdateMarkups()
		{
			if (!IsCalculable())
				return;

			if (ActualVitallyImportant)
				_retailMarkup = NullableHelper.Round((RetailCost - SupplierCost) / (ProducerCost * TaxFactor) * 100, 2);
			else
				_retailMarkup = NullableHelper.Round((RetailCost - SupplierCost) / SupplierCost * 100, 2);

			_realRetailMarkup = NullableHelper.Round((RetailCost - SupplierCost) / SupplierCost * 100, 2);
		}

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

		private decimal? CalculateRetailCost(decimal? markup)
		{
			var baseCost = ProducerCost;
			if (!ActualVitallyImportant)
				baseCost = SupplierCostWithoutNds;

			var value = SupplierCost + baseCost * markup / 100 * TaxFactor;
			return RoundCost(value);
		}

		private decimal? RoundCost(decimal? value)
		{
			value = NullableHelper.Round(value, 2);
			if (Waybill.RoundTo1)
				return ((int?)(value * 10)) / 10m;
			return value;
		}
	}
}