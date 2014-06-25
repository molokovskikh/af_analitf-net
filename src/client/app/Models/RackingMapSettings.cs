using System.ComponentModel;
using AnalitF.Net.Client.Helpers;
using Common.Tools;

namespace AnalitF.Net.Client.Models
{
	public enum RackingMapSize
	{
		[Description("Стандартный размер")] Normal,
		[Description("Большой размер")] Big,
		[Description("Стеллажная карта №2")] Normal2
	}

	public class RackingMapSettings : BaseNotify
	{
		private RackingMapSize _size;
		private bool _printProduct;
		private bool _printProducer;
		private bool _printSerialNumber;
		private bool _printPeriod;
		private bool _printQuantity;
		private bool _printSupplier;
		private bool _printCertificates;
		private bool _printDocumentDate;
		private bool _printRetailCost;

		public RackingMapSettings()
		{
			PrintProduct = true;
			PrintProducer = true;
			PrintSerialNumber = true;
			PrintPeriod = true;
			PrintQuantity = true;
			PrintSupplier = true;
			PrintCertificates = true;
			PrintDocumentDate = true;
			PrintRetailCost = true;
		}

		public virtual RackingMapSize Size
		{
			get { return _size; }
			set
			{
				_size = value;
				typeof(RackingMapSettings).GetProperties().Each(p => OnPropertyChanged(p.Name));
			}
		}
		public virtual bool HideNotPrinted { get; set; }

		public virtual bool PrintProduct
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printProduct;
			}
			set
			{
				_printProduct = value;
			}
		}

		public virtual bool PrintProducer
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printProducer;
			}
			set
			{
				_printProducer = value;
			}
		}

		public virtual bool PrintSerialNumber
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printSerialNumber;
			}
			set
			{
				_printSerialNumber = value;
			}
		}

		public virtual bool PrintPeriod
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printPeriod;
			}
			set
			{
				_printPeriod = value;
			}
		}

		public virtual bool PrintQuantity
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printQuantity;
			}
			set
			{
				_printQuantity = value;
			}
		}

		public virtual bool PrintSupplier
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printSupplier;
			}
			set
			{
				_printSupplier = value;
			}
		}

		public virtual bool PrintCertificates
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return false;
				return _printCertificates;
			}
			set
			{
				_printCertificates = value;
			}
		}

		public virtual bool PrintDocumentDate
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printDocumentDate;
			}
			set
			{
				_printDocumentDate = value;
			}
		}

		public virtual bool PrintRetailCost
		{
			get
			{
				if (_size == RackingMapSize.Normal2)
					return true;
				return _printRetailCost;
			}
			set
			{
				_printRetailCost = value;
			}
		}

		public bool IsConfigurable
		{
			get { return Size != RackingMapSize.Normal2; }
		}
	}
}