using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using AnalitF.Net.Client.Binders;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Microsoft.Win32;
using DialogResult = System.Windows.Forms.DialogResult;

namespace AnalitF.Net.Client.Extentions
{
	public class WindowManager : Caliburn.Micro.WindowManager
	{
#if DEBUG
		public bool UnitTesting;
		public bool SkipApp;
		public MessageBoxResult DefaultQuestsionResult = MessageBoxResult.Yes;
		public MessageBoxResult DefaultResult = MessageBoxResult.OK;
		public Action<object> ContinueViewDialog = d => {  };

		public Subject<object> DialogOpened = new Subject<object>();
		public Subject<Window> WindowOpened = new Subject<Window>();
		public Subject<FileDialog> FileDialog = new Subject<FileDialog>();
		public Subject<string> MessageOpened = new Subject<string>();

		public List<Window> Dialogs = new List<Window>();
		public List<string> MessageBoxes = new List<string>();
#endif

		public override void ShowWindow(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
#if DEBUG
			if (UnitTesting)
				return;
#endif
			base.ShowWindow(rootModel, context, settings);
		}

		public override bool? ShowDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			if (Stub(rootModel))
				return true;

			var window = CreateWindow(rootModel, true, context, settings);
			if (window.Owner != null) {
				window.SizeToContent = SizeToContent.Manual;
				window.Height = window.Owner.Height * 2 / 3;
				window.Width = window.Owner.Width * 2 / 3;
				window.ShowInTaskbar = false;
			}

			return ShowDialog(window);
		}

		public bool? ShowDialog(FileDialog dialog)
		{
#if DEBUG
			if (Stub(dialog))
				return true;
			if (SkipApp) {
				FileDialog.OnNext(dialog);
				return true;
			}
#endif
			return dialog.ShowDialog();
		}

		public bool? ShowFixedDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			if (Stub(rootModel))
				return true;

			var window = CreateWindow(rootModel, true, context, settings);
			window.ResizeMode = ResizeMode.NoResize;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			window.ShowInTaskbar = false;

			return ShowDialog(window);
		}

		private bool Stub(object rootModel)
		{
#if DEBUG
			if (UnitTesting) {
				if (rootModel is FileDialog) {
					FileDialog.OnNext((FileDialog)rootModel);
				}
				else {
					IoC.BuildUp(rootModel);
					ScreenExtensions.TryActivate(rootModel);
					DialogOpened.OnNext(rootModel);
					ContinueViewDialog(rootModel);
				}
				return true;
			}
#endif
			return false;
		}

		//скорбная песнь
		//для того что значения форматировались в соответствии с правилами русского языка
		//wpf нужно сказать что мы русские люди, сам он понимать отказывается и по умолчанию
		//использует локаль en-us
		//для этого есть много способов:
		//перегрузка значения для FramewrokElement.Language, работает не полностью тк есть еще FramewrokContentElement.Language
		//я его перегрузить нельзя тк на самом деле это свойство и само является переопределением FramewrokElement.Language
		//а переопределить свойство дважды в wpf нельзя
		//
		//xml:lang - нужно применять для всех форм которые ты создаешь, что геморрой и будет просрано
		//
		//остается надеяться что ни какой придурок не будет создавать окна руками
		//по этому для каждого вновь создаваемого окна принудительно указываем Language
		protected override Window CreateWindow(object rootModel, bool isDialog, object context, IDictionary<string, object> settings)
		{
			IoC.BuildUp(rootModel);
			var screen = rootModel as Screen;
			if (screen != null && string.IsNullOrEmpty(screen.DisplayName))
				screen.DisplayName = "АналитФАРМАЦИЯ";

			var window = base.CreateWindow(rootModel, isDialog, context, settings);
			window.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
#if DEBUG
			if (SkipApp)
				WindowOpened.OnNext(window);
#endif
			return window;
		}

		protected override Window InferOwnerOf(Window window)
		{
#if DEBUG
			if (UnitTesting || SkipApp)
				return null;
#endif
			return base.InferOwnerOf(window);
		}

		private bool? ShowDialog(Window window)
		{
#if DEBUG
			if (UnitTesting) {
				window.Closed += (sender, args) => Dialogs.Remove(window);
				Dialogs.Add(window);
				return true;
			}
#endif
			window.InputBindings.Add(new KeyBinding(Commands.InvokeViewModel, new KeyGesture(Key.Escape)) {
				CommandParameter = "TryClose"
			});
			return window.ShowDialog();
		}

		public MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
		{
#if DEBUG
			if (UnitTesting) {
				MessageOpened.OnNext(text);
				MessageBoxes.Add(text);
				return icon == MessageBoxImage.Warning ? DefaultQuestsionResult : DefaultResult;
			}
			if (SkipApp)
				MessageOpened.OnNext(text);
#endif

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
