using AnalitF.Net.Client.Models.Commands;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class UpdateCommandFixture
	{
		[Test]
		public void Calculate_message()
		{
			var command = new UpdateCommand();
			command.SyncData = "Waybills";
			Assert.AreEqual("Получение документов завершено успешно.", command.SuccessMessage);
		}
	}
}