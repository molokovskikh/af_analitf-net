using System;
using System.Data;
using System.Data.Common;
using Common.Tools;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;
using NHibernate.Driver;
using NHibernate.Engine;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class DevArtDriver : ReflectionBasedDriver
	{
		public override bool UseNamedPrefixInSql
		{
			get
			{
				return true;
			}
		}

		public override bool UseNamedPrefixInParameter
		{
			get
			{
				return true;
			}
		}

		public override string NamedPrefix
		{
			get
			{
				return "@";
			}
		}

		public override bool SupportsMultipleOpenReaders
		{
			get
			{
				return false;
			}
		}

		protected override bool SupportsPreparingCommands
		{
			get
			{
				return false;
			}
		}

		public override bool SupportsMultipleQueries
		{
			get
			{
				return true;
			}
		}

		public DevArtDriver()
			: base("Devart.Data.MySql", "Devart.Data.MySql", "Devart.Data.MySql.MySqlConnection", "Devart.Data.MySql.MySqlCommand")
		{
		}

		public override IResultSetsCommand GetResultSetsCommand(ISessionImplementor session)
		{
			return new BasicResultSetsCommand(session);
		}
	}

	public class DevartMySqlDialect : MySQL5Dialect
	{
		public override IDataBaseSchema GetDataBaseSchema(DbConnection connection)
		{
			return new DevartMySqlSchema(connection);
		}

		public override bool SupportsForeignKeyConstraintInAlterTable
		{
			get
			{
				return true;
			}
		}
	}

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
			return new DevArtMySqlTableMetadata(rs, this, extras);
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

	public class DevArtMySqlTableMetadata : AbstractTableMetadata
	{
		public DevArtMySqlTableMetadata(DataRow rs, IDataBaseSchema meta, bool extras) : base(rs, meta, extras)
		{
		}

		protected override void ParseTableInfo(DataRow rs)
		{
			Catalog = Convert.ToString(rs["Database"]);
			if (string.IsNullOrEmpty(Catalog)) Catalog = null;
			Name = Convert.ToString(rs["Name"]);
		}

		protected override string GetConstraintName(DataRow rs)
		{
			throw new System.NotImplementedException();
		}

		protected override string GetColumnName(DataRow rs)
		{
			return Convert.ToString(rs["Name"]);
		}

		protected override string GetIndexName(DataRow rs)
		{
			return Convert.ToString(rs["Index"]);
		}

		protected override IColumnMetadata GetColumnMetadata(DataRow rs)
		{
			return new DevartMySQLColumnMetadata(rs);
		}

		protected override IForeignKeyMetadata GetForeignKeyMetadata(DataRow rs)
		{
			throw new System.NotImplementedException();
		}

		protected override IIndexMetadata GetIndexMetadata(DataRow rs)
		{
			return new DevartMySQLIndexMetadata(rs);
		}
	}

	public class DevartMySQLIndexMetadata : AbstractIndexMetadata
	{
		public DevartMySQLIndexMetadata(DataRow rs)
			: base(rs)
		{
			Name = Convert.ToString(rs["Index"]);
		}
	}

	public class DevartMySQLColumnMetadata : AbstractColumnMetaData
	{
		public DevartMySQLColumnMetadata(DataRow rs)
			: base(rs)
		{
			Name = Convert.ToString(rs["Name"]);
			Nullable = Convert.ToString(rs["Nullable"]);
			TypeName = Convert.ToString(rs["DataType"]);
			SetColumnSize(rs["Length"]);
			SetNumericalPrecision(rs["Precision"]);
		}
	}
}