using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using System.Xaml;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Castle.Components.DictionaryAdapter;
using Common.Tools;
using Common.Tools.Calendar;
using Inflector;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using ReactiveUI;
using ReactiveUI.Testing;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class UtilFixture
	{
		public class Test
		{
			public Test Item { get; set; }

			public string Name { get; set; }

			public int I;
		}

		[Test]
		public void Set_value()
		{
			var d = new Test { Item = new Test { Name = "123" } };
			Util.SetValue(d, "Item.Name", "456");
			Assert.AreEqual("456", Util.GetValue(d, "Item.Name"));
			Util.SetValue(d, "I", 1);
			Assert.AreEqual(1, d.I);
		}

		[Test]
		public void Get_value()
		{
			Assert.AreEqual(1, Util.GetValue(new Test { I = 1 }, "I"));
		}
	}
}