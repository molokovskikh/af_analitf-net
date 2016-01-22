using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using Common.Tools;
using NHibernate.Linq;
using PdfiumViewer;
using AnalitF.Net.Client.Models.Sbis;
using TaskResult = AnalitF.Net.Client.Models.Results.TaskResult;

namespace AnalitF.Net.Client.ViewModels.Sbis
{
	public class DisplayItem : INotifyPropertyChanged
	{
		private string localFilename;
		private string status;
		public Attach Attachment;
		public Doc Message;

		public DisplayItem()
		{
		}

		public string Sender { get; set; }
		public string FileName { get; set; }
		public string Status
		{
			get { return status; }
			set
			{
				if (status != value) {
					status = value;
					OnPropertyChanged();
				}
			}
		}

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

		public bool CanSign()
		{
			return true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class Index : BaseScreen
	{
		private static string[] fileformats = {
			".bmp", ".jpeg", ".jpg", ".png", ".tiff", ".gif", ".pdf"
		};

		private ObservableCollection<DisplayItem> items = new ObservableCollection<DisplayItem>();
		private int currentPage;
		private HttpClient client;

		public Index()
		{
			InitFields();

			DisplayName = "Сбис";
			CurrentItem
				.Select(x => {
					if (x == null)
						return Observable.Return<string>(null);
					return Observable.StartAsync(async () => {
						var dir = FileHelper.MakeRooted("sbis");
						Directory.CreateDirectory(dir);
						var filename = Directory.GetFiles(dir, $"{x.Attachment.Идентификатор}.*").FirstOrDefault();
						if (!String.IsNullOrEmpty(filename))
							return filename;

						var result = await client.JsonRpc("СБИС.ПрочитатьДокумент", new {
							Документ = new {
								Идентификатор = x.Message.Идентификатор,
							}
						});
						var attachments = result["result"]["Вложение"].ToObject<Attach[]>();
						var attachment = attachments.FirstOrDefault(y => y.Идентификатор == x.Attachment.Идентификатор);
						if (attachment == null)
							throw new Exception($"Не удалось найти вложение с идентификатором {x.Attachment.Идентификатор}");

						x.Attachment = attachment;
						filename = Path.Combine(dir, x.Attachment.Идентификатор + Path.GetExtension(x.FileName));
						await LoadToFile(filename, attachment.Файл.Ссылка);
						return filename;
					}).Catch<string, Exception>(y => {
						Log.Error("Не удалось загрузить документ", y);
						return Observable.Return<string>(null);
					});
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

					return Observable
						.StartAsync(async () => await LoadPrintPdf(attachment))
						.Catch<string, Exception>(y => {
							Log.Error("Не удалось загрузить pdf");
							return Observable.Return<string>(null);
						});
				})
				.Switch()
				.ObserveOn(UiScheduler)
				.Subscribe(PreviewFilename);

			CurrentItem.Select(x => (x?.CanSign()).GetValueOrDefault())
				.Subscribe(CanSign);
			CurrentItem.Select(x => (x?.CanSign()).GetValueOrDefault())
				.Subscribe(CanReject);

			Filename.Select(x => x != null)
				.Subscribe(CanOpen);
			Filename.Select(x => x != null)
				.Subscribe(CanSave);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanDelete);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanDeleteItem);
			CurrentItem.Select(x => x != null)
				.Subscribe(CanPrintItem);

			SearchTerm
				.Throttle(Consts.TextInputLoadTimeout, Scheduler)
				.Select(x => String.IsNullOrEmpty(x)
					? items.ToObservableCollection()
					: items.Where(y => y.FileName.IndexOf(x, StringComparison.CurrentCultureIgnoreCase) >= 0)
						.ToObservableCollection())
				.Subscribe(Items);
			//CurrentItem.Select(x => AttachmentHistory.Collect(x))
			//	.Subscribe(History);
			IsLoading.Select(x => !x).Subscribe(IsLoaded);
			IsLoading.Select(x => !x).Subscribe(CanPrev);
			IsLoading.Select(x => !x).Subscribe(CanNext);
			IsLoading.Select(x => !x).Subscribe(CanReload);
		}

