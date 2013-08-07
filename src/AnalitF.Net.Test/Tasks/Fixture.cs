using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
		public void Execute(string name)
		{
			var type = GetTypes().FirstOrDefault(t => t.Name.Match(name));
			if (type == null) {
				Console.WriteLine("Не удалось найти набор тестовых данных '{0}'," +
					" использую list что просмотреть доступные наборы", name);
				return;
			}

			var fixture = (dynamic)Activator.CreateInstance(type);
			var local = fixture.Local;
			if (local) {
				var nhibernate = new Config.Initializers.NHibernate();
				nhibernate.UseRelativePath = true;
				nhibernate.Init();

				using(var session = nhibernate.Factory.OpenSession()) {
					using (var transaction = session.BeginTransaction()) {
						fixture.Execute(session);
						transaction.Commit();
					}
				}
			}
			else {
				var config = Application.ReadConfig();
				var development = new Development();
				development.BasePath = Environment.CurrentDirectory;
				development.Run(config);
				fixture.Config = config;
				global::Test.Support.Setup.Initialize("local");
				using(var session = global::Test.Support.Setup.SessionFactory.OpenSession()) {
					using (var transaction = session.BeginTransaction()) {
						fixture.Execute(session);
						transaction.Commit();
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