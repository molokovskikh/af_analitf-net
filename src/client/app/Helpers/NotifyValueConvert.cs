using System;
using Newtonsoft.Json;

namespace AnalitF.Net.Client.Helpers
{
	public class NotifyValueConvert : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null) {
				writer.WriteNull();
				return;
			}
			serializer.Serialize(writer, ((IValue)value).Value);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			existingValue = existingValue ?? Activator.CreateInstance(objectType);
			((IValue)existingValue).Value = serializer.Deserialize(reader, objectType.GetGenericArguments()[0]);
			return existingValue;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(NotifyValue<>);
		}
	}
}