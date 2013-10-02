using System;
using System.Reflection;
using AnalitF.Net.Service.Models;
using Common.NHibernate;
using NHibernate.Mapping.Attributes;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class NHibernate : BaseNHibernate
	{
		public override void Init()
		{
			Excludes.Add(typeof(ClientOrderItem));

			Mapper.Class<ClientAppLog>(m => {
				m.Property(p => p.Text, c => c.Length(10000));
			});

			Mapper.Class<UserPrice>(m => {
				m.Schema("Customers");
				m.ComposedId(i => {
					i.Property(p => p.RegionId);
					i.ManyToOne(p => p.Price);
					i.ManyToOne(p => p.User);
				});
				m.ManyToOne(p => p.Price);
				m.ManyToOne(p => p.User);
				m.Property(p => p.RegionId);
			});

			Mapper.Class<DocumentLog>(m => {
				m.Table("Document_Logs");
				m.Id(l => l.Id, i => i.Column("RowId"));
				m.ManyToOne(l => l.Supplier, i => i.Column("FirmCode"));
			});

			Mapper.AfterMapClass += (i, t, c) => {
				if (t.Name.EndsWith("Log")) {
					c.Schema("Logs");
				}
			};

			Configuration.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
			base.Init();
		}
	}
}