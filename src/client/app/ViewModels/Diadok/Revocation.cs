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

		public async Task Save()
		{
			var patch = Payload.Patch();

			Signer sg = new Signer();

			sg.SignerCertificate = new byte[] {1,2,3};
			sg.SignerCertificateThumbprint = "asfkjaksfjaskfjaf";
			sg.SignerDetails = new SignerDetails();
			sg.SignerDetails.FirstName = "Misha";
			sg.SignerDetails.Inn = "9697899845";
			sg.SignerDetails.JobTitle = "Specialist";
			sg.SignerDetails.Patronymic = "Gennadevich";
			sg.SignerDetails.SoleProprietorRegistrationCertificate = "a ksbfajhbsf jahf as";
			sg.SignerDetails.Surname = "Shunko";

			RevocationRequestInfo revinfo = new RevocationRequestInfo();
			revinfo.Comment = Comment.Value;
			revinfo.Signer = sg;

			int skip = 0;
			var entt = Payload.Message.Entities.Skip(skip).First();

			GeneratedFile revocationXml = Payload.Api.GenerateRevocationRequestXml(
				Payload.Token,
				Payload.BoxId,
				Payload.Message.MessageId,
				entt.EntityId,
				revinfo
				);
			RevocationRequestAttachment revattch = new RevocationRequestAttachment();
			revattch.ParentEntityId = entt.EntityId;
			revattch.SignedContent = new SignedContent();

			if(revocationXml.Content.Length > (500 * 1024 * 1000)) {
				var fileid = Payload.Api.UploadFileToShelf(Payload.Token, revocationXml.Content);
				revattch.SignedContent.NameOnShelf = fileid;
				byte[] sign;
				TrySign(revocationXml.Content, out sign);
				var filesignid = Payload.Api.UploadFileToShelf(Payload.Token, sign);
				revattch.SignedContent.SignatureNameOnShelf = filesignid;
			}
			else {
				revattch.SignedContent.Content = revocationXml.Content;
				revattch.SignedContent.Signature = new byte[] { 1,2,3};
			}

			revattch.SignedContent.SignWithTestSignature = true;

			patch.AddRevocationRequestAttachment(revattch);

			await Async(x => Payload.Api.PostMessagePatch(x, patch));
		}
	}
}