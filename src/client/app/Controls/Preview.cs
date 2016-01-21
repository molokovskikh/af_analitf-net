using System;
using System.Drawing;
using System.IO;
using System.Net.Cache;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AnalitF.Net.Client.Helpers;
using Common.Tools;
using PdfiumViewer;
using Image = System.Windows.Controls.Image;

namespace AnalitF.Net.Client.Controls
{
	public class Preview : ContentControl
	{
		public static DependencyProperty FilenameProperty = DependencyProperty.Register("Filename",
			typeof(string), typeof(Preview), new PropertyMetadata(FilenameChanged));

		public string Filename
		{
			get { return (string)GetValue(FilenameProperty); }
			set { SetValue(FilenameProperty, value); }
		}

		private static void FilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var preview = (Preview)d;
			var scroll = preview.Parent as ScrollViewer;
			scroll.ScrollToHome();
			scroll.ScrollToTop();
			var filename = (string)e.NewValue;
			if (String.IsNullOrEmpty(filename)) {
				preview.Content = null;
				return;
			}
			if (!File.Exists(filename)) {
				preview.Content = new Label {
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					Content = "Файл не найден",
					FontWeight = FontWeights.Bold,
					FontSize = 18
				};
				return;
			}

			if (Path.GetExtension(filename).Match(".pdf")) {
				using (var doc = PdfDocument.Load(filename)) {
					var pannel = new StackPanel();
					preview.Content = pannel;
					for(var i = 0; i < doc.PageCount; i++) {
						var pageSize = doc.PageSizes[i];
						var factor = preview.ActualWidth / pageSize.Width;
						var width = (int)preview.ActualWidth;
						var height = (int)(pageSize.Height * factor);
						using (var page = (Bitmap)doc.Render(i, width - 30, height - 30, 96, 96, false)) {
							var intPtr = page.GetHbitmap();
							try {
								pannel.Children.Add(new Image {
									Source = Imaging.CreateBitmapSourceFromHBitmap(intPtr, IntPtr.Zero,
										Int32Rect.Empty,
										BitmapSizeOptions.FromEmptyOptions())
								});
							}
							finally {
								WinApi.DeleteObject(intPtr);
							}
						}
					}
				}
				return;
			}

			try {
				preview.Content = new Image {
					Source = new BitmapImage(new Uri(filename)) {
						CreateOptions = BitmapCreateOptions.IgnoreImageCache,
						CacheOption = BitmapCacheOption.OnLoad
					}
				};
			} catch(Exception) {
				preview.Content = new Label {
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
					Content = "Формат не поддерживается",
					FontWeight = FontWeights.Bold,
					FontSize = 18
				};
			}
		}
	}
}