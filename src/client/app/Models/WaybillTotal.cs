using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnalitF.Net.Client.Models
{
    public class WaybillTotal : INotifyPropertyChanged
    {
        private decimal _totalSum;
        private decimal _totalRetailSum;

        public decimal TotalSum
        {
            get { return _totalSum; }
            set
            {
                _totalSum = value;
                OnPropertyChanged("TotalSum");
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
