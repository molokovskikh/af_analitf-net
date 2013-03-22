using System.Data;
using System.Data.Common;
using NHibernate.Dialect;
using NHibernate.Dialect.Schema;

namespace AnalitF.Net.Client.Config.Initializers
{
	public class DevartMySqlDialect : MySQL5Dialect
	{
		public DevartMySqlDialect()
		{
			//http://dev.mysql.com/doc/refman/5.0/en/fixed-point-types.html
			//по умолчанию mysql интерпретирует decimal как decimal(10, 0) обрезая дробную часть
			RegisterCastType(DbType.Decimal, "DECIMAL(19,5)");
			RegisterCastType(DbType.Double, "DECIMAL(19,5)");
			RegisterCastType(DbType.Single, "DECIMAL(19,5)");
		}

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
}