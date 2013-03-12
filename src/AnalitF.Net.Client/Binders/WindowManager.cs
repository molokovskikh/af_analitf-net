using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace AnalitF.Net.Client.Extentions
{
	public class WindowManager : Caliburn.Micro.WindowManager
	{
		public bool UnderTest;
		public MessageBoxResult DefaultResult = MessageBoxResult.OK;

		public List<Window> Dialogs = new List<Window>();
		public List<string> MessageBoxes = new List<string>();

		public override void ShowWindow(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			if (UnderTest)
				return;

			base.ShowWindow(rootModel, context, settings);
		}

		public override bool? ShowDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			var window = CreateWindow(rootModel, true, context, settings);
			if (window.Owner != null) {
				window.SizeToContent = SizeToContent.Manual;
				window.Height = window.Owner.Height * 2 / 3;
				window.Width = window.Owner.Width * 2 / 3;
				window.ShowInTaskbar = false;
			}

			return ShowDialog(window);
		}

		public bool? ShowFixedDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			var window = CreateWindow(rootModel, true, context, settings);
			window.ResizeMode = ResizeMode.NoResize;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			window.ShowInTaskbar = false;

			return ShowDialog(window);
		}

		//скорбная песнь
		//для того что значения форматировались в соответсвии с правилами русского языка
		//wpf нужно сказать что мы русские люди, сам он понимать отказывается и по умолчанию
		//использует локаль en-us
		//для этого есть много способов:
		//перегрузка значения для FramewrokElement.Language, работает не полностью тк есть еще FramewrokContentElement.Language
		//я его перегрузить нельзя тк на самом деле это свойство и само является переопределением FramewrokElement.Language
		//а переопределить свойство дважды в wpf нельзя
		//
		//xml:lang - нужно применять для всех форм которые ты создаешь, что геморой и будет просрано
		//
		//остается надеяться что ни какой придурок не будет создавать окна руками
		//по этому для каждого вновь создаваемого окна принудительно указываем Language
		protected override Window CreateWindow(object rootModel, bool isDialog, object context, IDictionary<string, object> settings)
		{
			var window = base.CreateWindow(rootModel, isDialog, context, settings);
			window.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
			return window;
		}

		private bool? ShowDialog(Window window)
		{
			if (UnderTest) {
				window.Closed += (sender, args) => Dialogs.Remove(window);
				Dialogs.Add(window);
				return true;
			}
			return window.ShowDialog();
		}

		public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
		{
			if (UnderTest) {
				MessageBoxes.Add(text);
				return DefaultResult;
			}

			var window = InferOwnerOf(null);
			if (window == null)
				return MessageBox.Show(text, caption, buttons, icon);
			else
				return MessageBox.Show(window, text, caption, buttons, icon);
		}

		public void Warning(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Внимание",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
		}

		public void Notify(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Информация",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		public void Error(string text)
		{
			ShowMessageBox(text, "АналитФАРМАЦИЯ: Ошибка",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}

		public MessageBoxResult Question(string text)
		{
			return ShowMessageBox(text, "АналитФАРМАЦИЯ: Внимание",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);
		}
	}
}