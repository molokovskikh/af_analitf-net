using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Proto.Invoicing;
using Diadoc.Api.Http;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Revocation : DiadokAction
	{
		public Revocation(ActionPayload payload)
			: base(payload)
		{
		}

		public NotifyValue<string> Comment { get; set; }

		public async void Save()
		{
			try
			{
				BeginAction();
				var patch = Payload.Patch();

				RevocationRequestInfo revinfo = new RevocationRequestInfo();
				revinfo.Comment = Comment.Value;
				revinfo.Signer = GetSigner();

				GeneratedFile revocationXml = await Async((x) => Payload.Api.GenerateRevocationRequestXml(
					x,
					Payload.BoxId,
					Payload.Message.MessageId,
					Payload.Entity.EntityId,
					revinfo));

				SignedContent revocSignContent = new SignedContent();
				revocSignContent.Content = revocationXml.Content;

				if(!TrySign(revocSignContent))
					throw new Exception();

				RevocationRequestAttachment revattch = new RevocationRequestAttachment();
				revattch.ParentEntityId = Payload.Entity.EntityId;
				revattch.SignedContent = revocSignContent;

				patch.AddRevocationRequestAttachment(revattch);
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				await Async(x => Payload.Api.PostMessagePatch(x, patch));
				EndAction();
			}
			catch(Exception exception)
			{
				if(exception is HttpClientException)
				{
					var e = exception as HttpClientException;
					Log.Warn($"Ошибка:", e);
					Manager.Error(e.AdditionalMessage);
				}
				else
					throw;
			}
		}
	}
}