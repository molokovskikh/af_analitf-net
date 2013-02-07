using System;
using System.Data;
using System.Reflection;
using AnalitF.Net.Models;
using Common.NHibernate;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace AnalitF.Net.Config.Initializers
{
	public class NHibernate : BaseNHibernate
	{
		public override void Init()
		{
			Mapper.Class<RequestLog>(m => {
				m.Schema("logs");
				m.Property(l => l.Version, pm => pm.Type<VersionType>());
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

			Configuration.AddInputStream(HbmSerializer.Default.Serialize(Assembly.Load("Common.Models")));
			base.Init();
		}
	}

	public class VersionType : IUserType
	{
		public new bool Equals(object x, object y)
		{
			return object.Equals(x, y);
		}

		public int GetHashCode(object x)
		{
			return x.GetHashCode();
		}

		public object NullSafeGet(IDataReader rs, string[] names, object owner)
		{
			var obj = NHibernateUtil.String.NullSafeGet(rs, names[0]);
			if (obj == null)
				return null;

			var version = new Version();
			if (Version.TryParse((string)obj, out version))
				return version;

			return null;
		}

		public void NullSafeSet(IDbCommand cmd, object value, int index)
		{
			if (value == null) {
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			}
			else {
				((IDataParameter)cmd.Parameters[index]).Value = value.ToString();
			}
		}

		public object DeepCopy(object value)
		{
			return ((Version)value).Clone();
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public object Assemble(object cached, object owner)
		{
			return cached;
		}

		public object Disassemble(object value)
		{
			return value;
		}

		public SqlType[] SqlTypes
		{
			get { return new[] { NHibernateUtil.String.SqlType }; }
		}

		public Type ReturnedType
		{
			get { return typeof(Version); }
		}

		public bool IsMutable
		{
			get { return false; }
		}
	}
}