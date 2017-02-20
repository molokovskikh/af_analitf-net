using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnalitF.Net.Client.Config.NHibernate;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Helpers
{
	public class BaseNotify : INotifyPropertyChanged
	{
		public BaseNotify()
		{
			IsNotifying = true;
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		[Ignore, JsonIgnore]
		public virtual bool IsNotifying { get; set; }

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			if (!IsNotifying)
				return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}