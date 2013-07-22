using System.Collections.Generic;
using System.Diagnostics;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public interface IActivateEx : IActivate
	{
		bool IsSuccessfulActivated { get; }
	}

	public class BaseConductor : Conductor<IScreen>
	{
		public bool UnitTesting;
		public List<string> StartedProcess = new List<string>();

		protected override void ChangeActiveItem(IScreen newItem, bool closePrevious)
		{
			base.ChangeActiveItem(newItem, closePrevious);

			var activateEx = newItem as IActivateEx;
			if (activateEx != null && !activateEx.IsSuccessfulActivated) {
				this.CloseItem(ActiveItem);
			}
		}

		protected void StartProcess(string exe, string args = "")
		{
			if (UnitTesting) {
				StartedProcess.Add(exe + " " + args);
				return;
			}

			Process.Start(exe, args);
		}

		public override void TryClose(bool? dialogResult)
		{
			UnitTestClose();
			base.TryClose(dialogResult);
		}

		public override void TryClose()
		{
			UnitTestClose();
			base.TryClose();
		}

		private void UnitTestClose()
		{
			if (UnitTesting) {
				if (Parent == null && Views.Count == 0)
					CanClose(r => {
						if (r)
							ScreenExtensions.TryDeactivate(this, true);
					});
			}
		}
	}
}