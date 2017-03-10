using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Caliburn.Micro;
using Common.Tools;
using Message = Common.Tools.Message;
using System.Runtime.CompilerServices;

namespace AnalitF.Net.Client.Controls
{
	public partial class WinFormGrid : UserControl
	{
		private int maxPromotionsPaneContentlHeight;
		private int maxProducerPromotionsPanelContentHeight;

		System.Timers.Timer WarningTimer = new System.Timers.Timer();

		private List<Models.ProducerPromotion> producerPromotionsItems = new List<Models.ProducerPromotion>();

		private List<Models.Promotion> promotionsItems = new List<Models.Promotion>();

		private List<Message> orderWarning = new List<Message>();

		public DataGridView Grid
		{ get { return GridEx; } }

		public Panel ProducerPromotions
		{
			get { return ProducerPromotionsPanel; }
		}

		public Panel Promotions
		{
			get { return PromotionsPanel; }
		}


		public object ProducerPromotionsItems
		{
			get { return producerPromotionsItems; }
			set
			{
				producerPromotionsItems = value as List<Models.ProducerPromotion>;
				GenerateProducerPromotionsItemsChangeEvent(this);
			}
		}

		public object PromotionsItems
		{
			get { return promotionsItems; }
			set
			{
				promotionsItems = value as List<Models.Promotion>;
				GeneratePromotionsItemsChangeEvent(this);
			}
		}

		public object OrderWarning
		{
			get { return orderWarning; }
			set
			{
				if (value != null)
				{
					orderWarning = value as List<Message>;
					GenerateOrderWarningChangeEvent(this);
				}
			}
		}

		public WinFormGrid()
		{
			InitializeComponent();
			ProducerPromotionsPanel.BackColor = Color.FromArgb(255, 251, 251, 251);
			PromotionsPanel.BackColor = Color.FromArgb(255, 251, 251, 251);
			BackColor = Color.FromArgb(255, 251, 251, 251);
			ProducerPromotionsItemsChangeEvent += WinFormGrid_ProducerPromotionsItemsChangeEvent;
			PromotionsItemsChangeEvent += WinFormGrid_PromotionsItemsChangeEvent;
			ProducerPromotionsPanel.VisibleChanged += ProducerPromotionsPanel_VisibleChanged1;
			PromotionsPanel.VisibleChanged += PromotionsPanel_VisibleChanged1;
			OrderWarningChangeEvent += WinFormGrid_OrderWarningChangeEvent;
			WarningTimer.Elapsed += new System.Timers.ElapsedEventHandler(WarningTimer_Elapsed);
			WarningTimer.AutoReset = false;
			CalcLocation();
		}
		void WarningTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Invoke(new System.Action(() => {
				OrderWarningLabel.Text = "";
				CalcLocation();
			}));
		}

		private void WinFormGrid_OrderWarningChangeEvent(object sender)
		{
			var warnings = orderWarning.Where(m => m.IsWarning).Implode(Environment.NewLine);
			//нельзя перетирать старые предупреждения, предупреждения очищаются только по таймеру
			if (String.IsNullOrEmpty(warnings))
				return;

			foreach (Message item in orderWarning)
			{
				if (item.IsWarning && !OrderWarningLabel.Text.Contains(item.MessageText))
				{
					OrderWarningLabel.Text += "\n" + item.MessageText;
				}
			}
			CalcLocation();
			WarningTimer.Interval = ViewModels.Consts.WarningTimeout.TotalMilliseconds;
			WarningTimer.Start();
		}

		private void PromotionsPanel_VisibleChanged1(object sender, EventArgs e)
		{
			CalcLocation();
		}

		private void ProducerPromotionsPanel_VisibleChanged1(object sender, EventArgs e)
		{
			CalcLocation();
		}

		private void WinFormGrid_ProducerPromotionsItemsChangeEvent(object sender)
		{
			AddControl<Models.ProducerPromotion>(producerPromotionsItems, ProducerPromotionsContentPanel);
		}

		private void WinFormGrid_PromotionsItemsChangeEvent(object sender)
		{
			AddControl<Models.Promotion>(promotionsItems, PromotionsContentPanel);
		}

