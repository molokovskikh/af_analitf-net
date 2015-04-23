using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using Caliburn.Micro;
using DialogResult = System.Windows.Forms.DialogResult;

namespace AnalitF.Net.Client.Config.Caliburn
{
	public class Win32Stub : System.Windows.Forms.IWin32Window
	{
		public Win32Stub(IntPtr handle)
		{
			Handle = handle;
		}

		public IntPtr Handle { get; private set; }
	}

	public class WindowManager : global::Caliburn.Micro.WindowManager
	{
#if DEBUG
		public bool UnitTesting;
		public bool SkipApp;
		public MessageBoxResult DefaultQuestsionResult = MessageBoxResult.Yes;
		public MessageBoxResult DefaultResult = MessageBoxResult.OK;

		public Subject<object> DialogOpened = new Subject<object>();
		public Subject<Window> WindowOpened = new Subject<Window>();
		public Subject<object> OsDialog = new Subject<object>();
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
#if DEBUG
			bool? dialogResult;
			if (Stub(rootModel, out dialogResult))
				return null;
#endif

			var window = CreateWindow(rootModel, true, context, settings);
			if (window.Owner != null) {
				window.SizeToContent = SizeToContent.Manual;
				window.Height = window.Owner.Height * 2 / 3;
				window.Width = window.Owner.Width * 2 / 3;
				window.ShowInTaskbar = false;
			}

			ShowDialog(window);
			return null;
		}

		public DialogResult ShowDialog(System.Windows.Forms.CommonDialog dialog)
		{
#if DEBUG
			bool? dialogResult;
			if (Stub(dialog, out dialogResult))
				return DialogResult.OK;
#endif

			var window = InferOwnerOf(null);
			if (window != null)
				return dialog.ShowDialog(new Win32Stub(new WindowInteropHelper(window).Handle));
			else
				return dialog.ShowDialog();
		}

		public bool? ShowDialog(Microsoft.Win32.CommonDialog dialog)
		{
#if DEBUG
			bool? dialogResult;
			if (Stub(dialog, out dialogResult))
				return dialogResult;
#endif
			return dialog.ShowDialog(InferOwnerOf(null));
		}

		public void ShowFixedDialog(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
#if DEBUG
			bool? dialogResult;
			if (Stub(rootModel, out dialogResult)) {
				return;
			}
#endif

			var window = CreateWindow(rootModel, true, context, settings);
			window.ResizeMode = ResizeMode.NoResize;
			window.SizeToContent = SizeToContent.WidthAndHeight;
			window.ShowInTaskbar = false;

			ShowDialog(window);
		}

#if DEBUG
		private bool Stub(object rootModel, out bool? dialogResult)
		{
			dialogResult = false;
			if (UnitTesting || SkipApp) {
				if (rootModel is Microsoft.Win32.CommonDialog || rootModel is System.Windows.Forms.CommonDialog) {
					//по умолчанию для системных диалогов мы должны возвращать true для всех остальных false
					dialogResult = true;
					OsDialog.OnNext(rootModel);
					return true;
				}
			}
			if (UnitTesting) {
				IoC.BuildUp(rootModel);
				ScreenExtensions.TryActivate(rootModel);
				DialogOpened.OnNext(rootModel);
				return true;
			}
			return false;
		}
#endif

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

		private void ShowDialog(Window window)
		{
#if DEBUG
			if (UnitTesting) {
				window.Closed += (sender, args) => Dialogs.Remove(window);
				Dialogs.Add(window);
			}
#endif
			window.InputBindings.Add(new KeyBinding(Commands.InvokeViewModel, new KeyGesture(Key.Escape)) {
				CommandParameter = "TryClose"
			});
			window.ShowDialog();
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
