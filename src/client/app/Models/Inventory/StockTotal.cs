using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public string Total
		{
			get { return _total; }
			set
			{
				_total = value;
				OnPropertyChanged("Total");
			}
		}

		public decimal TotalCount
		{
			get { return _totalCount; }
			set
			{
				_totalCount = value;
				OnPropertyChanged("TotalCount");
			}
		}

		public decimal TotalSum
		{
			get { return _totalSum; }
			set
			{
				_totalSum = value;
				OnPropertyChanged("TotalSum");
			}
		}

		public decimal TotalSumWithNds
		{
			get { return _totalSumWithNds; }
			set
			{
				_totalSumWithNds = value;
				OnPropertyChanged("TotalSumWithNds");
			}
		}

		public decimal TotalRetailSum
		{
			get { return _totalRetailSum; }
			set
			{
				_totalRetailSum = value;
				OnPropertyChanged("TotalRetailSum");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName]string prop = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}
	}
}
