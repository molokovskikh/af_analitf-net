using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Documents.NonformalizedDocument;
using Diadoc.Api.Proto.Events;
using Newtonsoft.Json;
using Message = Diadoc.Api.Proto.Events.Message;
using NonformalizedDocumentStatus = Diadoc.Api.Com.NonformalizedDocumentStatus;

namespace AnalitF.Net.Client.Models
{
	//хранится в той же таблице что и ExtDocAttachment
	public class Sign
	{
		public virtual uint Id { get; set; }
		public virtual byte[] SignBytes { get; set; }
	}

	/// <summary>
	/// эта модель повторяет структуру диадока что бы было проще выполнять синхронизацию
	/// в модели может быть как документ непосредственно так и псевдо документу который описывает
	/// событие, например отправку документа на согласование
	/// </summary>
	public class ExtDocAttachment
	{
		public static Dictionary<object, string> Descriptions = new Dictionary<object, string> {
			{ ResolutionStatusType.SignatureDenied, "В подписании отказано" },
			{ ResolutionStatusType.SignatureRequested, "На подписании" },
			{ ResolutionStatusType.ApprovementRequested, "На согласовании" },
			{ ResolutionStatusType.Approved, "Согласовано" },
			{ ResolutionStatusType.Disapproved, "В согласовании отказано" },
			{ NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected, "Отказано" },
			{ NonformalizedDocumentStatus.InboundWaitingForRecipientSignature, "Требуется подпись" },
			{ NonformalizedDocumentStatus.InboundWithRecipientSignature, "Подписан" },
		};

		public enum Status
		{
			None,
			Signed,
			SignedAndSent,
		}

		public ExtDocAttachment(ExtDoc doc, string localfilename, Entity entity)
		{
#if DEBUG
			File.WriteAllText(FileHelper.MakeRooted(localfilename) + ".json",
				JsonConvert.SerializeObject(entity, Formatting.Indented));
#endif
			DiadokEntityId = entity.EntityId;
			ParentEntityId = entity.ParentEntityId;
			ExtDoc = doc;
			CreatedOn = entity.CreationTime;

			if (entity.AttachmentType == AttachmentType.ResolutionRequest) {
				var author = entity.ResolutionRequestInfo.Author;
				var detartment = entity.ResolutionRequestInfo.Target.Department;
				var user = entity.ResolutionRequestInfo.Target.User;
				if (entity.ResolutionRequestInfo.RequestType == ResolutionRequestType.SignatureRequest) {
					Comment = $"{author} передал документ на подпись в подразделение: {detartment} {user}";
				}
				else if (entity.ResolutionRequestInfo.RequestType == ResolutionRequestType.ApprovementRequest) {
					Comment = $"{author} передал документ на согласование в подразделение: {detartment} {user}";
				}
				if (entity.Content?.Size > 0) {
					Comment += "\r\n" + Encoding.UTF8.GetString(entity.Content.Data);
				}
				return;
			}
			if (entity.AttachmentType == AttachmentType.Resolution) {
				var author = entity.ResolutionInfo.Author;
				if (entity.ResolutionInfo.ResolutionType == ResolutionType.Disapprove)
					Comment = $"{author} отказал в запросе подписи сотруднику";

				if (entity.Content?.Size > 0)
					Comment += "\r\n" + Encoding.UTF8.GetString(entity.Content.Data);
				return;
			}
			if (entity.AttachmentType == AttachmentType.ResolutionRequestDenial) {
				var author = entity.ResolutionRequestDenialInfo.Author;
				Comment = $"{author} отказал в запросе подписи сотруднику";
				if (entity.Content?.Size > 0) {
					Comment += "\r\n" + Encoding.UTF8.GetString(entity.Content.Data);
				}
				return;
			}
			if (entity.AttachmentType == AttachmentType.AttachmentComment) {
				Comment = Encoding.UTF8.GetString(entity.Content.Data);
				return;
			}
			Filename = String.IsNullOrEmpty(entity.FileName)
				? $"Без имени {entity.EntityType} {entity.AttachmentType}" : entity.FileName;
			LocalFilename = localfilename;

			Update(entity.DocumentInfo);
		}

		public ExtDocAttachment()
		{
		}

		public virtual uint Id { get; set; }
		public virtual DateTime CreatedOn { get; set; }
		public virtual string Filename { get; set; }
		public virtual string LocalFilename { get; set; }
		public virtual ExtDoc ExtDoc { get; set; }
		public virtual string DiadokEntityId { get; set; }
		public virtual string ParentEntityId { get; set; }
		public virtual Status AttachmentStatus { get; set; }
		public virtual ResolutionStatusType? ResolutionStatus { get; set; }
		public virtual NonformalizedDocumentStatus? DocumentStatus { get; set; }
		public virtual RevocationStatus? RevocationStatus { get; set; }
		public virtual long Timestamp { get; set; }
		public virtual string Comment { get; set; }

		public virtual string Name
		{
			get
			{
				var result = Filename;
				var desc = new List<string> {
					Descriptions.GetValueOrDefault(ResolutionStatus),
					Descriptions.GetValueOrDefault(DocumentStatus),
				};
				var resultDesc = desc.Where(x => !String.IsNullOrEmpty(x)).Implode();
				if (!String.IsNullOrEmpty(resultDesc))
					result += " - " + resultDesc;
				return result;
			}
		}

		public virtual string FullFilename => FileHelper.MakeRooted(LocalFilename);

		public virtual ImageSource FileTypeIcon
		{
			get
			{
				if (String.IsNullOrEmpty(FullFilename))
					return null;

				try {
					var icon = Icon.ExtractAssociatedIcon(FullFilename);
					return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
						new Int32Rect(0, 0, icon.Width, icon.Height),
						BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
				}
				catch(Exception) {
					return null;
				}
			}
		}

		public virtual void Update(Document doc)
		{
			ResolutionStatus = doc?.ResolutionStatus?.Type;
			DocumentStatus = doc?.NonformalizedDocumentMetadata?.Status;
			RevocationStatus = doc?.RevocationStatus;
			Timestamp = (doc?.LastModificationTimestampTicks).GetValueOrDefault();
		}

		public virtual bool CanSign()
		{
			if (AttachmentStatus != Status.None)
				return false;
			if (ResolutionStatus != null && (ResolutionStatus.Value == ResolutionStatusType.ApprovementRequested
				&& ResolutionStatus.Value == ResolutionStatusType.SignatureRequested))
				return false;
			if (DocumentStatus != null && (DocumentStatus.Value == NonformalizedDocumentStatus.InboundWithRecipientSignature
				|| DocumentStatus.Value == NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected))
				return false;
			return true;
		}
	}

	/// <summary>
	/// модель повторяет структуру диадок, придставляет собой структуру которая объединяет группу документов
	/// и событий к ним
	/// </summary>
	public class ExtDoc
	{
		public ExtDoc()
		{
		}

		public ExtDoc(Message message)
		{
			Sender = message.FromTitle;
			SentAt = DateTime.Now;
			Subject = $"Документ ${message.MessageId}";
			DiadokMessageId = message.MessageId;
			DiadokBoxId = message.ToBoxId;
			Attachments = new List<ExtDocAttachment>();
		}

		public virtual uint Id { get; set; }

		public virtual string Subject { get; set; }
		public virtual string Sender { get; set; }
		public virtual DateTime SentAt { get; set; }

		public virtual string DiadokMessageId { get ;set; }
		public virtual string DiadokBoxId { get; set; }

		public virtual IList<ExtDocAttachment> Attachments { get; set; }
	}
}