		private async Task LoadToFile(string filename, string uri)
		{
			var file = await client.GetAsync(uri);
			file.EnsureSuccessStatusCode();
			var bytes = await file.Content.ReadAsByteArrayAsync();
			File.WriteAllBytes(filename, bytes);
		}

		private async Task<string> LoadPrintPdf(DisplayItem item)
		{
			var dir = FileHelper.MakeRooted("sbis");
			var file = Path.Combine(dir, $"print.{item.Attachment.Идентификатор}.pdf");
			if (File.Exists(file))
				return file;
			if (String.IsNullOrEmpty(item.Attachment.СсылкаНаPDF))
				return null;
			await LoadToFile(file, item.Attachment.СсылкаНаPDF);
			return file;
		}

		public NotifyValue<string> Total { get; set; }
		public NotifyValue<bool> CanPrintItem { get; set; }
		public NotifyValue<bool> CanPrev { get; set; }
		public NotifyValue<bool> CanNext { get; set; }
		public NotifyValue<bool> CanReload { get; set; }
		public NotifyValue<bool> CanReject { get; set; }
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
		//public NotifyValue<List<AttachmentHistory>> History { get; set; }
		public NotifyValue<bool> IsLoaded { get; set; }
		public NotifyValue<bool> IsLoading { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			if (IsConfigured()) {
				Manager.Warning("Для начала работы нужно ввести учетные данные Сбис." +
					" Если Вы не зарегистрированы в системе Сбис нужно пройти регистрацию на сайте https://sbis.ru.");
				Shell.ShowSettings("SbisTab");
				Session.Evict(Settings.Value);
				Settings.Value = Session.Query<Settings>().First();
				if (IsConfigured()) {
					return;
				}
			}
			Reload();
		}

		public override void PostActivated()
		{
			base.PostActivated();

			if (IsConfigured()) {
				TryClose();
			}
		}

		private bool IsConfigured()
		{
			return String.IsNullOrEmpty(Settings.Value.SbisUsername)
				|| String.IsNullOrEmpty(Settings.Value.SbisPassword);
		}

		public void Prev()
		{
			Reload(currentPage - 1);
		}

		public void Next()
		{
			Reload(currentPage + 1);
		}

		public void Reload(int page = 0)
		{
			if (page < 0)
				page = 0;
			IsLoading.Value = true;
			if (client == null) {
				client = new HttpClient();
				OnCloseDisposable.Add(client);
			}
			Observable.StartAsync(async () => {
					if (!client.DefaultRequestHeaders.Contains("X-SBISSessionID")) {
						var login = await client.JsonRpc("СБИС.Аутентифицировать", new {
							Логин = Settings.Value.SbisUsername,
							Пароль = Settings.Value.SbisPassword
						});
						client.DefaultRequestHeaders.Add("X-SBISSessionID", login["result"].ToObject<string>());
					}
					var result = await client.JsonRpc("СБИС.СписокДокументовПоСобытиям", new {
						Фильтр = new {
							ТипРеестра = "Входящие",
							НашаОтганизация = new {
								СвЮЛ = new {
									КодФилиала = ""
								}
							},
							Навигация = new {
								РазмерСтраницы = "100",
								Страница = page.ToString(),
								ВернутьРазмерСписка = "Да"
							}
						}
					});
					return result.GetValue("result").ToObject<Result>();
				})
				.ObserveOn(UiScheduler)
				.Do(_ => IsLoading.Value = false, _ => IsLoading.Value = false)
				.Subscribe(x => {
					SearchTerm.Value = "";
					Total.Value = $"{page + 1} из {x.Навигация.РазмерСписка / x.Навигация.РазмерСтраницы}";
					currentPage = page;
					var display = new List<DisplayItem>();
					foreach (var message in x.Реестр) {
						DateTime date;
						DateTime.TryParseExact(message.ДатаВремя, "dd.MM.yyyy HH.mm.ss", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out date);
						var sender = message.Документ?.Контрагент?.СвЮЛ?.Название;
						foreach (var attachment in message.Документ?.Вложение ?? new Attach[0]) {
							display.Add(new DisplayItem {
								Sender = sender,
								FileName = attachment.Название,
								Date = date,
								Status = message.Документ?.Состояние?.Название,
								Department = message.Документ?.Ответственный?.ToString(),
								Attachment = attachment,
								Message = message.Документ,
							});
							sender = null;
						}
					}
					items = display.ToObservableCollection();
					Items.Value = display.ToObservableCollection();
				}, e => {
					Log.Error("Не удалось загрузить документы", e);
					Manager.Error(ErrorHelper.TranslateException(e)
						?? "Не удалось выполнить операцию, попробуйте повторить позднее.");
				});
		}

