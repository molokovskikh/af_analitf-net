using System.Runtime.Serialization;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SaveViewModelFixture : BaseFixture
	{
		[DataContract]
		public class Model : BaseScreen
		{
			[DataMember]
			public bool Test;
		}

		[Test]
		public void Save_view_model()
		{
			var model = Init(new Model());
			shell.ActivateItem(model);
			model.Test = true;
			model.TryClose();
			model = Init(new Model());
			shell.ActivateItem(model);
			Assert.That(model.Test, Is.True);
		}
	}
}