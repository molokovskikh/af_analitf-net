using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Http;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Proto.Invoicing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Globalization;
using NHibernate.Linq;
using System.Threading;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public static class ddk
	{
	
		// I
		public static string ie_login = "f816686@mvrht.com";
		public static string ie_passwd = "A123456";
		public static string ie_boxid = "92c4c6b0948d4252b2b81c2b5730b5d1@diadoc.ru";
		public static string ie_inn = "9698754923";

		public static string ch_login = "pdh23916@zasod.com";
		public static string ch_passwd = "A123456";
		public static string ch_boxid = "ebc25f997551449282541b8a6d1605c9@diadoc.ru";
		public static string ch_inn = "9656351023";
		
			/*
		// II
		public static string ie_login = "c963832@mvrht.com";
		public static string ie_passwd = "222852";
		public static string ie_boxid = "35e8de1b915c4f5eb9df37c98af2b0af@diadoc.ru";
		public static string ie_inn = "9656279962";

		public static string ch_login = "c963977@mvrht.com";
		public static string ch_passwd = "222852";
		public static string ch_boxid = "b38475cbd7ed4f0b892d9f0fd6a8bb30@diadoc.ru";
		public static string ch_inn = "9667029241";
		*/
	}

	public class ActionPayload
	{
		public DiadocApi Api;
		public string BoxId;
		public string Token;

		public MessagePatchToPost Patch()
		{
			return new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId
			};
		}

		public MessagePatchToPost PatchTorg12(ReceiptAttachment receipattachment)
		{
			return new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId,
				XmlTorg12BuyerTitles = { receipattachment }
			};
		}

		public MessagePatchToPost Patch(params ReceiptAttachment[] receipattachments)
		{
			var msg = new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId
			};
			msg.Receipts.AddRange(receipattachments);
			return msg;
		}

		public Entity Entity { get; set; }
		public Message Message { get; set; }
		public Document Document { get; set; }
	}

	public abstract class DiadokAction : BaseScreen
	{
		public bool Success;

		public DiadokAction(ActionPayload payload)
		{
			Success = false;
			InitFields();
			Payload = payload;
			
			switch(payload.Entity.AttachmentType)
			{ 
				case AttachmentType.XmlTorg12:
					DocumentName = new DiadocXMLHelper(payload.Entity).GetDiadokTORG12Name(" , ");
					break;
				case AttachmentType.Invoice:
					DocumentName = new DiadocXMLHelper(payload.Entity).GetDiadokInvoiceName(" , ");
					break;
				default:
					DocumentName = payload.Entity.FileName;
					break;
			}

			IsEnabled.Value = true;
			LastPatchStamp = DateTime.MinValue;
			if(Settings.Value.DebugUseTestSign)
			{
				SignerFirstName = "Иван";
				SignerSureName = "Иванович";
				SignerPatronimic = "Иванов";
				SignerINN = ddk.ie_inn;
			}
			else {
				Cert = Settings.Value.GetCert(Settings.Value.DiadokCert);
				var certFields = X509Helper.ParseSubject(Cert.Subject);
				try
				{
					var namefp = certFields["G"].Split(' ');
					SignerFirstName = namefp[0];
					SignerSureName = certFields["SN"];
					SignerPatronimic = namefp[1];
					if(!string.IsNullOrEmpty(Settings.Value.DebugDiadokSignerINN))
						SignerINN = Settings.Value.DebugDiadokSignerINN;
					else
					{
						if(certFields.Keys.Contains("OID.1.2.643.3.131.1.1"))
							SignerINN = certFields["OID.1.2.643.3.131.1.1"];
						if(certFields.Keys.Contains("ИНН"))
							SignerINN = certFields["ИНН"];
				#if DEBUG
						if(SignerINN.Length > 10)
							SignerINN = SignerINN.Substring(2);
				#endif
						if(string.IsNullOrEmpty(SignerINN))
							throw new Exception("Не найдено поле ИНН(OID.1.2.643.3.131.1.1)");
					}
				}
				catch(Exception exept)
				{
					Manager.Error("Ошибка сертификата.");
					Log.Error("Ошибка разбора сертификата, G,SN,OID.1.2.643.3.131.1.1", exept);
					throw;
				}
			}
		}

		public string SignerFirstName { get; set;}
		public string SignerSureName { get; set;}
		public string SignerPatronimic { get; set;}
		public string SignerINN { get;set;}

		public X509Certificate2 Cert { get; protected set;}
		public NotifyValue<bool> IsEnabled { get; set; }
		public ActionPayload Payload { get; set; }
		public string DocumentName { get; set; }
		public DateTime LastPatchStamp { get; set;}

		public bool ReqRevocationSign
		{
			get
			{
				return Payload.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation;
			}
		}

		public Signer GetSigner()
		{
			Signer ret = new Signer();
			ret.SignerDetails = new SignerDetails();
			if(Settings.Value.DebugUseTestSign)
			{
				ret.SignerCertificate = new byte[1];
				ret.SignerCertificateThumbprint = "0987654321ABCDE";
				ret.SignerDetails.FirstName = "Иван";
				ret.SignerDetails.Surname = "Иванович";
				ret.SignerDetails.Patronymic = "Иванов";
				ret.SignerDetails.JobTitle = "Должность";
				ret.SignerDetails.Inn = ddk.ie_inn;
			}
			else
			{
				ret.SignerCertificate = Cert.RawData;
				ret.SignerCertificateThumbprint = Cert.Thumbprint;
				ret.SignerDetails.FirstName = SignerFirstName;
				ret.SignerDetails.Surname = SignerSureName;
				ret.SignerDetails.Patronymic = SignerPatronimic;
				ret.SignerDetails.JobTitle = Settings.Value.DiadokSignerJobTitle;
				ret.SignerDetails.Inn = SignerINN;
			}

			return ret;
		}

		protected void BeginAction()
		{
			IsEnabled.Value = false;
		}

		protected void EndAction(bool waitupdate = true)
		{
			if(waitupdate)
			{
				Message msg = null;
				int breaker = 0;
				do
				{
					msg = Payload.Api.GetMessage(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId, Payload.Entity.EntityId);
					if(LastPatchStamp != msg.LastPatchTimestamp)
						break;
					else
						Thread.Sleep(1000);
					breaker++;
				} while(breaker<5 && LastPatchStamp != DateTime.MinValue);
				Result = msg;
			}
			IsEnabled.Value = true;
			Success = true;
		}

		public Message Result { get; set;}

		public bool TrySign(SignedContent content)
		{
			if (Settings.Value.DebugUseTestSign) {
				content.SignWithTestSignature = true;
				return true;
			}
			byte[] sign;
			var result = TrySign(content.Content, out sign);
			if(result)
				content.Signature = sign;

			return result;
		}

		public bool TrySign(byte[] content, out byte[] sign)
		{
			sign = null;
			if (Settings.Value.DebugUseTestSign)
				return true;

			try {
				sign = new WinApiCrypt().Sign(content, Cert.RawData);
			}
			catch (Win32Exception e) {
				Log.Error($"Ошибка при подписании документа {Payload.Entity.EntityId}", e);
				Manager.Error(e.Message);
				return false;
			}
			catch (Exception e) {
				Log.Error($"Ошибка при подписании документа {Payload.Entity.EntityId}", e);
				Manager.Error(ErrorHelper.TranslateException(e) ?? "Не удалось подписать документ");
				return false;
			}
			return true;
		}
	}

	public class FormValidation: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			// Редактор XAML почему то передает в value массив, а CLR передает модель
			try
			{
				BindingGroup bindingGroup = (BindingGroup)value;
				Sign model = bindingGroup.Items[0] as Sign;

				string error = "Заполнены не все обязательные поля:";
				string fields = "";

				if(bindingGroup.Name == "AcceptedValidation")
				{
					if(model.ByAttorney || !string.IsNullOrEmpty(model.ACPTFirstName) ||
					!string.IsNullOrEmpty(model.ACPTSurename) ||
					!string.IsNullOrEmpty(model.ACPTPatronimic) ||
					!string.IsNullOrEmpty(model.ACPTJobTitle)
					)
					{
						if(string.IsNullOrEmpty(model.ACPTSurename))
							fields += "\nФамилия";
						if(string.IsNullOrEmpty(model.ACPTFirstName))
							fields += "\nИмя";
					}
				}
				if(bindingGroup.Name == "AttorneyValidation")
				{
					if (!string.IsNullOrEmpty(model.ATRNum) ||
							model.ATRDate != DateTime.MinValue ||
							!string.IsNullOrEmpty(model.ATROrganization) ||
							!string.IsNullOrEmpty(model.ATRFirstName) ||
							!string.IsNullOrEmpty(model.ATRSurename) ||
							!string.IsNullOrEmpty(model.ATRPatronymic) ||
							!string.IsNullOrEmpty(model.ATRAddInfo))
					{
						if(string.IsNullOrEmpty(model.ATRNum))
							fields += "\nНомер";
						if(model.ATRDate == DateTime.MinValue)
							fields += "\nДата";

						if(!string.IsNullOrEmpty(model.ATRFirstName) ||
							!string.IsNullOrEmpty(model.ATRSurename) ||
							!string.IsNullOrEmpty(model.ATRPatronymic))
						{
							if(string.IsNullOrEmpty(model.ATRFirstName))
								fields += "\nИмя";
							if(string.IsNullOrEmpty(model.ATRSurename))
								fields += "\nФамилия";
							if(string.IsNullOrEmpty(model.ATRPatronymic))
								fields += "\nОтчество";
						}
					}
					else if(model.ByAttorney)
						fields += "\nНомер\nДата\nИмя\nФамилия\nОтчество";
				}

				if(string.IsNullOrEmpty(fields))
					return ValidationResult.ValidResult;

				return new ValidationResult(false, error + fields);
			}
			catch (Exception)
			{
				return new ValidationResult(false, "Заполнены не все обязательные поля.");
			}
		}
	}

	public class Sign : DiadokAction
	{
		public Sign(ActionPayload payload)
			: base(payload)
		{
			Torg12TitleVisible = Payload.Entity.AttachmentType == AttachmentType.XmlTorg12 && !ReqRevocationSign;

			RCVFIO.Value = $"{SignerSureName} {SignerFirstName} {SignerPatronimic}";
			RCVJobTitle.Value = Settings.Value.DiadokSignerJobTitle;
			RCVDate.Value = DateTime.Now;
			ATRDate.Value = DateTime.Now;

			LikeReciever.Subscribe(x => {
				if(x)
				{
					ACPTFirstName.Value = SignerFirstName;
					ACPTSurename.Value = SignerSureName;
					ACPTPatronimic.Value = SignerPatronimic;
					ACPTJobTitle.Value = RCVJobTitle.Value;
				}
			});

			ByAttorney.Subscribe(x => {
				if(!x)
				{
					ATRNum.Value = "";
					ATROrganization.Value = "";
					ATRSurename.Value = "";
					ATRFirstName.Value = "";
					ATRPatronymic.Value = "";
					ATRAddInfo.Value = "";
				}
			});

			CurrentAutoSave = new NotifyValue<SignTorg12Autosave>();

			CurrentAutoSave.Subscribe(x =>
			{
				if(x != null)
				{
					RCVJobTitle.Value = x.SignerJobTitle;

					if(x.LikeReciever)
					{
						LikeReciever.Value = true;
					}
					else
					{
						ACPTFirstName.Value = x.ACPTFirstName;
						ACPTSurename.Value = x.ACPTSurename;
						ACPTPatronimic.Value = x.ACPTPatronimic;
						ACPTJobTitle.Value = x.ACPTJobTitle;
					}

					if(x.ByAttorney)
					{
						ByAttorney.Value = true;
						ATRNum.Value = x.ATRNum;
						ATRDate.Value = x.ATRDate;
						ATROrganization.Value = x.ATROrganization;
						ATRSurename.Value = x.ATRSurename;
						ATRFirstName.Value = x.ATRFirstName;
						ATRPatronymic.Value = x.ATRPatronymic;
						ATRAddInfo.Value = x.ATRAddInfo;
					}
					else
					{
						ByAttorney.Value = false;
						ATRNum.Value = "";
						ATRDate.Value = DateTime.MinValue;
						ATROrganization.Value = "";
						ATRSurename.Value ="";
						ATRFirstName.Value = "";
						ATRPatronymic.Value = "";
						ATRAddInfo.Value = "";
					}

					Comment.Value = x.Comment;
				}
			});

			Comment.Subscribe(x => {
				if(!string.IsNullOrEmpty(x))
					CommentVisibility.Value = true;
			});
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			SavedData.Value = Session.Query<SignTorg12Autosave>().OrderByDescending(o => o.CreationDate).ToList();
		}

		public bool Torg12TitleVisible { get;set;}

		public NotifyValue<string> RCVFIO { get; set;}
		public NotifyValue<string> RCVJobTitle { get; set;}
		public NotifyValue<DateTime> RCVDate { get; set;}

		public NotifyValue<SignTorg12Autosave> CurrentAutoSave { get;set;}
		public NotifyValue<List<SignTorg12Autosave>> SavedData { get; set;}
		public NotifyValue<bool> LikeReciever { get; set;}
		public NotifyValue<string> ACPTSurename { get; set;}
		public NotifyValue<string> ACPTFirstName { get; set;}
		public NotifyValue<string> ACPTPatronimic { get; set;}
		public NotifyValue<string> ACPTJobTitle { get; set;}

		public NotifyValue<bool> ByAttorney { get; set;}
		public NotifyValue<string> ATRNum { get; set;}
		public NotifyValue<DateTime> ATRDate { get; set;}
		public NotifyValue<string> ATROrganization { get; set;}
		public NotifyValue<string> ATRSurename { get; set;}
		public NotifyValue<string> ATRFirstName { get; set;}
		public NotifyValue<string> ATRPatronymic { get; set;}
		public NotifyValue<string> ATRAddInfo { get; set;}

		public NotifyValue<bool> SaveData { get; set;}
		const int autosave_max = 10;
		public NotifyValue<bool> CommentVisibility { get; set;}
		public NotifyValue<string> Comment { get; set;}

		Official GetRevicedOfficial()
		{// тоже что и SignerDetails
			Official ret = new Official();
			ret.FirstName = SignerFirstName;
			ret.Surname = SignerSureName;
			ret.Patronymic = SignerPatronimic;
			ret.JobTitle = RCVJobTitle;
			return ret;
		}

		Official GetAcceptetOfficial()
		{
			Official ret = null;
			if(!string.IsNullOrEmpty(ACPTFirstName) && !string.IsNullOrEmpty(ACPTSurename))
			{
				ret = new Official();
				ret.FirstName = ACPTFirstName;
				ret.Surname = ACPTSurename;
				ret.Patronymic = ACPTPatronimic;
				ret.JobTitle = ACPTJobTitle;
				return ret;
			}
			return ret;
		}

		Attorney GetAttorney()
		{
			if(!string.IsNullOrEmpty(ATRNum) && ATRDate.Value != DateTime.MinValue)
			{
				Attorney ret = new Attorney();
				ret.Number = ATRNum;
				ret.Date = ATRDate.Value.ToString("dd.MM.yyyy");
				ret.IssuerOrganizationName = ATROrganization;
				if(!string.IsNullOrEmpty(ATRFirstName.Value) &&
					!string.IsNullOrEmpty(ATRSurename.Value) &&
					!string.IsNullOrEmpty(ATRPatronymic.Value))
				{
					ret.IssuerPerson = new Official();
					ret.IssuerPerson.FirstName = ATRFirstName;
					ret.IssuerPerson.Surname = ATRSurename;
					ret.IssuerPerson.Patronymic = ATRPatronymic;
					ret.IssuerPerson.JobTitle = ATRAddInfo;
				}
				return ret;
			}
			return null;
		}

		Entity GetDateConfirmationStep7(Message msg)
		{
			Entity invoice = msg.Entities.FirstOrDefault(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);
			Entity invoiceReciept = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoice?.EntityId && i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceReceipt);
			Entity invoiceRecieptConfirmation = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoiceReciept?.EntityId && i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
			return invoiceRecieptConfirmation;
		}

		public void Save()
		{
			BeginAction();

			MessagePatchToPost patch = null;
			Signer signer = GetSigner();
			try
			{
				if(ReqRevocationSign)
				{
					Entity revocReq = Payload.Message.Entities.FirstOrDefault(x => x.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.RevocationRequest);
					patch = Payload.Patch();
					DocumentSignature acceptSignature = new DocumentSignature();
					acceptSignature.IsApprovementSignature = true;
					acceptSignature.ParentEntityId = revocReq.EntityId;
					byte[] sign = null;
					if(!TrySign(revocReq.Content.Data, out sign))
						throw new Exception();
					if (Settings.Value.DebugUseTestSign) {
						acceptSignature.SignWithTestSignature = true;
					}
					acceptSignature.Signature = sign;
					patch.AddSignature(acceptSignature);
					LastPatchStamp = Payload.Message.LastPatchTimestamp;
					Payload.Api.PostMessagePatch(Payload.Token, patch);
				}
				else
				{
					if (Payload.Entity.AttachmentType == AttachmentType.XmlTorg12)
					{
						if(SaveData.Value)
						{
							SignTorg12Autosave autosave = new SignTorg12Autosave();
							autosave.SignerJobTitle = RCVJobTitle.Value;
							if(LikeReciever.Value)
							{
								 autosave.LikeReciever = true;
							}
							else
							{
								autosave.ACPTFirstName = ACPTFirstName.Value;
								autosave.ACPTSurename = ACPTSurename.Value;
								autosave.ACPTPatronimic = ACPTPatronimic.Value;
								autosave.ACPTJobTitle = ACPTJobTitle.Value;
							}
							if(ByAttorney.Value)
							{
								autosave.ByAttorney = true;
								autosave.ATRNum = ATRNum.Value;
								autosave.ATRDate = ATRDate.Value;
								autosave.ATROrganization = ATROrganization.Value;
								autosave.ATRSurename = ATRSurename.Value;
								autosave.ATRFirstName = ATRFirstName.Value;
								autosave.ATRPatronymic = ATRPatronymic.Value;
								autosave.ATRAddInfo = ATRAddInfo.Value;
							}
							autosave.Comment = Comment.Value;
							Session.Save(autosave);
							for(int i = autosave_max - 1; i < SavedData.Value.Count; i++)
							{
								Session.Delete(SavedData.Value[i]);
							}
						}
						Official recived = GetRevicedOfficial();
						Official accepted = GetAcceptetOfficial();
						Attorney attorney = GetAttorney();

						var inf =  new Torg12BuyerTitleInfo() {
							ReceivedBy = recived, //лицо, получившее груз signer
							AcceptedBy = accepted,//лицо, принявшее груз
							Attorney = attorney,
							AdditionalInfo = Comment,
							ShipmentReceiptDate = RCVDate.Value.ToString("dd.MM.yyyy"),
							Signer = signer
							};

						GeneratedFile torg12XmlForBuyer = Payload.Api.GenerateTorg12XmlForBuyer(
							Payload.Token,
							inf,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							Payload.Entity.DocumentInfo.EntityId);

						SignedContent signContent = new SignedContent();
						signContent.Content = torg12XmlForBuyer.Content;
						if(TrySign(signContent) == false)
							throw new Exception();

						ReceiptAttachment receipt = new ReceiptAttachment {
							ParentEntityId = Payload.Entity.DocumentInfo.EntityId,
							SignedContent = signContent
						};
						patch = Payload.PatchTorg12(receipt);
						LastPatchStamp = Payload.Message.LastPatchTimestamp;
						Payload.Api.PostMessagePatch(Payload.Token, patch);
						Log.Info($"Документ {patch.MessageId} успешно подписан");
					}
					else
					{
						Entity invoice = Payload.Message.Entities.First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);

						GeneratedFile invoiceReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoice.EntityId,
							signer);

						SignedContent signContentInvoiceReciept = new SignedContent();
						signContentInvoiceReciept.Content = invoiceReceipt.Content;
						if(TrySign(signContentInvoiceReciept) == false)
							throw new Exception();

						ReceiptAttachment receiptInvoice = new ReceiptAttachment {
							ParentEntityId = invoice.EntityId,
							SignedContent = signContentInvoiceReciept
						};

						Entity invoiceConfirmation = Payload.Message.Entities.OrderBy(x => x.CreationTime).First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
						GeneratedFile invoiceConfirmationReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoiceConfirmation.EntityId,
							signer);

						SignedContent signContentInvoiceConfirmationReciept = new SignedContent();
						signContentInvoiceConfirmationReciept.Content = invoiceReceipt.Content;

						if (TrySign(signContentInvoiceConfirmationReciept) == false)
							throw new Exception();

						ReceiptAttachment invoiceConfirmationreceipt = new ReceiptAttachment {
							ParentEntityId = invoiceConfirmation.EntityId,
							SignedContent = signContentInvoiceConfirmationReciept
						};

						patch = Payload.Patch(receiptInvoice, invoiceConfirmationreceipt);

						Payload.Api.PostMessagePatch(Payload.Token, patch);
						Log.Info($"Документ {patch.MessageId} receiptInvoice, invoiceConfirmationreceipt отправлены");

						Entity invoiceDateConfirmation = null;
						{
							Message msg = null;
							Entity dateConfirm = null;
							int breaker = 0;
							do
							{
								Thread.Sleep(1000);
								msg = Payload.Api.GetMessage(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId, Payload.Entity.EntityId);
								dateConfirm = GetDateConfirmationStep7(msg);
								breaker++;
							}
							while (dateConfirm ==null && breaker < 10);
							if(dateConfirm == null)
								throw new TimeoutException("Превышено время ожидания ответа, повторите операцию позже.");
							LastPatchStamp = msg.LastPatchTimestamp;
							invoiceDateConfirmation = dateConfirm;
						}

						GeneratedFile invoiceOperConfirmationReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoiceDateConfirmation.EntityId,
							signer);

						SignedContent signContentOperInvoiceConfrmReciept = new SignedContent();
						signContentOperInvoiceConfrmReciept.Content = invoiceOperConfirmationReceipt.Content;

						if (TrySign(signContentOperInvoiceConfrmReciept) == false)
							throw new Exception();

						ReceiptAttachment receipt = new ReceiptAttachment {
							ParentEntityId = invoiceDateConfirmation.EntityId,
							SignedContent = signContentOperInvoiceConfrmReciept
						};
						patch = Payload.Patch(receipt);
						Payload.Api.PostMessagePatch(Payload.Token, patch);
						Log.Info($"Документ {patch.MessageId} invoiceDateConfirmation отправлен");
					}
				}
				EndAction();
			}
			catch(Exception exception)
			{
				EndAction(false);
				if(exception is TimeoutException)
				{
					Manager.Warning("Превышено время ожидания ответа, повторите операцию позже.");
				}
				else if (exception is HttpClientException)
				{
					var e = exception as HttpClientException;
					if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
						Log.Warn($"Документ {patch.MessageId} был подписан ранее", e);
						Manager.Warning("Документ уже был подписан другим пользователем.");
					}
				}
				else
					throw;
			}
			TryClose();
		}
	}
}