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

		public async Task Save()
		{
			try
			{
				BeginAction();
				var patch = Payload.Patch();

				RevocationRequestInfo revinfo = new RevocationRequestInfo();
				revinfo.Comment = Comment.Value;
				revinfo.Signer = GetSigner();

				var document = Payload.Message.Entities.First();

				GeneratedFile revocationXml = Payload.Api.GenerateRevocationRequestXml(
					Payload.Token,
					Payload.BoxId,
					Payload.Message.MessageId,
					document.EntityId,
					revinfo);

				SignedContent revocSignContent = new SignedContent();
				revocSignContent.Content = revocationXml.Content;

				if(!TrySign(revocSignContent))
					throw new Exception();

				RevocationRequestAttachment revattch = new RevocationRequestAttachment();
				revattch.ParentEntityId = document.EntityId;
				revattch.SignedContent = revocSignContent;

				patch.AddRevocationRequestAttachment(revattch);
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				await Async(x => Payload.Api.PostMessagePatch(x, patch));
			}
			catch(HttpClientException e)
			{
				Log.Warn($"Ошибка:", e);
				Manager.Error(e.AdditionalMessage);
			}
			finally
			{
				await EndAction();
			}
		}
	}
}