		private void CalcLocation()
		{
			OrderWarningLabel.Visible = !String.IsNullOrEmpty(OrderWarningLabel.Text);
			var OrderWarningLabelHeight = (OrderWarningLabel.Visible ? OrderWarningLabel.Height : 0);
			var ProducerPromotionsPanelHeight = (ProducerPromotionsPanel.Visible ? ProducerPromotionsPanel.Height : 0);
			var PromotionsPanelHeight = (PromotionsPanel.Visible ? PromotionsPanel.Height : 0);

			OverlayPanel.Height = (int)((Grid.Height) * 0.7);

			var halfArea = (int)((Grid.Height - OrderWarningLabelHeight) / 2 * 0.7);
			var differencePromotionsPanel = maxPromotionsPaneContentlHeight - PromotionsPanel.Height 
					+ (PromotionsPanel.Height - PromotionsContentPanel.Height);
			if (differencePromotionsPanel < 0)
				differencePromotionsPanel = 0;
			var differenceProducerPromotionsPanel = maxProducerPromotionsPanelContentHeight - ProducerPromotionsPanel.Height 
					+ (ProducerPromotionsPanel.Height - ProducerPromotionsContentPanel.Height);
			if (differenceProducerPromotionsPanel < 0)
				differenceProducerPromotionsPanel = 0;
			var FreeAreaPromotionsPanel = halfArea - (PromotionsPanelHeight + differencePromotionsPanel);
			var FreeAreaProducerPromotionsPanel = halfArea - (ProducerPromotionsPanelHeight + differenceProducerPromotionsPanel);

			if (differencePromotionsPanel > 0 
				&& PromotionsPanel.Height + differencePromotionsPanel < halfArea + FreeAreaProducerPromotionsPanel)
			{
				PromotionsPanel.Height += differencePromotionsPanel;
				PromotionsContentPanel.Height += differencePromotionsPanel;
			}
			if (differencePromotionsPanel > 0
				&& PromotionsPanel.Height + differencePromotionsPanel >= halfArea + FreeAreaProducerPromotionsPanel)
			{
				PromotionsPanel.Height += halfArea + FreeAreaProducerPromotionsPanel;
				PromotionsContentPanel.Height += halfArea + FreeAreaProducerPromotionsPanel;
			}

			if (differenceProducerPromotionsPanel > 0 
				&& ProducerPromotionsPanel.Height + differenceProducerPromotionsPanel < halfArea + FreeAreaPromotionsPanel)
			{
				ProducerPromotionsPanel.Height += differenceProducerPromotionsPanel;
				ProducerPromotionsContentPanel.Height += differenceProducerPromotionsPanel;
			}
			if (differenceProducerPromotionsPanel > 0 
				&& ProducerPromotionsPanel.Height + differenceProducerPromotionsPanel >= halfArea + FreeAreaPromotionsPanel)
			{
				ProducerPromotionsPanel.Height += halfArea + FreeAreaPromotionsPanel;
				ProducerPromotionsContentPanel.Height += halfArea + FreeAreaPromotionsPanel;
			}


			OverlayPanel.Height = 0;
			OverlayPanel.Height += OrderWarningLabelHeight;
			OverlayPanel.Height += ProducerPromotionsPanelHeight;
			OverlayPanel.Height += PromotionsPanelHeight;
			if (OverlayPanel.Height > (int)((Grid.Height - OrderWarningLabelHeight) * 0.7))
				OverlayPanel.Height = (int)((Grid.Height - OrderWarningLabelHeight) * 0.7);
			OverlayPanel.Location = new Point(Grid.Width / 2 - OverlayPanel.Width / 2, Grid.Height / 2 - OverlayPanel.Height / 2);
			if (PromotionsPanel.Visible || ProducerPromotionsPanel.Visible || OrderWarningLabel.Visible)
				OverlayPanel.Visible = true;
			else
			{
				Grid.Focus();
			}
		}

		private void GridEx_ClientSizeChanged(object sender, EventArgs e)
		{
			CalcLocation();
		}

