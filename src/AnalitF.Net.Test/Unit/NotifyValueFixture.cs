using System;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

		public class MyClass : BaseNotify
		{
			private int data;

			public int Data
			{
				get { return data; }
				set
				{
					data = value;
					OnPropertyChanged();
				}
			}
		}

		[Test]
		public void Values_changes()
		{
			var count = 0;
			var p = new NotifyValue<MyClass>();
			p.ChangedValue().Subscribe(_ => count++);
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
			p.ChangedValue().Subscribe(_ => count++);
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

		[Test]
		public void Initial_value()
		{
			var v = new NotifyValue<string>("123", () => { throw new NotImplementedException(); });
			Assert.AreEqual("123", v.Value);
		}

		[Test]
		public void Has_value()
		{
			var v = new NotifyValue<string>();
			var changes = v.CollectChanges();
			Assert.IsFalse(v.HasValue);
			v.Value = "1";
			Assert.IsTrue(v.HasValue);
			Assert.AreEqual(1, changes.Count(c => c.PropertyName == "HasValue"));
		}

		[Test]
		public void Notify_value_from_observable()
		{
			var s = new Subject<string>();
			var v = s.ToValue();
			var changes = v.Changed().Select(e => e.EventArgs.PropertyName).Collect();
			s.OnNext("1");
			Assert.AreEqual("1", v.Value);
			Assert.That(changes, Is.EqualTo(new[] { "Value" }));
		}
	}
}