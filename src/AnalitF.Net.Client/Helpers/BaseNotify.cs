using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnalitF.Net.Client.Helpers
{
	public class BaseNotify : INotifyPropertyChanged
	{
		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}