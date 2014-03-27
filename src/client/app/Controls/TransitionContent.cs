using System.Windows;
using System.Windows.Controls;

namespace AnalitF.Net.Client.Controls
{
	public class TransitionContent : ContentControl
	{
		private ContentPresenter oldContentHost;
		private ContentPresenter newContentHost;

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			oldContentHost = (ContentPresenter)GetTemplateChild("PreviousContentPresentationSite");
			newContentHost = (ContentPresenter)GetTemplateChild("CurrentContentPresentationSite");
			newContentHost.Content = Content;
			VisualStateManager.GoToState(this, "Normal", true);
		}

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);

			if (newContentHost != null && oldContentHost != null) {
				newContentHost.Content = newContent;
				oldContentHost.Content = oldContent;
				VisualStateManager.GoToState(this, "Normal", true);
				VisualStateManager.GoToState(this, "Down", true);
			}
		}
	}
}