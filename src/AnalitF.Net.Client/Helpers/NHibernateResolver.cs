using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate.Proxy;
using Newtonsoft.Json.Serialization;

namespace AnalitF.Net.Client.Helpers
{
	public class NHibernateResolver : DefaultContractResolver
	{
		protected override List<MemberInfo> GetSerializableMembers(Type objectType)
		{
			if (typeof(INHibernateProxy).IsAssignableFrom(objectType))
				return base.GetSerializableMembers(objectType.BaseType);
			else
				return base.GetSerializableMembers(objectType);
		}
	}
}