using System;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api;
using Diadoc.Api.Http;

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
			/*
			try
			{
				BeginAction();
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				await Async(x => Payload.Api.Delete(x, Payload.BoxId,
					Payload.Entity.DocumentInfo.MessageId,
					Payload.Entity.EntityId));
			}
			catch(HttpClientException e)
			{
				Log.Warn($"Ошибка:", e);
				Manager.Error(e.AdditionalMessage);
			}
			finally
			{
				await EndAction();
			}*/
		}
	}
}