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
			: base("Devart.Data.MySql", "MyDevart.Data.MySql", "Devart.Data.MySql.MySqlConnection", "Devart.Data.MySql.MySqlCommand")
		{
		}

		public override IResultSetsCommand GetResultSetsCommand(ISessionImplementor session)
		{
			return new BasicResultSetsCommand(session);
		}

	}
}