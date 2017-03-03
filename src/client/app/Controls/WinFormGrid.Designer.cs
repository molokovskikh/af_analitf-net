namespace AnalitF.Net.Client.Controls
{
	partial class WinFormGrid
	{
		/// <summary> 
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором компонентов

		/// <summary> 
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.GridEx = new System.Windows.Forms.DataGridView();
			this.PromotionsPanel = new System.Windows.Forms.Panel();
			this.PromotionsCloseButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.PromotionsContentPanel = new System.Windows.Forms.Panel();
			this.ProducerPromotionsCloseButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.ProducerPromotionsPanel = new System.Windows.Forms.Panel();
			this.ProducerPromotionsContentPanel = new System.Windows.Forms.Panel();
			this.OverlayPanel = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.GridEx)).BeginInit();
			this.PromotionsPanel.SuspendLayout();
			this.ProducerPromotionsPanel.SuspendLayout();
			this.OverlayPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// GridEx
			// 
			this.GridEx.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GridEx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GridEx.Location = new System.Drawing.Point(0, 0);
			this.GridEx.Name = "GridEx";
			this.GridEx.Size = new System.Drawing.Size(624, 444);
			this.GridEx.TabIndex = 0;
			this.GridEx.ClientSizeChanged += new System.EventHandler(this.GridEx_ClientSizeChanged);
			// 
			// PromotionsPanel
			// 
			this.PromotionsPanel.AutoScroll = true;
			this.PromotionsPanel.BackColor = System.Drawing.SystemColors.InactiveCaption;
			this.PromotionsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.PromotionsPanel.Controls.Add(this.PromotionsCloseButton);
			this.PromotionsPanel.Controls.Add(this.label1);
			this.PromotionsPanel.Controls.Add(this.PromotionsContentPanel);
			this.PromotionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.PromotionsPanel.Location = new System.Drawing.Point(0, 94);
			this.PromotionsPanel.Name = "PromotionsPanel";
			this.PromotionsPanel.Size = new System.Drawing.Size(533, 95);
			this.PromotionsPanel.TabIndex = 3;
			// 
			// PromotionsCloseButton
			// 
			this.PromotionsCloseButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.ActiveBorder;
			this.PromotionsCloseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.PromotionsCloseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.PromotionsCloseButton.Location = new System.Drawing.Point(433, 6);
			this.PromotionsCloseButton.Name = "PromotionsCloseButton";
			this.PromotionsCloseButton.Size = new System.Drawing.Size(75, 23);
			this.PromotionsCloseButton.TabIndex = 3;
			this.PromotionsCloseButton.Text = "Закрыть";
			this.PromotionsCloseButton.UseVisualStyleBackColor = true;
			this.PromotionsCloseButton.Click += new System.EventHandler(this.PromotionsCloseButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(3, 6);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			this.label1.Size = new System.Drawing.Size(281, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "Сегодня проводятся Акции, поставщики:";
			// 
			// PromotionsContentPanel
			// 
			this.PromotionsContentPanel.AutoScroll = true;
			this.PromotionsContentPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.PromotionsContentPanel.Location = new System.Drawing.Point(0, 36);
			this.PromotionsContentPanel.Name = "PromotionsContentPanel";
			this.PromotionsContentPanel.Size = new System.Drawing.Size(531, 57);
			this.PromotionsContentPanel.TabIndex = 4;
			// 
			// ProducerPromotionsCloseButton
			// 
			this.ProducerPromotionsCloseButton.AutoEllipsis = true;
			this.ProducerPromotionsCloseButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.ActiveBorder;
			this.ProducerPromotionsCloseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.ProducerPromotionsCloseButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.ProducerPromotionsCloseButton.Location = new System.Drawing.Point(434, 5);
			this.ProducerPromotionsCloseButton.Name = "ProducerPromotionsCloseButton";
			this.ProducerPromotionsCloseButton.Size = new System.Drawing.Size(75, 23);
			this.ProducerPromotionsCloseButton.TabIndex = 2;
			this.ProducerPromotionsCloseButton.Text = "Закрыть";
			this.ProducerPromotionsCloseButton.UseVisualStyleBackColor = true;
			this.ProducerPromotionsCloseButton.Click += new System.EventHandler(this.ProducerPromotionsCloseButton_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label2.Location = new System.Drawing.Point(3, 12);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			this.label2.Size = new System.Drawing.Size(306, 16);
			this.label2.TabIndex = 1;
			this.label2.Text = "Сегодня проводятся Акции, производители:";
			// 
			// ProducerPromotionsPanel
			// 
			this.ProducerPromotionsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ProducerPromotionsPanel.Controls.Add(this.ProducerPromotionsContentPanel);
			this.ProducerPromotionsPanel.Controls.Add(this.label2);
			this.ProducerPromotionsPanel.Controls.Add(this.ProducerPromotionsCloseButton);
			this.ProducerPromotionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.ProducerPromotionsPanel.Location = new System.Drawing.Point(0, 0);
			this.ProducerPromotionsPanel.Name = "ProducerPromotionsPanel";
			this.ProducerPromotionsPanel.Size = new System.Drawing.Size(533, 94);
			this.ProducerPromotionsPanel.TabIndex = 0;
			// 
			// ProducerPromotionsContentPanel
			// 
			this.ProducerPromotionsContentPanel.AutoScroll = true;
			this.ProducerPromotionsContentPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.ProducerPromotionsContentPanel.Location = new System.Drawing.Point(0, 32);
			this.ProducerPromotionsContentPanel.Name = "ProducerPromotionsContentPanel";
			this.ProducerPromotionsContentPanel.Size = new System.Drawing.Size(531, 60);
			this.ProducerPromotionsContentPanel.TabIndex = 3;
			// 
			// OverlayPanel
			// 
			this.OverlayPanel.AutoScroll = true;
			this.OverlayPanel.Controls.Add(this.PromotionsPanel);
			this.OverlayPanel.Controls.Add(this.ProducerPromotionsPanel);
			this.OverlayPanel.Location = new System.Drawing.Point(24, 20);
			this.OverlayPanel.Name = "OverlayPanel";
			this.OverlayPanel.Size = new System.Drawing.Size(550, 161);
			this.OverlayPanel.TabIndex = 0;
			// 
			// WinFormGrid
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.OverlayPanel);
			this.Controls.Add(this.GridEx);
			this.Name = "WinFormGrid";
			this.Size = new System.Drawing.Size(624, 444);
			((System.ComponentModel.ISupportInitialize)(this.GridEx)).EndInit();
			this.PromotionsPanel.ResumeLayout(false);
			this.PromotionsPanel.PerformLayout();
			this.ProducerPromotionsPanel.ResumeLayout(false);
			this.ProducerPromotionsPanel.PerformLayout();
			this.OverlayPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView GridEx;
		private System.Windows.Forms.Button ProducerPromotionsCloseButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel PromotionsPanel;
		private System.Windows.Forms.Button PromotionsCloseButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel PromotionsContentPanel;
		private System.Windows.Forms.Panel ProducerPromotionsPanel;
		private System.Windows.Forms.Panel ProducerPromotionsContentPanel;
		private System.Windows.Forms.Panel OverlayPanel;
	}
}
