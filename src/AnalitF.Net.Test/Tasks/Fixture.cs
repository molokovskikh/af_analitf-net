using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Tools;
using NHibernate;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Test.Tasks
{
	public class Fixture
	{
		public Fixture()
		{
			Environment.CurrentDirectory = @"C:\Projects\Production\AnalitF.Net\src\AnalitF.Net.Client\bin\run";
		}

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
		public void Execute(string name)
		{
			Console.WriteLine(Environment.CurrentDirectory);
			var type = GetTypes().FirstOrDefault(t => t.Name.Match(name));
			if (type == null) {
				Console.WriteLine("Не удалось найти набор тестовых данных '{0}'," +
					" использую list что просмотреть доступные наборы");
				return;
			}
			var nhibernate = new Config.Initializers.NHibernate();
			nhibernate.UseRelativePath = true;
			nhibernate.Init();

			using(var session = nhibernate.Factory.OpenSession()) {
				using (var transaction = session.BeginTransaction()) {
					var fixture = (dynamic)Activator.CreateInstance(type);
					fixture.Execute(session);
					transaction.Commit();
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