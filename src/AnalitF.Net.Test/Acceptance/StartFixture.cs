using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using Common.Tools;
using Common.Tools.Calendar;
using NUnit.Framework;
using Remotion.Linq.Parsing;

namespace AnalitF.Net.Client.Test.Acceptance
{
	[TestFixture, Explicit]
	public class StartFixture : BaseFixture
	{
		[Test]
		public void Start_only_one()
		{
			HandleDialogs();
			Activate();

			var process = Start();
			Assert.IsTrue(process.WaitForExit((int)10.Second().TotalMilliseconds));
		}

		[Test]
		public void Wait_for_shutdown_before_start()
		{
			HandleDialogs();
			Activate();
			Close();
			var oldProcess = Process;
			Assert.IsFalse(Process.HasExited);

			Timeout = 30.Second();
			Process = Start();
			Console.WriteLine(Process.Id);
			WaitMainWindow();

			Assert.IsTrue(oldProcess.HasExited);
		}

		[Test]
		public void Fast_startup()
		{
			HandleDialogs();
			var windows = new List<Tuple<int, string>>();
			WindowHandle = w => windows.Add(Tuple.Create(
				(int)w.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty),
				String.Format("{0}({1})",
					w.GetCurrentPropertyValue(AutomationElement.LocalizedControlTypeProperty),
					w.GetCurrentPropertyValue(AutomationElement.NameProperty))));

			Process = Start();
			Thread.Sleep(300);
			var process2 = Start();
			WaitMainWindow();

			Assert.IsFalse(Process.HasExited);
			Assert.IsTrue(process2.WaitForExit((int)10.Second().TotalMilliseconds));
			Assert.That(windows.Where(w => w.Item1 == Process.Id).Implode(t => t.Item2),
				Is.StringContaining("окно(АналитФАРМАЦИЯ)"));
			Assert.That(windows.Where(w => w.Item1 == process2.Id).Implode(t => t.Item2), Is.Empty);
		}

		private void Close()
		{
			var close = (WindowPattern)MainWindow.GetCurrentPattern(WindowPattern.Pattern);
			close.WaitForInputIdle((int)10.Second().TotalMilliseconds);
			close.Close();
			close.WaitForInputIdle((int)10.Second().TotalMilliseconds);
			MainWindow = null;
		}

		private Process Start()
		{
			return StartProcess(exe, root);
		}
	}
}
