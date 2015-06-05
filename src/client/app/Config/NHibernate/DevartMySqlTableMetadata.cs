using System;
using System.Data;
using NHibernate.Dialect.Schema;

namespace AnalitF.Net.Client.Config.NHibernate
{
	public class DevartMySqlTableMetadata : AbstractTableMetadata
	{
		public DevartMySqlTableMetadata(DataRow rs, IDataBaseSchema meta, bool extras) : base(rs, meta, extras)
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