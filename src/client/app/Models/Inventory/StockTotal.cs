using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnalitF.Net.Client.Models.Inventory
{
	public class StockTotal : INotifyPropertyChanged
	{
		private string _total;
		private decimal _totalCount;
		private decimal _totalSum;
		private decimal _totalSumWithNds;
		private decimal _totalRetailSum;
		private decimal _reservedQuantity;

		public string Total
		{
			get { return _total; }
			set
			{
				_total = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalCount
		{
			get { return _totalCount; }
			set
			{
				_totalCount = value;
				OnPropertyChanged();
			}
		}

		public decimal ReservedQuantity
		{
			get { return _reservedQuantity; }
			set
			{
				_reservedQuantity = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalSum
		{
			get { return _totalSum; }
			set
			{
				_totalSum = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalSumWithNds
		{
			get { return _totalSumWithNds; }
			set
			{
				_totalSumWithNds = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalRetailSum
		{
			get { return _totalRetailSum; }
			set
			{
				_totalRetailSum = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}
