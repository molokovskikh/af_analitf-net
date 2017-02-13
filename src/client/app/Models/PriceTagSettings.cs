using System.ComponentModel;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace AnalitF.Net.Client.Models
{
		public enum PriceTagType
	{
		[Description("Стандартный размер")] Normal,
		[Description("Малый размер")] Small,
		[Description("Малый размер с большой ценой")] BigCost,
		[Description("Малый размер с большой ценой №2")] BigCost2,
		[Description("Конструктор")] Custom,
	}

	public class PriceTagSettings : BaseNotify
	{
		private PriceTagType _type;
		private bool _printFullName;
		private bool _printProduct;
		private bool _printCountry;
		private bool _printProducer;
		private bool _printPeriod;
		private bool _printProviderDocumentId;
		private bool _printSupplier;
		private bool _printSerialNumber;
		private bool _printDocumentDate;

		public PriceTagSettings()
		{
			PrintProduct = true;
			PrintCountry = true;
			PrintProducer = true;
			PrintPeriod = true;
			PrintProviderDocumentId = true;
			PrintSupplier = true;
			PrintSerialNumber = true;
			PrintDocumentDate = true;
			PrintFullName = true;
		}

		public PriceTagSettings(Address address) : this()
		{
			Address = address;
		}

		public PriceTagSettings(PriceTagSettings source, Address address) : this(address)
		{
			HideNotPrinted = source.HideNotPrinted;
			PrintCountry = source.PrintCountry;
			PrintDocumentDate = source.PrintDocumentDate;
			PrintEmpty = source.PrintEmpty;
			PrintFullName = source.PrintFullName;
			PrintPeriod = source.PrintPeriod;
			PrintProducer = source.PrintProducer;
			PrintProduct = source.PrintProduct;
			PrintProviderDocumentId = source.PrintProviderDocumentId;
			PrintSerialNumber = source.PrintSerialNumber;
			PrintSupplier = source.PrintSupplier;
			Settings = source.Settings;
			Type = source.Type;
		}

		public virtual uint Id { get; set; }
		public virtual Settings Settings { get; set; }
		public virtual Address Address { get; set; }

		public virtual PriceTagType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				typeof(PriceTagSettings).GetProperties().Each(p => OnPropertyChanged(p.Name));
			}
		}

		public virtual bool PrintEmpty { get; set; }
		public virtual bool HideNotPrinted { get; set; }

		public virtual bool PrintFullName
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printFullName;
				else if (_type == PriceTagType.Small)
					return false;
				else
					return true;
			}
			set
			{
				_printFullName = value;
			}
		}

		public virtual bool PrintProduct
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printProduct;
				else
					return true;
			}
			set
			{
				_printProduct = value;
			}
		}

		public virtual bool PrintCountry
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printCountry;
				else if (_type == PriceTagType.Small)
					return true;
				else
					return false;
			}
			set
			{
				_printCountry = value;
			}
		}

		public virtual bool PrintProducer
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printProducer;
				else
					return true;
			}
			set
			{
				_printProducer = value;
			}
		}

		public virtual bool PrintPeriod
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printPeriod;
				else
					return true;
			}
			set
			{
				_printPeriod = value;
			}
		}

		public virtual bool PrintProviderDocumentId
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printProviderDocumentId;
				else
					return false;
			}
			set
			{
				_printProviderDocumentId = value;
			}
		}

		public virtual bool PrintSupplier
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printSupplier;
				else if (_type == PriceTagType.BigCost2)
					return true;
				else
					return false;

			}
			set
			{
				_printSupplier = value;
			}
		}

		public virtual bool PrintSerialNumber
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printSerialNumber;
				else if (_type == PriceTagType.BigCost2)
					return true;
				else
					return false;

			}
			set
			{
				_printSerialNumber = value;
			}
		}

		public virtual bool PrintDocumentDate
		{
			get
			{
				if (_type == PriceTagType.Normal)
					return _printDocumentDate;
				else if (_type == PriceTagType.Small)
					return true;
				else
					return false;
			}
			set
			{
				_printDocumentDate = value;
			}

		}

		public static IEnumerable<PriceTagSettings> Defaults(Address address)
		{
			return new[]
			{
				new PriceTagSettings(address)
			};
		}

		[IgnoreDataMember]
		public virtual bool IsConfigurable
		{
			get { return Type == PriceTagType.Normal; }
		}

		[IgnoreDataMember]
		public virtual bool IsConstructor
		{
			get { return Type == PriceTagType.Custom; }
		}
	}
}