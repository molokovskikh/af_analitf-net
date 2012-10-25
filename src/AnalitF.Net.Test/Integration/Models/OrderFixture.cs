using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class OrderFixture
	{
		//тест для ситуации когда для загруженных объектов не работает биндинг
		//измещение о новыйх данных ui не обрабатывает
		//проблема в том что ui биндится к прокси а объект посылает извещения от себя
		[Test]
		public void Send_order_notification()
		{
			using(var session = SetupFixture.Factory.OpenSession()) {
				uint orderId = 0;
				var order = new Order();
				session.Save(order);
				session.Flush();
				orderId = order.Id;
				session.Clear();

				object eventSender = null;
				order = session.Load<Order>(orderId);
				order.PropertyChanged += (sender, args) => {
					eventSender = sender;
				};
				order.Sum = 100;
				Assert.That(eventSender.GetHashCode(), Is.EqualTo(order.GetHashCode()));
			}
		}
	}
}