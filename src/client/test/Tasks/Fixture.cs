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
	public class ServiceAttribute : Attribute
	{ }

	public class Fixture
	{
		[Description("Выводит список всех наборов тестовых данных")]
		public void List(string pattern = null)
		{
			var types = GetTypes();
			types = types.Where(t => String.IsNullOrEmpty(pattern) || t.Name.ToLower().Contains(pattern)).OrderBy(t => t.Name);

			foreach (var type in types) {
				WriteDesc(type.Name, type);
			}

			foreach (var method in GetMethods().OrderBy(m => m.Name)) {
				WriteDesc(method.Name, method);
			}
		}

		private static void WriteDesc(string text, ICustomAttributeProvider method)
		{
			var desc = DescriptionHelper.GetDescription(method);
			if (!String.IsNullOrEmpty(desc)) {
				text += " - ";
				text += desc;
			}
			Console.WriteLine(text);
			//на подумать, получается все равно криво
			//var rest = text.Length;
			//var begin = 0;
			//while (rest > 0) {
			//	var length = Math.Min(Math.Min(Console.WindowWidth - 1, 64), rest);
			//	var line = text.Substring(begin, length);
			//	if (begin > 0)
			//		Console.Write("    ");
			//	Console.WriteLine(line);
			//	begin += length;
			//	rest -= length;
			//}
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
					" используй list что просмотреть доступные наборы", name);
				return;
			}

			for(var i = 0; i < count; i++) {
				if (type != null) {
					new FixtureHelper(verbose: true).Run(type);
				}
				else {
					var factory = FixtureHelper.GetFactory();
					if (method.GetCustomAttributes(typeof(ServiceAttribute)).Any()) {
						factory = DbHelper.ServerNHConfig("local");
					}

					using (var session = factory.OpenSession())
					using (session.BeginTransaction()) {
						var infos = method.GetParameters();
						if (infos.Length > 1 && infos[1].ParameterType == typeof(bool)) {
							method.Invoke(null, new object[] { session, true });
						}
						else {
							method.Invoke(null, new object[] { session });
						}
						session.Transaction.Commit();
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