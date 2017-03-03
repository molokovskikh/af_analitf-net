using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Caliburn.Micro;

namespace AnalitF.Net.Client.Controls
{
	public partial class WinFormGrid : UserControl
	{
		private List<Models.ProducerPromotion> producerPromotionsItems = new List<Models.ProducerPromotion>();

		private List<Models.Promotion> promotionsItems = new List<Models.Promotion>();

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
			CalcLocation();
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
			if (producerPromotionsItems != null)
			{
				int y = 0;
				foreach (var item in producerPromotionsItems)
				{
					Label ProducerName = new Label();
					ProducerName.Font = new Font(Font, FontStyle.Bold);
					ProducerName.Parent = ProducerPromotionsContentPanel;
					ProducerName.Location = new Point(15, y);
					ProducerName.MaximumSize = new Size(ProducerPromotionsContentPanel.Width / 2 - 5, 0);
					ProducerName.AutoSize = true;
					ProducerName.Text = item.Producer.Name;


					LinkLabel Open = new LinkLabel();
					Open.Parent = ProducerPromotionsContentPanel;
					Open.Location = new Point(ProducerPromotionsContentPanel.Size.Width / 2 + 15, y);
					Open.MaximumSize = new Size(ProducerPromotionsContentPanel.Width / 2 - 5, 0);
					Open.AutoSize = true;
					Open.Text = item.Name;
					Open.Click += (s1, e1) =>
					{
						new Models.Results.DialogResult(new ViewModels.Dialogs.DocModel<Models.ProducerPromotion>(item.Id), resizable: true).Execute(new ActionExecutionContext());
					};

					y += (ProducerName.Height > Open.Height ? ProducerName.Height : Open.Height);

					Label Annotation = new Label();
					Annotation.AutoSize = true;
					Annotation.Parent = ProducerPromotionsContentPanel;
					Annotation.Location = new Point(15, y);
					Annotation.Text = item.Annotation;

					y += Annotation.Height;
				}
			}
		}

		private void WinFormGrid_PromotionsItemsChangeEvent(object sender)
		{
			if (promotionsItems != null)
			{
				int y = 0;
				foreach (var item in promotionsItems)
				{
					Label SupplierName = new Label();
					SupplierName.Font = new Font(Font, FontStyle.Bold);
					SupplierName.Parent = PromotionsContentPanel;
					SupplierName.Location = new Point(15, y);
					SupplierName.MaximumSize = new Size(PromotionsContentPanel.Width / 2 - 5, 0);
					SupplierName.AutoSize = true;
					SupplierName.Text = item.Supplier.Name;


					LinkLabel Open = new LinkLabel();
					Open.Parent = PromotionsContentPanel;
					Open.Location = new Point(PromotionsContentPanel.Size.Width / 2 + 15, y);
					Open.MaximumSize = new Size(PromotionsContentPanel.Width / 2 - 5, 0);
					Open.AutoSize = true;
					Open.Text = item.Name;
					Open.Click += (s1, e1) =>
					{
						new Models.Results.DialogResult(new ViewModels.Dialogs.DocModel<Models.Promotion>(item.Id), resizable: true).Execute(new ActionExecutionContext());
					};

					y += (SupplierName.Height > Open.Height ? SupplierName.Height : Open.Height);

					Label Annotation = new Label();
					Annotation.AutoSize = true;
					Annotation.Parent = PromotionsContentPanel;
					Annotation.Location = new Point(15, y);
					Annotation.Text = item.Annotation;

					y += Annotation.Height;
				}
			}
		}

		private void CalcLocation()
		{
			var ProducerPromotionsPanelHeight = (ProducerPromotionsPanel.Visible ? ProducerPromotionsPanel.Height : 0);
			var PromotionsPanelHeight = (PromotionsPanel.Visible ? PromotionsPanel.Height : 0);
			OverlayPanel.Height = 0;
			OverlayPanel.Height += ProducerPromotionsPanelHeight;
			OverlayPanel.Height += PromotionsPanelHeight;
			if (OverlayPanel.Height > (int)(Grid.Height * 0.7))
				OverlayPanel.Height = (int)(Grid.Height * 0.7);
			OverlayPanel.Location = new Point(Grid.Width / 2 - OverlayPanel.Width / 2, Grid.Height / 2 - OverlayPanel.Height / 2);
			if (PromotionsPanel.Visible || ProducerPromotionsPanel.Visible)
				OverlayPanel.Visible = true;
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
	}
}
