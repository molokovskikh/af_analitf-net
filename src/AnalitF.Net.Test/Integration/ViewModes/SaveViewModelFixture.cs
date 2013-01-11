﻿using System.Runtime.Serialization;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class SaveViewModelFixture : BaseFixture
	{
		private Model model;

		[DataContract]
		public class Model : BaseScreen
		{
			private bool test;

			[DataMember]
			public bool Test
			{
				get { return test; }
				set
				{
					test = value;
					NotifyOfPropertyChange("Test");
				}
			}
		}

		[SetUp]
		public void Setup()
		{
			model = Init(new Model());
			shell.ActivateItem(model);
			model.Test = true;
			model.TryClose();
		}

		[Test]
		public void Save_view_model()
		{
			model = Init(new Model());
			shell.ActivateItem(model);
			Assert.That(model.Test, Is.True);
		}

		[Test]
		public void Do_not_notify_about_changes_on_deserialize()
		{
			model = new Model();
			var changes = TrackChanges(model);
			model = Init(model);
			Assert.That(changes.Implode(), Is.Not.StringContaining("Test"));
		}
	}
}