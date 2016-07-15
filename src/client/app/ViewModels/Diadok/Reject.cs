using System;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api.Proto.Events;
using System.Linq;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Reject : DiadokAction
	{
		public Reject(ActionPayload payload)
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
				var operation = Payload.Entity.DocumentInfo.RevocationStatus;
				if(operation == Diadoc.Api.Proto.Documents.RevocationStatus.RequestsMyRevocation)
				{
					Entity revocreq = Payload.Message.Entities.FirstOrDefault(x => x.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.RevocationRequest);
					XmlSignatureRejectionAttachment signrejattch = new XmlSignatureRejectionAttachment();
					signrejattch.ParentEntityId = Payload.Entity.EntityId;
					SignedContent signcontent = new SignedContent();
					signcontent.Content = revocreq.Content.Data;
					if (!TrySign(signcontent))
						throw new Exception();
					signrejattch.SignedContent = signcontent;
					patch.AddXmlSignatureRejectionAttachment(signrejattch);
				}
				else
				{
					var content = new SignedContent();
					//комментарий должен быть всегда
					if (!String.IsNullOrEmpty(Comment.Value))
						content.Content = Encoding.UTF8.GetBytes(Comment.Value);
					else
						content.Content = Encoding.UTF8.GetBytes(" ");

					if (!TrySign(content))
						throw new Exception();
					patch.RequestedSignatureRejections.Add(new RequestedSignatureRejection {
						ParentEntityId = Payload.Entity.EntityId,
						SignedContent = content
					});
				}

				await Async(x => Payload.Api.PostMessagePatch(x, patch));
			}
			catch(Exception ex)
			{
			}
			finally
			{
				EndAction();
			}
		}
	}
}