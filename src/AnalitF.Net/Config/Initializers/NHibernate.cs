using System.Reflection;
using AnalitF.Net.Models;
using Common.NHibernate;
using NHibernate.Mapping.Attributes;

namespace AnalitF.Net.Config.Initializers
{
	public class NHibernate : BaseNHibernate
	{
		public override void Init()
		{
			Mapper.Class<RequestLog>(m => m.Schema("logs"));

			Configuration.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
			base.Init();
		}
	}
}