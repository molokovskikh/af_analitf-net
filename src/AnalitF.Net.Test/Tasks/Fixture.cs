using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using Common.Tools;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Fixture
	{
		[Description("Выводит список всех наборов тестовых данных")]
		public void List(string pattern = null)
		{
			var types = GetTypes();
			types = types.Where(t => String.IsNullOrEmpty(pattern) || t.Name.ToLower().Contains(pattern));

			foreach (var type in types) {
				Console.WriteLine(type.Name);
			}
		}

		[Description("Применяет указанный набор тестовых данных")]
		public void Execute(string name, int count = 1)
		{
			var type = GetTypes().FirstOrDefault(t => t.Name.Match(name));
			if (type == null) {
				Console.WriteLine("Не удалось найти набор тестовых данных '{0}'," +
					" использую list что просмотреть доступные наборы", name);
				return;
			}

			for(var i = 0; i < count; i++)
				new FixtureHelper().Run(type);
		}

		private static IEnumerable<Type> GetTypes()
		{
			var types = typeof(Fixture).Assembly.GetTypes().Where(t => t.IsClass
				&& t.IsPublic
				&& !t.IsAbstract
				&& t.Namespace != null
				&& t.Namespace.EndsWith("Fixtures"));
			return types;
		}
	}
}