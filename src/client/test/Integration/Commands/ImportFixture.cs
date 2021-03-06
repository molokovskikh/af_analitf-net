﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate.Linq;
using NUnit.Framework;
using AnalitF.Net.Client.Test.TestHelpers;
using Test.Support;
using NHibernate;
//using Common.Tools.Helpers;
using System.IO;

namespace AnalitF.Net.Client.Test.Integration.Commands
{
	[TestFixture]
	public class ImportFixture : DbFixture
	{
		[Test]
		public void Import_future_data()
		{
			restore = true;
			var data = new List<Tuple<string, string[]>> {
				Tuple.Create(TempFile("Users.txt", "5\ttest\t"), new[] { "Id", "NonExistsColumn" })
			};
			var cmd = InitCmd(new ImportCommand(data){
				Strict = false
			});
			cmd.Execute();
		}
	}
}