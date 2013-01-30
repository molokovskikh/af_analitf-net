using System.Data;
using System.Data.Common;
using NHibernate.Dialect.Schema;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class DevartMySqlSchema : MySQLDataBaseSchema
	{
		public DevartMySqlSchema(DbConnection connection)
			: base(connection)
		{
		}

		public override string ColumnNameForTableName
		{
			get
			{
				return "Name";
			}
		}

		public override DataTable GetTables(string catalog, string schemaPattern, string tableNamePattern, string[] types)
		{
			return base.GetTables("data", schemaPattern, tableNamePattern, types);
		}

		public override ITableMetadata GetTableMetadata(DataRow rs, bool extras)
		{
			return new DevartMySqlTableMetadata(rs, this, extras);
		}

		public override DataTable GetColumns(string catalog, string schemaPattern, string tableNamePattern, string columnNamePattern)
		{
			var restrictions = new[] {catalog, tableNamePattern, columnNamePattern};
			return Connection.GetSchema("Columns", restrictions);
		}

		public override DataTable GetIndexInfo(string catalog, string schemaPattern, string tableName)
		{
			var restrictions = new[] {catalog, tableName, null};
			var t = Connection.GetSchema("Indexes", restrictions);;
			return Connection.GetSchema("Indexes", restrictions);
		}

		public override DataTable GetIndexColumns(string catalog, string schemaPattern, string tableName, string indexName)
		{
			var restrictions = new[] {catalog, tableName, indexName, null};
			var t = Connection.GetSchema("IndexColumns", restrictions);;
			return Connection.GetSchema("IndexColumns", restrictions);
		}

		public override DataTable GetForeignKeys(string catalog, string schema, string table)
		{
			var restrictions = new[] {catalog, table, null};
			var t = Connection.GetSchema("ForeignKeyColumns", restrictions);;
			return Connection.GetSchema("ForeignKeyColumns", restrictions);
		}
	}
}