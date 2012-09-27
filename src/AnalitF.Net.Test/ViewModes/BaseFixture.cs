using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Test.ViewModes
{
	public class BaseFixture
	{
		protected Client.Extentions.WindowManager manager;

		[SetUp]
		public void Setup()
		{
			manager = new Client.Extentions.WindowManager();
			manager.UnderTest = true;
			IoC.GetInstance = (type, key) => {
				return manager;
			};
		}
	}
}