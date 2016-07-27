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

		public void Save()
		{
			try
			{
				BeginAction();
				var patch = Payload.Patch();

				RevocationRequestInfo revinfo = new RevocationRequestInfo();
				revinfo.Comment = Comment.Value;
				revinfo.Signer = GetSigner();

				GeneratedFile revocationXml = Payload.Api.GenerateRevocationRequestXml(
					Payload.Token,
					Payload.BoxId,
					Payload.Message.MessageId,
					Payload.Entity.EntityId,
					revinfo);

				SignedContent revocSignContent = new SignedContent();
				revocSignContent.Content = revocationXml.Content;

				if(!TrySign(revocSignContent))
					throw new Exception();

				RevocationRequestAttachment revattch = new RevocationRequestAttachment();
				revattch.ParentEntityId = Payload.Entity.EntityId;
				revattch.SignedContent = revocSignContent;

				patch.AddRevocationRequestAttachment(revattch);
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				Payload.Api.PostMessagePatch(Payload.Token, patch);
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