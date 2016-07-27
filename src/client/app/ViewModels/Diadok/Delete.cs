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

		public void Save()
		{
			try
			{
				BeginAction();
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				Payload.Api.Delete(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId, Payload.Entity.EntityId);
				EndAction();
			}
			catch(Exception exception)
			{
				EndAction(false);
				if(exception is HttpClientException)
				{
					var e = exception as HttpClientException;
					Log.Warn($"Ошибка:", e);
					Manager.Error(e.AdditionalMessage);
				}
				else
					throw;
			}
			TryClose();
		}
	}
}