		public IResult Delete()
		{
			if (!Confirm("Удалить документ?"))
				return null;
			return new TaskResult(InnerDelete());
		}

		public async Task InnerDelete()
		{
			var result = await client.JsonRpc("СБИС.УдалитьДокумент", new {
				Документ = new {
					Идентификатор = CurrentItem.Value.Message.Идентификатор,
				}
			});
			var id = CurrentItem.Value.Message.Идентификатор;
			items.RemoveEach(x => x.Message.Идентификатор == id);
			Items.Value.RemoveEach(x => x.Message.Идентификатор == id);
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

		public IEnumerable<IResult> Reject()
		{
			var model = new Reject();
			yield return new DialogResult(model);
			yield return new TaskResult(InnerRevoke(model));
		}

		public async Task InnerRevoke(Reject model)
		{
			await DoAction("Утверждение", "Отклонить", model.Comment);
		}

		public IResult Sign()
		{
			return new TaskResult(InnerSign());
		}

		public async Task InnerSign()
		{
			await DoAction("Утверждение", "Утвердить");
		}

		private async Task DoAction(string phase, string action, string comment = null)
		{
			var cert = Settings.Value.GetCert(Settings.Value.SbisCert);
			var parts = Util.ParseSubject(cert);
			var result = await client.JsonRpc("СБИС.ПодготовитьДействие", new {
				Документ = new {
					Идентификатор = CurrentItem.Value.Message.Идентификатор,
					Этап = new {
						Название = phase,
						Действие = new {
							Название = action,
							Комментарий = comment,
							Сертификат = new {
								ФИО = parts.GetValueOrDefault("SN") + " " + parts.GetValueOrDefault("G"),
								Должность = parts.GetValueOrDefault("T"),
								ИНН = parts.GetValueOrDefault("ИНН")
							}
						}
					}
				}
			});
			var attachments = result["result"]["Этап"][0]["Вложение"].ToObject<Attach[]>();
			var crypt = new Diadoc.Api.Cryptography.WinApiCrypt();
			var signs = new List<Attach>();
			foreach (var attachment in attachments) {
				var file = await client.GetAsync(attachment.Файл.Ссылка);
				file.EnsureSuccessStatusCode();
				var content = await file.Content.ReadAsByteArrayAsync();
				var signature = await Util.Run(() => crypt.Sign(content, cert.RawData));
				signs.Add(new Attach {
					Идентификатор = attachment.Идентификатор,
					Подпись = new[] { new FileRef(signature) }
				});
			}
			result = await client.JsonRpc("СБИС.ВыполнитьДействие", new {
				Документ = new {
					Идентификатор = CurrentItem.Value.Message.Идентификатор,
					Этап = new {
						Название = phase,
						Вложение = signs.ToArray(),
						Действие = new {
							Название = action,
						}
					}
				}
			});
			var doc = result["result"].ToObject<Doc>();
			foreach (var item in items.Where(x => x.Message.Идентификатор == doc.Идентификатор)) {
				item.Message.Состояние = doc.Состояние;
				item.Status = doc.Состояние?.Название;
			}
		}

		public IEnumerable<IResult> PrintItem()
		{
			var task = Util.Run(async () => await LoadPrintPdf(CurrentItem.Value));
			yield return new TaskResult(task);
			var file = task.Result.Result;
			using (var pdf = PdfDocument.Load(file)) {
				var print = pdf.CreatePrintDocument();
				var dialog = new System.Windows.Forms.PrintDialog();
				dialog.AllowSomePages = true;
				dialog.Document = print;
				dialog.UseEXDialog = true;
				dialog.Document.PrinterSettings.FromPage = 1;
				dialog.Document.PrinterSettings.ToPage = pdf.PageCount;
				if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
					yield break;
				if (dialog.Document.PrinterSettings.FromPage <= pdf.PageCount)
					dialog.Document.Print();
			}
		}
	}
}