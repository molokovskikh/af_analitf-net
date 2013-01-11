using Caliburn.Micro;

namespace AnalitF.Net.Client.Binders
{
	public interface IActivateEx : IActivate
	{
		bool IsSuccessfulActivated { get; }
	}

	public class BaseConductor : Conductor<IScreen>
	{
		protected override void ChangeActiveItem(IScreen newItem, bool closePrevious)
		{
			base.ChangeActiveItem(newItem, closePrevious);

			var activateEx = newItem as IActivateEx;
			if (activateEx != null && !activateEx.IsSuccessfulActivated) {
				this.CloseItem(ActiveItem);
			}
		}
	}
}