using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Inventory;
using System.Reactive.Concurrency;
using System.ComponentModel;
using System.Reactive.Disposables;
using ReactiveUI;
using System.Reactive.Linq;

namespace AnalitF.Net.Client.Views.Inventory
{
	/// <summary>
	/// Interaction logic for InventoryDocs.xaml
	/// </summary>
	public partial class InventoryDocs : UserControl
	{
		private SerialDisposable _ref = new SerialDisposable();

		public InventoryDocs()
		{
			InitializeComponent();
			ApplyStyles();

			Items.ItemSourceChanged += (sender, args) => {
				var collection = Items.ItemsSource as ReactiveCollection<InventoryDoc>;
				if (collection != null)
					_ref.Disposable = collection.ItemChanged.Throttle(TimeSpan.FromMilliseconds(100), DispatcherScheduler.Current)
						.Subscribe(_ => {
							((IEditableCollectionView)Items.Items).CommitEdit();
							Items.Items.Refresh();
						});
			};
		}

		public void ApplyStyles()
		{
			StyleHelper.ApplyStyles(typeof(InventoryDoc), Items, Application.Current.Resources, Legend);
		}
	}
}
