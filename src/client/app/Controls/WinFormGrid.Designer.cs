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
			((System.ComponentModel.ISupportInitialize)(this.GridEx)).BeginInit();
			this.SuspendLayout();
			// 
			// GridEx
			// 
			this.GridEx.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GridEx.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GridEx.Location = new System.Drawing.Point(0, 0);
			this.GridEx.Name = "GridEx";
			this.GridEx.Size = new System.Drawing.Size(645, 308);
			this.GridEx.TabIndex = 0;
			// 
			// WinFormGrid
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.GridEx);
			this.Name = "WinFormGrid";
			this.Size = new System.Drawing.Size(645, 308);
			((System.ComponentModel.ISupportInitialize)(this.GridEx)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridView GridEx;
	}
}
