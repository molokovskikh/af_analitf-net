using System.ComponentModel;

namespace AnalitF.Net.Client.Helpers
{
	public class BaseNotify : INotifyPropertyChanged
	{
		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}