		private void ProducerPromotionsCloseButton_Click(object sender, EventArgs e)
		{
			ProducerPromotionsPanel.Visible = false;
			CalcLocation();
		}

		private void PromotionsCloseButton_Click(object sender, EventArgs e)
		{
			PromotionsPanel.Visible = false;
			CalcLocation();
		}

		#region событие изменение ProducerPromotionsItems
		public delegate void ProducerPromotionsItemsChangeHandler(object sender);

		public event ProducerPromotionsItemsChangeHandler ProducerPromotionsItemsChangeEvent;

		public void GenerateProducerPromotionsItemsChangeEvent(object Sender)
		{
			if (ProducerPromotionsItemsChangeEvent != null)
			{
				ProducerPromotionsItemsChangeEvent(this);
			}
		}
		#endregion

		#region событие изменение PromotionsItems
		public delegate void PromotionsItemsChangeHandler(object sender);

		public event PromotionsItemsChangeHandler PromotionsItemsChangeEvent;

		public void GeneratePromotionsItemsChangeEvent(object Sender)
		{
			if (PromotionsItemsChangeEvent != null)
			{
				PromotionsItemsChangeEvent(this);
			}
		}
		#endregion

		#region событие изменение OrderWarning
		public delegate void OrderWarningChangeHandler(object sender);

		public event OrderWarningChangeHandler OrderWarningChangeEvent;

		public void GenerateOrderWarningChangeEvent(object Sender)
		{
			if (OrderWarningChangeEvent != null)
			{
				OrderWarningChangeEvent(this);
			}
		}
		#endregion

		private void AddControl<T>(object items, Panel parent)
		{
			if (items != null)
			{
				int y = 0;
				foreach (var item in items as List<T>)
				{
					Label Name = new Label();
					Name.Font = new Font(Font, FontStyle.Bold);
					Name.Parent = parent;
					Name.Location = new Point(15, y);
					Name.MaximumSize = new Size(parent.Width / 2 - 5, 0);
					Name.Font = new Font("Segoe UI", 10);
					Name.AutoSize = true;

					LinkLabel Open = new LinkLabel();
					Open.Parent = parent;
					Open.Location = new Point(parent.Size.Width / 2 + 15, y);
					Open.MaximumSize = new Size(parent.Width / 2 - 5, 0);
					Open.Font = new Font("Segoe UI", 10);
					Open.AutoSize = true;
					Open.Click += (s1, e1) =>
					{
						if (item is Models.Promotion)
							new Models.Results.DialogResult(new ViewModels.Dialogs.DocModel<Models.Promotion>((item as Models.Promotion).Id)
								, resizable: true).Execute(new ActionExecutionContext());
						else if (item is Models.ProducerPromotion)
							new Models.Results.DialogResult(new ViewModels.Dialogs.DocModel<Models.ProducerPromotion>((item as Models.ProducerPromotion).Id)
								, resizable: true).Execute(new ActionExecutionContext());
					};

					y += (Name.Height > Open.Height ? Name.Height : Open.Height);

					Label Annotation = new Label();
					Annotation.AutoSize = true;
					Annotation.Parent = parent;
					Annotation.Location = new Point(15, y);
					Annotation.Font = new Font("Segoe UI", 10);
					if (item is Models.Promotion)
					{
						Name.Text = (item as Models.Promotion).Supplier.Name;
						Open.Text = (item as Models.Promotion).Name;
						Annotation.Text = (item as Models.Promotion).Annotation;
					}
					else if (item is Models.ProducerPromotion)
					{
						Name.Text = (item as Models.ProducerPromotion).Producer.Name;
						Open.Text = (item as Models.ProducerPromotion).Name;
						Annotation.Text = (item as Models.ProducerPromotion).Annotation;
					}

					y += Annotation.Height;
					if (parent == PromotionsContentPanel)
					{
						maxPromotionsPaneContentlHeight = y;
					}
					else if (parent == ProducerPromotionsContentPanel)
					{
						maxProducerPromotionsPanelContentHeight = y;
					}
				}
			}
		}
	}
}
