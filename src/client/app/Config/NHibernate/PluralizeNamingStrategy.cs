using Inflector;
using NHibernate.Cfg;

namespace AnalitF.Net.Client.Config.NHibernate
{
	public class PluralizeNamingStrategy : INamingStrategy
	{
		public string ClassToTableName(string className)
		{
			var name = DefaultNamingStrategy.Instance.ClassToTableName(className);
			return name.Pluralize();
		}

		public string PropertyToColumnName(string propertyName)
		{
			return DefaultNamingStrategy.Instance.PropertyToColumnName(propertyName);
		}

		public string TableName(string tableName)
		{
			return DefaultNamingStrategy.Instance.TableName(tableName);
		}

		public string ColumnName(string columnName)
		{
			return DefaultNamingStrategy.Instance.ColumnName(columnName);
		}

		public string PropertyToTableName(string className, string propertyName)
		{
			return DefaultNamingStrategy.Instance.PropertyToTableName(className, propertyName);
		}

		public string LogicalColumnName(string columnName, string propertyName)
		{
			return DefaultNamingStrategy.Instance.LogicalColumnName(columnName, propertyName);
		}
	}
}