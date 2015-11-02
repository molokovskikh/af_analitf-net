using System;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Delete : DiadokAction
	{
		public Delete(ActionPayload payload)
			: base(payload)
		{
		}

		public async Task Save()
		{
			await Async(x => Payload.Api.Delete(x, Payload.BoxId,
				Payload.Entity.DocumentInfo.MessageId,
				Payload.Entity.EntityId));
		}
	}
}