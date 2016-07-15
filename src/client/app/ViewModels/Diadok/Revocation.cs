using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Proto.Invoicing;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Revocation : DiadokAction
	{
		public Revocation(ActionPayload payload)
			: base(payload)
		{
		}

		public NotifyValue<string> Comment { get; set; }

		Signer GetSigner()
		{
			var certFields = Cert.Subject.Split(',').Select(s => s.Split('=')).ToDictionary(p => p[0].Trim(), p => p[1].Trim());

			Signer ret = new Signer();
			ret.SignerDetails = new SignerDetails();
			ret.SignerCertificate = Cert.RawData;
			ret.SignerCertificateThumbprint = Cert.Thumbprint;
			ret.SignerDetails.FirstName = certFields["CN"];
			ret.SignerDetails.Surname = certFields["CN"];
			ret.SignerDetails.Patronymic = certFields["CN"];
			ret.SignerDetails.JobTitle = certFields["CN"];
			ret.SignerDetails.Inn = "9656279962";//certFields["CN"];
			return ret;
		}

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

				await Async(x => Payload.Api.PostMessagePatch(x, patch));
			}
			catch(Exception)
			{
			}
			finally
			{
				EndAction();
			}

		}
	}
}