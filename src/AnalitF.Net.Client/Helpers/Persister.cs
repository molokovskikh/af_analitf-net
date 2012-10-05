namespace AnalitF.Net.Client.Helpers
{
	public class Persister
	{
		public static void SaveState()
		{
			//var serializer = JsonSerializer.Create(new JsonSerializerSettings {
			//	ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			//});
			////var serializer = new XmlSerializer(Shell.GetType());
			//using (var stream = new StreamWriter(File.OpenWrite("state")))
			//	serializer.Serialize(stream, Shell);
		}

		public static void LoadState()
		{
			//var serializer = JsonSerializer.Create(new JsonSerializerSettings {
			//	ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			//});
			//var state = "state";
			//if (File.Exists(state)) {
			//	using (var stream = new StreamReader(File.OpenRead(state))) {
			//		Shell = (ShellViewModel)serializer.Deserialize(stream, typeof(ShellViewModel));
			//	}
			//}
		}
	}
}