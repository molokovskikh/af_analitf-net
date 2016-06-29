using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Events;
using NHibernate.Linq;
using AttachmentType = Diadoc.Api.Proto.Events.AttachmentType;
using EntityType = Diadoc.Api.Proto.Events.EntityType;
using Message = Diadoc.Api.Proto.Events.Message;
using ResolutionRequestType = Diadoc.Api.Proto.Events.ResolutionRequestType;
using ResolutionStatusType = Diadoc.Api.Proto.Documents.ResolutionStatusType;
using ResolutionType = Diadoc.Api.Proto.Events.ResolutionType;
using Diadoc.Api.Proto.Documents.NonformalizedDocument;
using PdfiumViewer;
using ReactiveUI;
using DialogResult = System.Windows.Forms.DialogResult;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class DisplayItem : INotifyPropertyChanged
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

		private string localFilename;
		public Entity Entity;
		public Message Message;

		public DisplayItem(Message message, Entity entity, Entity prev, OrganizationList orgs)
		{
			Message = message;
			Entity = entity;
			if (prev == null || prev.DocumentInfo.MessageId != entity.DocumentInfo.MessageId)
				Sender = message.FromTitle;
			Department = orgs.Organizations.SelectMany(x => x.Departments)
				.FirstOrDefault(x => x.DepartmentId == entity.DocumentInfo.DepartmentId)?.Name
					?? "Головное подразделение";
			Date = entity.CreationTime.ToLocalTime();
		}

		public string Sender { get; set; }
		public string FileName => Entity.FileName;
		public string Status => GetStatus();
		public bool IsGroup => !String.IsNullOrEmpty(Sender);
		public string Department { get; set; }
		public DateTime Date { get; set; }

		public string LocalFilename
		{
			get { return localFilename; }
			set
			{
				if (localFilename == value)
					return;
				localFilename = value;
				OnPropertyChanged();
				OnPropertyChanged("Icon");
			}
		}

		public ImageSource Icon
		{
			get
			{
				if (String.IsNullOrEmpty(LocalFilename))
					return null;
				try {
					var icon = System.Drawing.Icon.ExtractAssociatedIcon(LocalFilename);
					return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
						new Int32Rect(0, 0, icon.Width, icon.Height),
						BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
				}
				catch(Exception) {
					return null;
				}
			}
		}

		public string GetStatus()
		{
			var desc = new List<string> {
				Descriptions.GetValueOrDefault(Entity?.DocumentInfo?.ResolutionStatus?.Type),
				Descriptions.GetValueOrDefault(Entity?.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus),
			};
			var status = desc.Where(x => !String.IsNullOrEmpty(x)).Implode();
			if (String.IsNullOrEmpty(status))
				status = "Получен";
			return status;
		}

		public virtual bool CanSign()
		{
			if (Entity.DocumentInfo?.ResolutionStatus?.Type == ResolutionStatusType.ApprovementRequested
				&& Entity.DocumentInfo?.ResolutionStatus?.Type == ResolutionStatusType.SignatureRequested)
				return false;
			if (Entity.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus == NonformalizedDocumentStatus.InboundWithRecipientSignature
				|| Entity.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus == NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected)
				return false;
			return true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class Attachment : INotifyPropertyChanged
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

		private string localFilename;
		public Entity Entity;

		public Attachment(Entity entity)
		{
			Entity = entity;
		}

		public string LocalFilename
		{
			get { return localFilename; }
			set
			{
				if (localFilename == value)
					return;
				localFilename = value;
				OnPropertyChanged();
				OnPropertyChanged("Icon");
			}
		}


		public string FileName => Entity.FileName;
		public string Status => GetStatus();

		public ImageSource Icon
		{
			get
			{
				if (String.IsNullOrEmpty(LocalFilename))
					return null;
				try {
					var icon = System.Drawing.Icon.ExtractAssociatedIcon(LocalFilename);
					return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
						new Int32Rect(0, 0, icon.Width, icon.Height),
						BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
				}
				catch(Exception) {
					return null;
				}
			}
		}

		public string GetStatus()
		{
			var desc = new List<string> {
				Descriptions.GetValueOrDefault(Entity?.DocumentInfo?.ResolutionStatus?.Type),
				Descriptions.GetValueOrDefault(Entity?.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus),
			};
			var status = desc.Where(x => !String.IsNullOrEmpty(x)).Implode();
			if (String.IsNullOrEmpty(status))
				status = "Получен";
			return " - " + status;
		}

		public virtual bool CanSign()
		{
			if (Entity.DocumentInfo?.ResolutionStatus?.Type == ResolutionStatusType.ApprovementRequested
				&& Entity.DocumentInfo?.ResolutionStatus?.Type == ResolutionStatusType.SignatureRequested)
				return false;
			if (Entity.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus == NonformalizedDocumentStatus.InboundWithRecipientSignature
				|| Entity.DocumentInfo?.NonformalizedDocumentMetadata?.DocumentStatus == NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected)
				return false;
			return true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class AttachmentHistory
	{
		public string Description { get; set; }
		public DateTime Date { get; set; }
		public string Comment { get; set; }
		public bool HasComment => !String.IsNullOrEmpty(Comment);

		public static List<AttachmentHistory> Collect(DisplayItem item)
		{
			var items = new List<AttachmentHistory>();
			if (item == null)
				return items;
			var message = item.Message;
			var current = item.Entity;
			var entities = message.Entities.Where(x => x.ParentEntityId == current.EntityId).ToArray();
			foreach (var entity in entities) {
				if (entity.EntityType == EntityType.Signature && entity.SignerBoxId == message.FromBoxId) {
					var data = entities.FirstOrDefault(x => x.AttachmentType == AttachmentType.AttachmentComment)?.Content?.Data;
					items.Add(new AttachmentHistory {
						Description = $"{message.FromTitle}, подписал и отправил документ",
						Comment = Encoding.UTF8.GetString(data ?? new byte[0]),
						Date = entity.CreationTime.ToLocalTime(),
					});
				} else if (entity.EntityType == EntityType.Signature && entity.SignerBoxId == message.ToBoxId) {
					items.Add(new AttachmentHistory {
						Description = $"{message.ToTitle}, подписал документ и завершил документооборот",
						Date = entity.CreationTime.ToLocalTime()
					});
				} else if (entity.AttachmentType == AttachmentType.ResolutionRequest) {
					var type = "";
					if (entity.ResolutionRequestInfo.RequestType == ResolutionRequestType.ApprovementRequest) {
						type = "на согласование";
					} else if (entity.ResolutionRequestInfo.RequestType == ResolutionRequestType.SignatureRequest) {
						type = "на подпись";
					}
					var target = "";
					if (!String.IsNullOrEmpty(entity.ResolutionRequestInfo.Target.User)) {
						target = $"сотруднику: {entity.ResolutionRequestInfo.Target.User}";
					} else {
						var dep = entity.ResolutionRequestInfo.Target.Department;
						if (String.IsNullOrEmpty(dep))
							dep = "Головное подразделение";
						target = $"в подразделение: {dep}";
					}
					items.Add(new AttachmentHistory {
						Description = $"{entity.ResolutionRequestInfo.Author} передал документ {type} {target}",
						Comment = Encoding.UTF8.GetString(entity.Content?.Data ?? new byte[0]),
						Date = entity.CreationTime.ToLocalTime()
					});
				} else if (entity.AttachmentType == AttachmentType.SignatureRequestRejection) {
					items.Add(new AttachmentHistory {
						Description = "Отказано в подписании документа",
						Comment = Encoding.UTF8.GetString(entity.Content?.Data ?? new byte[0]),
						Date = entity.CreationTime.ToLocalTime()
					});
				} else if (entity.AttachmentType == AttachmentType.Resolution) {
					if (entity.ResolutionInfo.ResolutionType == ResolutionType.Approve) {
						items.Add(new AttachmentHistory {
						Description = $"{entity.ResolutionInfo.Author} согласовал документ",
						Comment = Encoding.UTF8.GetString(entity.Content?.Data ?? new byte[0]),
						Date = entity.CreationTime.ToLocalTime()
					});
					} else if (entity.ResolutionInfo.ResolutionType == ResolutionType.Disapprove) {
						items.Add(new AttachmentHistory {
							Description = $"{entity.ResolutionInfo.Author} отказал в согласовании документа",
							Comment = Encoding.UTF8.GetString(entity.Content?.Data ?? new byte[0]),
							Date = entity.CreationTime.ToLocalTime()
						});
					}
				} else if (entity.AttachmentType == AttachmentType.ResolutionRequestDenial) {
					items.Add(new AttachmentHistory {
						Description = $"{entity.ResolutionRequestDenialInfo.Author} отказал в запросе подписи сотруднику",
						Comment = Encoding.UTF8.GetString(entity.Content?.Data ?? new byte[0]),
						Date = entity.CreationTime.ToLocalTime()
					});
				} else if (entity.AttachmentType == AttachmentType.XmlSignatureRejection) {
					//todo комментарий содержится внутри xml в Content
					items.Add(new AttachmentHistory {
						Description = "Отказано в подписании документа",
						Date = entity.CreationTime.ToLocalTime()
					});
				}
			}
			return items.OrderBy(x => x.Date).ToList();
		}
	}


	public class Index : BaseScreen
	{
		private ObservableCollection<DisplayItem> items = new ObservableCollection<DisplayItem>();
		private DiadocApi api;
		private string token;
		private Box box;
		private string nextPage;
		private List<string> pageIndexes = new List<string>();
		private static string[] fileformats = {
			".bmp", ".jpeg", ".jpg", ".png", ".tiff", ".gif", ".pdf"
		};

		private OrganizationList orgs;

		public Index()
		{
			DisplayName = "Диадок";
			InitFields();

			CurrentItem.Subscribe(LoadFiles);
			CurrentItem
				.Select(x => {
					if (x == null)
						return Observable.Return<string>(null);
					return Observable.Start(() => {
						var dir = FileHelper.MakeRooted("diadok");
						Directory.CreateDirectory(dir);
						var filename = Directory.GetFiles(dir, $"{x.Entity.EntityId}.*").FirstOrDefault();
						if (!String.IsNullOrEmpty(filename))
							return filename;

						var bytes = x.Entity.Content.Data
							?? api.GetEntityContent(token, box.BoxId, x.Entity.DocumentInfo.MessageId, x.Entity.EntityId);
						filename = Path.Combine(dir, x.Entity.EntityId + Path.GetExtension(x.Entity.FileName));
						File.WriteAllBytes(filename, bytes);
						return filename;
					}).DefaultIfFail();
				})
				.Switch()
				.ObserveOn(UiScheduler)
				.CatchSubscribe(x => {
					if (CurrentItem.Value != null)
						CurrentItem.Value.LocalFilename = x;
					Filename.Value = x;
				});
			Filename.Select(x => {
					if (x == null)
						return Observable.Return<string>(null);

					var attachment = CurrentItem.Value;
					if (fileformats.Contains(Path.GetExtension(x) ?? "", StringComparer.CurrentCultureIgnoreCase))
						return Observable.Return(x);

					return Observable.Start(() => LoadPrintPdf(attachment)).DefaultIfFail();
				})
				.Switch()
				.ObserveOn(UiScheduler)
				.Subscribe(PreviewFilename);

			CurrentItem.Select(x => (x?.CanSign()).GetValueOrDefault())
				.Subscribe(CanSign);
			CurrentItem.Select(x => (x?.CanSign()).GetValueOrDefault())
				.Subscribe(CanReject);
			CurrentItem.Select(x => (x?.CanSign()).GetValueOrDefault())
				.Subscribe(CanRequestSign);
			CurrentItem.Select(x => x != null &&
				!(x.Entity.DocumentInfo.NonformalizedDocumentMetadata?.DocumentStatus == NonformalizedDocumentStatus.InboundRecipientSignatureRequestRejected
					|| x.Entity.DocumentInfo.RevocationStatus != RevocationStatus.RevocationStatusNone))
				.Subscribe(CanRevoke);

			Filename.Select(x => x != null)
				.Subscribe(CanOpen);
			Filename.Select(x => x != null)
				.Subscribe(CanSave);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanDelete);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanDeleteItem);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanRequestResolution);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanApprove);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanDisapprove);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanPrintItem);

			SearchTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(x => String.IsNullOrEmpty(x)
					? items.ToObservableCollection()
					: items.Where(y => y.FileName.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0)
						.ToObservableCollection())
				.Subscribe(Items);
			CurrentItem.Select(x => AttachmentHistory.Collect(x))
				.Subscribe(History);
			IsLoading.Select(x => !x).Subscribe(IsLoaded);
			IsLoading.Select(x => !x).Subscribe(CanPrev);
			IsLoading.Select(x => !x).Subscribe(CanNext);
			IsLoading.Select(x => !x).Subscribe(CanReload);
		}

		private string LoadPrintPdf(DisplayItem item)
		{
			var dir = FileHelper.MakeRooted("diadok");
			var file = Directory.GetFiles(dir, $"print.{item.Entity.EntityId}").FirstOrDefault();
			if (file != null)
				return file;
			while (true) {
				var data = api.GeneratePrintForm(token, box.BoxId,
					item.Entity.DocumentInfo.MessageId,
					item.Entity.EntityId);
				if (data.HasContent) {
					var preview = Path.Combine(dir, $"print.{item.Entity.EntityId}.pdf");
					File.WriteAllBytes(preview, data.Content.Bytes);
					return preview;
				}
				else {
					Thread.Sleep(data.RetryAfter);
				}
			}
		}

		public static bool IsFile(Entity y)
		{
			return y.EntityType == EntityType.Attachment
				&& !String.IsNullOrEmpty(y.FileName)
				&& String.IsNullOrEmpty(y.ParentEntityId);
		}

		private async void LoadFiles(DisplayItem attachment)
		{
			if (attachment == null)
				return;
			var dir = FileHelper.MakeRooted("diadok");
			await TaskEx.Run(() => Directory.CreateDirectory(dir));
			attachment.LocalFilename = await TaskEx.Run(() => Directory.GetFiles(dir, $"{attachment.Entity.EntityId}.*").FirstOrDefault());
		}

		public NotifyValue<string> Total { get; set; }
		public NotifyValue<bool> CanPrintItem { get; set; }
		public NotifyValue<bool> CanPrev { get; set; }
		public NotifyValue<bool> CanNext { get; set; }
		public NotifyValue<bool> CanReload { get; set; }
		public NotifyValue<bool> CanRevoke { get; set; }
		public NotifyValue<bool> CanReject { get; set; }
		public NotifyValue<bool> CanRequestResolution { get; set; }
		public NotifyValue<bool> CanRequestSign { get; set; }
		public NotifyValue<bool> CanApprove { get; set; }
		public NotifyValue<bool> CanDisapprove { get; set; }
		public NotifyValue<string> SearchTerm { get; set; }
		public NotifyValue<bool> CanDeleteItem { get; set; }
		public NotifyValue<bool> CanSave { get; set; }
		public NotifyValue<bool> CanDelete { get; set; }
		public NotifyValue<bool> CanOpen { get; set; }
		public NotifyValue<bool> CanSign { get; set; }

		public NotifyValue<ObservableCollection<DisplayItem>> Items { get; set; }
		public NotifyValue<DisplayItem> CurrentItem { get; set; }
		public NotifyValue<ImageSource> Icon { get; set; }
		public NotifyValue<string> Filename { get; set; }
		public NotifyValue<string> PreviewFilename { get; set; }
		public NotifyValue<List<AttachmentHistory>> History { get; set; }
		public NotifyValue<bool> IsLoaded { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (String.IsNullOrEmpty(Settings.Value.DiadokUsername)
				|| String.IsNullOrEmpty(Settings.Value.DiadokPassword)) {
				Manager.Warning("Для начала работы нужно ввести учетные данные Диадок." +
					" Если Вы не зарегистрированы в системе Диадок нужно пройти регистрацию на сайте http://diadoc.kontur.ru.");
				Shell.ShowSettings("DiadokTab");
				Session.Evict(Settings.Value);
				Settings.Value = Session.Query<Settings>().First();
				if (String.IsNullOrEmpty(Settings.Value.DiadokUsername)
				|| String.IsNullOrEmpty(Settings.Value.DiadokPassword)) {
					return;
				}
			}
			Reload();
		}

		public override void PostActivated()
		{
			base.PostActivated();

			if (String.IsNullOrEmpty(Settings.Value.DiadokUsername)
			|| String.IsNullOrEmpty(Settings.Value.DiadokPassword)) {
				TryClose();
			}
		}

		public void Prev()
		{
			var index = pageIndexes.IndexOf(nextPage);
			if (index == 0)
				return;
			if (index == -1) {
				pageIndexes.Clear();
				Reload();
			} else {
				if (index - 2 < 0)
					Reload();
				else
					Reload(pageIndexes[index - 2]);
			}
		}

		public void Next()
		{
			Reload(nextPage);
		}

		public void Reload(string key = null)
		{
			IsLoading.Value = true;
			Observable.Start(() => {
					if (api == null) {
						api = new DiadocApi(Shell.Config.DiadokApiKey, Shell.Config.DiadokUrl, new WinApiCrypt());
					}
					if (String.IsNullOrEmpty(token)) {
						token = api.Authenticate(Settings.Value.DiadokUsername, Settings.Value.DiadokPassword);
						box = api.GetMyOrganizations(token).Organizations[0].Boxes[0];
					}
					var docs = api.GetDocuments(token, new DocumentsFilter {
						FilterCategory = "Any.Inbound",
						BoxId = box.BoxId,
						SortDirection = "Descending",
						AfterIndexKey = key
					});
					var items = docs.Documents.Select(x => x.MessageId).Distinct()
						.AsParallel()
						.Select(x => api.GetMessage(token, box.BoxId, x))
						.OrderByDescending(x => x.Timestamp)
						.ToObservableCollection();
					return Tuple.Create(items,
						$"Загружено {docs.Documents.Count} из {docs.TotalCount}",
						docs.Documents.LastOrDefault()?.IndexKey,
						api.GetMyOrganizations(token));
				})
				.ObserveOn(UiScheduler)
				.Do(_ => IsLoading.Value = false, _ => IsLoading.Value = false)
				.Subscribe(x => {
					SearchTerm.Value = "";
					Total.Value = x.Item2;
					nextPage = x.Item3;
					if (!pageIndexes.Contains(nextPage))
						pageIndexes.Add(nextPage);
					var display = new List<DisplayItem>();
					Entity prev = null;
					foreach (var message in x.Item1) {
						foreach (var entity in message.Entities.Where(IsFile)) {
							display.Add(new DisplayItem(message, entity, prev, x.Item4));
							prev = entity;
						}
					}
					orgs = x.Item4;
					items = display.ToObservableCollection();
					Items.Value = display.ToObservableCollection();
				}, e => {
					Log.Error("Не удалось загрузить документы диадок", e);
					Manager.Error(ErrorHelper.TranslateException(e)
						?? "Не удалось выполнить операцию, попробуйте повторить позднее.");
				});
		}

		public async Task RequestSign()
		{
			await ProcessAction(new RequestResolution(GetPayload(), ResolutionRequestType.SignatureRequest));
		}

		public async Task RequestResolution()
		{
			await ProcessAction(new RequestResolution(GetPayload(), ResolutionRequestType.ApprovementRequest));
		}

		public async Task Approve()
		{
			await ProcessAction(new Resolution(GetPayload(), ResolutionType.Approve));
		}

		public async Task Disapprove()
		{
			await ProcessAction(new Resolution(GetPayload(), ResolutionType.Disapprove));
		}

		public void Delete()
		{
			var dialog = new Delete(GetPayload());
			Manager.ShowFixedDialog(dialog);
			if (dialog.Success) {
				var current = CurrentItem.Value;
				items.Remove(CurrentItem);
				Items.Value.Remove(current);
			}
		}

		public IEnumerable<IResult> Save()
		{
			var ext = Path.GetExtension(Filename.Value);
			var result = new SaveFileResult(new[] { Tuple.Create(ext, ext) }, CurrentItem.Value.FileName);
			yield return result;
			File.Copy(Filename.Value, result.Dialog.FileName);
		}

		public IEnumerable<IResult> Open()
		{
			yield return new OpenResult(Filename.Value);
		}

		public async Task Revoke()
		{
			await ProcessAction(new Revocation(GetPayload()));
		}

		public async Task Reject()
		{
			await ProcessAction(new Reject(GetPayload()));
		}

		public async Task Sign()
		{
			await ProcessAction(new Sign(GetPayload()));
		}

		public IEnumerable<IResult> PrintItem()
		{
			var task = TaskEx.Run(() => LoadPrintPdf(CurrentItem.Value));
			yield return new TaskResult(task);
			var file = task.Result;
			using (var pdf = PdfDocument.Load(file)) {
				var print = pdf.CreatePrintDocument();
				var dialog = new System.Windows.Forms.PrintDialog();
				dialog.AllowSomePages = true;
				dialog.Document = print;
				dialog.UseEXDialog = true;
				dialog.Document.PrinterSettings.FromPage = 1;
				dialog.Document.PrinterSettings.ToPage = pdf.PageCount;
				if (dialog.ShowDialog() != DialogResult.OK)
					yield break;
				if (dialog.Document.PrinterSettings.FromPage <= pdf.PageCount)
					dialog.Document.Print();
			}
		}

		private async Task ProcessAction(DiadokAction dialog)
		{
			Manager.ShowFixedDialog(dialog);
			if (dialog.Success) {
				var current = CurrentItem.Value;
				var message = await TaskEx.Run(() => api.GetMessage(token, box.BoxId, current.Message.MessageId));
				var index = items.IndexOf(current);
				Entity prev = null;
				if (index > 1)
					prev = items[index - 1].Entity;

				var item = new DisplayItem(message,
					message.Entities.First(x => x.EntityId == current.Entity.EntityId),
					prev, orgs);
				items[index] = item;
				Items.Value[Items.Value.IndexOf(current)] = item;
				CurrentItem.Value = item;
			}
		}

		private ActionPayload GetPayload()
		{
			var payload = new ActionPayload {
				Api = api,
				BoxId = box.BoxId,
				Token = token,
				Entity = CurrentItem.Value.Entity
			};
			return payload;
		}
	}
}