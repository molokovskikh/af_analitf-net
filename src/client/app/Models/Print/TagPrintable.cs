using System;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Models.Print
{
	public class TagPrintable : BaseNotify
	{
		private bool _selected;

		public string Product { get; set; }
		public decimal? RetailCost { get; set; }
		public string SupplierName { get; set; }
		public string ClientName { get; set; }
		public string Producer { get; set; }
		public string ProviderDocumentId { get; set; }
		public DateTime DocumentDate { get; set; }
		public string Barcode { get; set; }
		public string AltBarcode { get; set; }
		public string SerialNumber { get; set; }
		public decimal Quantity { get; set; }
		public string Period { get; set; }
		public string Country { get; set; }
		public string Certificates { get; set; }
		public decimal? SupplierPriceMarkup { get; set; }
		public decimal? RetailMarkup { get; set; }
		public decimal? SupplierCost { get; set; }
		public double Nds { get; set; }
		public double Np { get; set; }
		public uint? WaybillId { get; set; }
		public int CopyCount { get; set; }

		public bool Selected
		{
			get { return _selected; }
			set
			{
				if (Selected != value)
				{
					_selected = value;
					OnPropertyChanged();
				}
			}
		}
	}
}
