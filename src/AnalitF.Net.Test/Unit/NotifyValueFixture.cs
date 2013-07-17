using System;
using System.ComponentModel;
using System.Dynamic;
using AnalitF.Net.Client.Helpers;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class NotifyValueFixture
	{
		[Test]
		public void Dependend_property()
		{
			var p1 = new NotifyValue<int>(1);
			var p2 = new NotifyValue<int>(() => p1.Value + 1, p1);
			Assert.That(p2.Value, Is.EqualTo(2));
			p1.Value = 2;
			Assert.That(p2.Value, Is.EqualTo(3));
		}

		public class MyClass : INotifyPropertyChanged
		{
			private int data;

			public int Data
			{
				get { return data; }
				set
				{
					data = value;
					OnPropertyChanged("Data");
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			protected virtual void OnPropertyChanged(string propertyName)
			{
				var handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		[Test]
		public void Values_changes()
		{
			var count = 0;
			var p = new NotifyValue<MyClass>();
			p.ValueUpdated().Subscribe(_ => count++);
			p.Value = new MyClass();

			Assert.AreEqual(count, 0);
			p.Value.Data = 1;
			Assert.AreEqual(count, 1);
			p.Value.Data = 2;
			Assert.AreEqual(count, 2);
			var old = p.Value;
			p.Value = new MyClass();
			old.Data = 1;
			Assert.AreEqual(count, 2);
			p.Value.Data = 1;
			Assert.AreEqual(count, 3);
		}

		[Test]
		public void Observ_on_null()
		{
			var count = 0;
			var p = new NotifyValue<MyClass>();
			p.ValueUpdated().Subscribe(_ => count++);
			p.Value = new MyClass();
			var old = p.Value;
			p.Value = null;
			old.Data = 1;
			Assert.AreEqual(0, count);
		}

		[Test]
		public void Notify_value_with_fallback()
		{
			var depended = new NotifyValue<int>(5);
			var v = new NotifyValue<int>(true, () => depended + 1, depended);
			var changed = v.Value;
			v.Changed().Subscribe(_ => changed = v.Value);
			Assert.AreEqual(6, v.Value);
			depended.Value = 7;
			Assert.AreEqual(8, v.Value);
			Assert.AreEqual(8, changed);
			v.Value = 1;
			Assert.AreEqual(1, v.Value);
			Assert.AreEqual(1, changed);
			depended.Value = 50;
			Assert.AreEqual(1, v.Value);
			Assert.AreEqual(1, changed);
		}

		[Test]
		public void Update_value_with_fallback()
		{
			var depended = new NotifyValue<int>(5);
			var v = new NotifyValue<int>(true, () => depended + 1, depended);
			var changed = v.Value;
			v.Changed().Subscribe(_ => changed = v.Value);

			depended.Value += 5;
			depended.Value += 5;
			Assert.AreEqual(v.Value, 16);
			Assert.AreEqual(changed, 16);
		}
	}
}