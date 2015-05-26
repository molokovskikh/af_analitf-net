using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Controls.Behaviors
{
	public class Watermark : Behavior<TextBox>
	{
		public static DependencyProperty WatermarkTextProperty = DependencyProperty.Register("WatermarkText", typeof(string), typeof(Watermark));
		private string _text;

		protected override void OnAttached()
		{
			AssociatedObject.SetValue(WatermarkTextProperty, _text);
			AssociatedObject.CommandBindings.Add(new CommandBinding(Commands.CleanText, (sender, args) => {
				var box = ((DependencyObject)args.OriginalSource).VisualParents<TextBox>().FirstOrDefault();
				if (box != null) {
					box.Text = "";
				}
			}));
		}

		public string Text
		{
			get
			{
				if (AssociatedObject == null)
					return _text;
				return (string)AssociatedObject.GetValue(WatermarkTextProperty);
			}
			set
			{
				_text = value;
				if (AssociatedObject == null)
					return;
				AssociatedObject.SetValue(WatermarkTextProperty, value);
			}
		}
	}
}