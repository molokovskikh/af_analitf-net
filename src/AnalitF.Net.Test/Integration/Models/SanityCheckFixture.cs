using System.IO;
using AnalitF.Net.Client.Models;
using Common.Tools;
using Devart.Data.MySql;
using NUnit.Framework;

namespace AnalitF.Net.Test.Integration.Models
{
	[TestFixture]
	public class SanityCheckFixture
	{
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			FileHelper.InitDir("backup");
			Directory.GetFiles("data")
				.Each(f => File.Copy(f, Path.Combine("backup", Path.GetFileName(f)), true));
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			Directory.GetFiles("backup")
				.Each(f => File.Copy(f, Path.Combine("data", Path.GetFileName(f)), true));
		}

		[Test]
		public void Make_check()
		{
			var check = new SanityCheck("data");
			check.Check();
		}

		[Test, Ignore("Тест не работает тк нельзя удалить директорию с данными тк в ней сидит mysql а способа остановить mysql нет")]
		public void Create_local_db()
		{
			MySqlConnection.ClearAllPools(true);
			var dataPath = "data";
			if (Directory.Exists(dataPath))
				Directory.GetFiles("data").Each(f => File.Delete(f));
				//Directory.Delete(dataPath, true);

			var sanityCheck = new SanityCheck(dataPath);
			sanityCheck.Check();

			Assert.That(Directory.Exists(dataPath));
		}
	}
}