using System;
using NHibernate.Type;

namespace AnalitF.Net.Client.Config.Initializers
{
	//в базе всегда храниться время в utc, при загрузке данных
	//преобразуем utc -> локальное
	//при сохранении нооборот
	public class UtcToLocalDateTimeType : DateTimeType
	{
		public override object Get(System.Data.IDataReader rs, int index)
		{
			var value = (DateTime)base.Get(rs, index);
			if (value == DateTime.MinValue)
				return value;
			if (value == DateTime.MaxValue)
				return value;
			return value.ToLocalTime();
		}

		public override void Set(System.Data.IDbCommand st, object value, int index)
		{
			base.Set(st, ((DateTime)value).ToUniversalTime(), index);
		}
	}
}