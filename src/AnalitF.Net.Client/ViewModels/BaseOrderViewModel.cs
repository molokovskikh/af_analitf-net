using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public abstract class BaseOrderViewModel : BaseScreen
	{
		private DateTime begin;
		private DateTime end;

		private bool isSentSelected;
		private bool isCurrentSelected;
		private bool isSendInit;

		public BaseOrderViewModel()
		{
			IsCurrentSelected = true;
			this.ObservableForProperty(m => m.Begin)
				.Merge(this.ObservableForProperty(m => m.End))
				.Subscribe(n => Update());

			this.ObservableForProperty(m => m.IsSentSelected)
				.Merge(this.ObservableForProperty(m => m.IsCurrentSelected))
				.Skip(1)
				.Take(1)
				.Repeat()
				.Subscribe(n => {
					if (IsSentSelected && !isSendInit) {
						isSendInit = true;
						Update();
					}

					NotifyOfPropertyChange("BeginEnabled");
					NotifyOfPropertyChange("EndEnabled");
				});
		}

		public abstract void Update();

		public bool IsCurrentSelected
		{
			get { return isCurrentSelected; }
			set
			{
				isCurrentSelected = value;
				NotifyOfPropertyChange("IsCurrentSelected");
			}
		}

		public bool IsSentSelected
		{
			get { return isSentSelected; }
			set
			{
				isSentSelected = value;
				NotifyOfPropertyChange("IsSentSelected");
			}
		}

		public DateTime Begin
		{
			get { return begin; }
			set
			{
				begin = value;
				NotifyOfPropertyChange("Begin");
			}
		}

		public bool BeginEnabled
		{
			get { return IsSentSelected; }
		}

		public DateTime End
		{
			get { return end; }
			set
			{
				end = value;
				NotifyOfPropertyChange("End");
			}
		}

		public bool EndEnabled
		{
			get { return IsSentSelected; }
		}

		protected override void OnDeactivate(bool close)
		{
			Session.Flush();
			base.OnDeactivate(close);
		}
	}
}