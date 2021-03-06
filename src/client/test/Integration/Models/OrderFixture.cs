﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.Models
{
	[TestFixture]
	public class OrderFixture : DbFixture
	{
		//тест для ситуации когда для загруженных объектов не работает биндинг
		//извещение о новых данных ui не обрабатывает
		//проблема в том что ui биндится к прокси а объект посылает извещения от себя
		[Test]
		public void Send_order_notification()
		{
			uint orderId = 0;
			var order = new Order();
			session.Save(order);
			session.Flush();
			orderId = order.Id;
			session.Clear();

			var events = new List<System.Tuple<object, PropertyChangedEventArgs>>();
			order = session.Load<Order>(orderId);
			order.PropertyChanged += (sender, args) => {
				events.Add(Tuple.Create(sender, args));
			};
			order.Sum = 100;
			Assert.That(events.Count, Is.EqualTo(3));
			Assert.That(events[0].Item2.PropertyName, Is.EqualTo("Sum"));
			Assert.That(events[0].Item1.GetHashCode(), Is.EqualTo(order.GetHashCode()));
			Assert.That(events[1].Item2.PropertyName, Is.EqualTo("IsInvalid"));
			Assert.That(events[1].Item1.GetHashCode(), Is.EqualTo(order.GetHashCode()));
			Assert.That(events[2].Item2.PropertyName, Is.EqualTo("IsOverLimit"));
			Assert.That(events[2].Item1.GetHashCode(), Is.EqualTo(order.GetHashCode()));
		}
	}
}