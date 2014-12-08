using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.Fixtures;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Service;
using AnalitF.Net.Service.Config.Environments;
using Common.Tools;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Fixture
	{
		[Description("Выводит список всех наборов тестовых данных")]
		public void List(string pattern = null)
		{
			var types = GetTypes();
			types = types.Where(t => String.IsNullOrEmpty(pattern) || t.Name.ToLower().Contains(pattern)).OrderBy(t => t.Name);

			foreach (var type in types) {
				var desc = DescriptionHelper.GetDescription(type);
				Console.Write(type.Name);
				if (!String.IsNullOrEmpty(desc)) {
					Console.Write(" - ");
					Console.Write(desc);
				}
				Console.WriteLine();
			}

			foreach (var method in GetMethods().OrderBy(m => m.Name)) {
				var desc = DescriptionHelper.GetDescription(method);
				Console.Write(method.Name);
				if (!String.IsNullOrEmpty(desc)) {
					Console.Write(" - ");
					Console.Write(desc);
				}
				Console.WriteLine();
			}
		}

		private static MethodInfo[] GetMethods()
		{
			return typeof(SimpleFixture).GetMethods(BindingFlags.Static | BindingFlags.Public);
		}

		[Description("Применяет указанный набор тестовых данных")]
		public void Execute(string name, int count = 1)
		{
			var type = GetTypes().FirstOrDefault(t => t.Name.Match(name));
			var method = GetMethods().FirstOrDefault(m => m.Name.Match(name));
			if (type == null && method == null) {
				Console.WriteLine("Не удалось найти набор тестовых данных '{0}'," +
					" использую list что просмотреть доступные наборы", name);
				return;
			}

			for(var i = 0; i < count; i++) {
				if (type != null) {
					new FixtureHelper(verbose: true).Run(type);
				}
				else {
					using (var session = FixtureHelper.GetFactory().OpenSession()) {
						if (method.GetParameters().Length == 1)
							method.Invoke(null, new object[] { session });
						else
							method.Invoke(null, new object[] { session, true });
					}
				}
			}
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