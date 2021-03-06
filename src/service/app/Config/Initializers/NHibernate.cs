﻿using System;
using System.Linq;
using System.Reflection;
using AnalitF.Net.Service.Models;
using AnalitF.Net.Service.Models.Inventory;
using Common.Models;
using Common.NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;
using SmartOrderFactory.Domain;

namespace AnalitF.Net.Service.Config.Initializers
{
	public class NHibernate : BaseNHibernate
	{
		public override void Init()
		{
			Excludes.Add(typeof(ClientOrderItem));
			Excludes.Add(typeof(ClientOrder));
			Excludes.Add(typeof(ProducerPromotion));

			Configuration.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
			Configuration.AddInputStream(HbmSerializer.Default.Serialize(typeof(SmartOrderRule).Assembly));

			Mapper.Class<Stock>(m => {
				m.Schema("Inventory");
				m.Version(x => x.Version, _ => {});
				m.Property(x => x.Timestamp, p => {
					p.Insert(false);
					p.Update(false);
				});
			});
			Mapper.Class<AnalitfNetData>(m => {
				m.Schema("Customers");
				m.Id(p => p.Id, c => {
					c.Generator(Generators.Foreign<AnalitfNetData>(d => d.User));
					c.Column("UserId");
				});
				m.OneToOne(p => p.User, c => c.ForeignKey("Id"));
			});

			Mapper.Class<UserSettings>(m => {
				m.Schema("Customers");
				m.Table("Users");
			});
			Mapper.Class<Promotion>(m => {
				m.Schema("UserSettings");
				m.Table("SupplierPromotions");
			});

			Mapper.Class<ClientSettings>(m => {
				m.Schema("Usersettings");
				m.Table("RetClientsSet");
				m.Id(e => e.Id, c => c.Column("ClientCode"));
			});

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

			Mapper.Class<Attachment>(m => m.Schema("Documents"));

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

			base.Init();
		}
	}
}