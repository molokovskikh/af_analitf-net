using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Config.Initializers;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public class WaybillLine : BaseNotify, IEditableObject
	{
		private decimal? _retailCost;
		private decimal? _realRetailMarkup;
		private decimal? _retailMarkup;
		private decimal? _maxRetailMarkup;
		private decimal? _maxSupplierMarkup;
		private bool _print;
		private Waybill _waybill;

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
		public virtual string SerialNumber { get; set; }
		public virtual string Certificates { get; set; }
		public virtual bool LoadCertificate { get; set; }

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

		public virtual bool Edited { get; set; }

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
				RecalculateMarkups();
				Waybill.CalculateRetailSum();
				OnPropertyChanged("RetailSum");
				OnPropertyChanged();
			}
		}

		public virtual uint? RejectId { get; set; }

		[Style(Description = "Новая разбракованя позиция")]
		public virtual bool IsRejectCanceled { get; set; }

		[Style(Description = "Новая забракованая позиция")]
		public virtual bool IsRejectNew { get; set; }

		[Style(Description = "Забракованая позиция")]
		public virtual bool IsReject
		{
			get { return !IsRejectNew && RejectId != null; }
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

		[Style("RetailMarkup", "RetailCost", "RetailSum", "RealRetailMarkup",
			Description = "Розничная цена: не рассчитана")]
		public virtual bool IsMarkupInvalid
		{
			get { return RetailMarkup == null; }
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

		[Style(Name = "VitallyImportant")]
		public virtual bool ActualVitallyImportant
		{
			get { return Waybill != null && Waybill.VitallyImportant || VitallyImportant.GetValueOrDefault(); }
		}

		public virtual decimal? ProducerCostWithTax
		{
			get { return ProducerCost * (1 + (decimal?) Nds / 100); }
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

		public virtual decimal? TaxPerUnit
		{
			get { return SupplierCost - SupplierCostWithoutNds; }
		}

		private void WaybillChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "VitallyImportant") {
				OnPropertyChanged("IsNdsInvalid");
				OnPropertyChanged("ActualVitallyImportant");
			}
		}

		private void RecalculateMarkups()
		{
			UpdateMarkups(RetailCost);
			OnPropertyChanged("RealRetailMarkup");
			OnPropertyChanged("RetailMarkup");
			OnPropertyChanged("IsMarkupInvalid");
			OnPropertyChanged("IsMarkupToBig");
		}

		private void RecalculateFromRetailMarkup()
		{
			decimal? stub;
			_retailCost = CalculateRetailCost(RetailMarkup, out stub);
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

			decimal? rawCost;
			_retailCost = CalculateRetailCost(RetailMarkup, out rawCost);
			//это лишено смысла но тем не менее analitf считает наценку от не округленной цены
			//что бы получить все выглядело идентично делаем тоже самое
			//тк RetailCost может быть округлена до большего то и наценка может вырости и привысить значение наценки которое
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
			rawCost = value;
			//безумее продолжается если округляем до десятых то тогда считаем от округленного значения
			if (Waybill.RoundTo1) {
				rawCost = ((int?)(value * 10)) / 10m;
			}
			return RoundCost(value);
		}

		private decimal? RoundCost(decimal? value)
		{
			value = NullableHelper.Round(value, 2);
			if (Waybill.RoundTo1)
				return ((int?)(value * 10)) / 10m;
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
				Calculate(Waybill.Settings, Waybill.Settings.Markups);
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