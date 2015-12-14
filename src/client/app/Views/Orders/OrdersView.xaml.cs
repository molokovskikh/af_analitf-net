using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Orders;
using ReactiveUI;

namespace AnalitF.Net.Client.Views.Orders
{
	public partial class OrdersView : UserControl
	{
		private SerialDisposable ordersRef = new SerialDisposable();

		public OrdersView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				ApplyStyles();
			};

			DataContextChanged += (sender, args) => {
				var model = DataContext as OrdersViewModel;
				if (model != null) {
					if (!model.User.HaveLimits)
						Orders.Columns.Remove(DataGridHelper.FindColumn(Orders, "Лимит"));
				}
			};


			Orders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));

			SentOrders.CommandBindings.Add(new CommandBinding(DataGrid.DeleteCommand,
				Commands.DoInvokeViewModel,
				Commands.CanInvokeViewModel));
			Orders.ItemSourceChanged += (sender, args) => {
				var collection = Orders.ItemsSource as ReactiveCollection<Order>;
				if (collection != null)
					ordersRef.Disposable = collection.ItemChanged.Throttle(TimeSpan.FromMilliseconds(100), DispatcherScheduler.Current)
						.Subscribe(_ => {
							((IEditableCollectionView)Orders.Items).CommitEdit();
							Orders.Items.Refresh();
						});
			};
		}

		public void ApplyStyles()
		{
			var context = "";
			if (((BaseScreen)DataContext).User != null && ((BaseScreen)DataContext).User.IsPreprocessOrders)
				context = "CorrectionEnabled";
			StyleHelper.ApplyStyles(typeof(Order), Orders, Application.Current.Resources, Legend, context);
			StyleHelper.ApplyStyles(typeof(SentOrder), SentOrders, Application.Current.Resources, Legend);
		}
	}
}
