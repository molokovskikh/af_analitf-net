using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms.Integration;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Configuration;
using AnalitF.Net.Client.Config.Caliburn;
using AnalitF.Net.Client.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reactive.Linq;
using System.Windows.Input;

namespace AnalitF.Net.Client.Controls
{
	public class WinFormDataGrid : WindowsFormsHost
	{
		private ContextMenuStrip menu = new ContextMenuStrip();
		private ToolStripMenuItem сolumnsMenuItem = new ToolStripMenuItem("Столбцы");
		private Dictionary<int, GroupHeader> groupHeader = new Dictionary<int, GroupHeader>();
		private bool calculatedWidth = true;
		private PresentationSource presentationSource;
		private Style style;
		private ResourceDictionary styleresources;

		public WinFormGrid DataGrid = new WinFormGrid();
		public bool IsStyleAppled = true;
		public Type Type { get; set; }
		public ResourceDictionary StyleResources
		{
			get { return styleresources; }
			set
			{
				styleresources = value;
				if (Type != null)
					style = (Style)styleresources[Type.Name + "Row"];
			}
		}
		public string WinFormDataGridName { get { return (this as FrameworkElement).Name; } }

		public static readonly DependencyProperty MyDataSourceProperty = DependencyProperty.Register("MyDataSource", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null && winFormDataGrid.DataGrid.Grid != null)
				{
					winFormDataGrid.groupHeader.Clear();
					if ((winFormDataGrid.GetValue(e.Property) is List<object>))
					{
						for (int i = 0; i < (winFormDataGrid.GetValue(e.Property) as List<object>).Count - 1; i++)
							if ((winFormDataGrid.GetValue(e.Property) as List<object>)[i] is GroupHeader && i + 1 <= (winFormDataGrid.GetValue(e.Property) as List<object>).Count - 1
									&& !((winFormDataGrid.GetValue(e.Property) as List<object>)[i+1] is GroupHeader))
							{
								winFormDataGrid.groupHeader.Add(i, ((winFormDataGrid.GetValue(e.Property) as List<object>)[i] as GroupHeader));
								(winFormDataGrid.GetValue(e.Property) as List<object>)[i] = (winFormDataGrid.GetValue(e.Property) as List<object>)[i + 1];
							}
						
					}
					winFormDataGrid.DataGrid.Grid.DataSource = winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object MyDataSource
		{
			get { return this.GetValue(MyDataSourceProperty); }
			set
			{
				this.SetValue(MyDataSourceProperty, value);
			}
		}

		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null && winFormDataGrid.DataGrid.Grid != null)
				{
					winFormDataGrid.SelectedItem = winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object SelectedItem
		{
			get
			{
				CurrentRowRefresh();
				DataGrid.Grid.Invalidate();
				return this.GetValue(SelectedItemProperty);
			}
			set
			{
				if (DataGrid.Grid.CurrentRow == null || (DataGrid.Grid.CurrentRow != null && !DataGrid.Grid.CurrentRow.DataBoundItem.Equals(value)))
					foreach (DataGridViewRow r in DataGrid.Grid.Rows)
					{
						if (r.DataBoundItem.Equals(value))
						{
							foreach (DataGridViewColumn c in DataGrid.Grid.Columns)
							{
								if (c.Visible)
								{
									DataGrid.Grid.CurrentCell = DataGrid.Grid.Rows[r.Index].Cells[c.Index];
									break;
								}
							}
							break;
						}
					}
				this.SetValue(SelectedItemProperty, value);
			}
		}

		public static readonly DependencyProperty ProducerPromotionsVisibleProperty = DependencyProperty.Register("ProducerPromotionsVisible", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null)
				{
					winFormDataGrid.DataGrid.ProducerPromotions.Visible = (bool)winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object ProducerPromotionsVisible
		{
			get { return this.GetValue(ProducerPromotionsVisibleProperty); }
			set
			{
				this.SetValue(ProducerPromotionsVisibleProperty, value);
			}
		}

		public static readonly DependencyProperty ProducerPromotionsItemsProperty = DependencyProperty.Register("ProducerPromotionsItems", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null)
				{
					winFormDataGrid.DataGrid.ProducerPromotionsItems = winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object ProducerPromotionsItems
		{
			get { return this.GetValue(ProducerPromotionsItemsProperty); }
			set
			{
				this.SetValue(ProducerPromotionsItemsProperty, value);
			}
		}

		public static readonly DependencyProperty PromotionsVisibleProperty = DependencyProperty.Register("PromotionsVisible", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null)
				{
					winFormDataGrid.DataGrid.Promotions.Visible = (bool)winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object PromotionsVisible
		{
			get { return this.GetValue(PromotionsVisibleProperty); }
			set
			{
				this.SetValue(PromotionsVisibleProperty, value);
			}
		}

		public static readonly DependencyProperty PromotionsItemsProperty = DependencyProperty.Register("PromotionsItems", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null)
				{
					winFormDataGrid.DataGrid.PromotionsItems = winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object PromotionsItems
		{
			get { return this.GetValue(PromotionsItemsProperty); }
			set
			{
				this.SetValue(PromotionsItemsProperty, value);
			}
		}

		public static readonly DependencyProperty OrderWarningProperty = DependencyProperty.Register("OrderWarning", typeof(Object), typeof(WinFormDataGrid),
			new PropertyMetadata("", new PropertyChangedCallback((d, e) =>
			{
				var winFormDataGrid = d as WinFormDataGrid;
				if (winFormDataGrid != null)
				{
					winFormDataGrid.DataGrid.OrderWarning = winFormDataGrid.GetValue(e.Property);
				}
			}), null));

		public object OrderWarning
		{
			get { return this.GetValue(OrderWarningProperty); }
			set
			{
				this.SetValue(OrderWarningProperty, value);
			}
		}

		public WinFormDataGrid() : base()
		{
			DataGrid.Grid.BackgroundColor = Color.FromArgb(255, 240, 240, 240);
			DataGrid.Grid.Font = new Font("Segoe UI", 10);
			DataGrid.Grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			DataGrid.Grid.ColumnHeadersHeight = DataGrid.Grid.RowTemplate.Height + 20;
			DataGrid.Grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			presentationSource = Keyboard.PrimaryDevice.ActiveSource;
			DataGrid.Grid.AutoGenerateColumns = false;
			SetDoubleBuffered(DataGrid.Grid);
			DataGrid.Grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			DataGrid.Grid.ReadOnly = true;
			DataGrid.Grid.AllowUserToOrderColumns = true;
			DataGrid.Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			DataGrid.Grid.RowHeadersWidth = 10;
			this.Child = DataGrid;
			InitDataSourceProperty();
			InitSelectedItemProperty();
			InitPromotionsVisibleProperty();
			InitSelectedItemProperty();
			this.DataGrid.SizeChanged += DataGrid_SizeChanged;
			this.DataGrid.Grid.ColumnWidthChanged += DataGrid_ColumnWidthChanged;
			this.DataGrid.Grid.DataBindingComplete += DataGrid_DataBindingComplete;
			this.DataGrid.Grid.Paint += DataGrid_Paint;
			this.DataGrid.Grid.KeyPress += DataGrid_KeyPress;
			this.DataGrid.Grid.CurrentCellChanged += DataGrid_CurrentCellChanged;
			menu.Items.Add(сolumnsMenuItem);
			this.DataGrid.Grid.ContextMenuStrip = menu;
			this.сolumnsMenuItem.Click += ColumnsMenuItem_Click;
			this.DataGrid.Grid.CellFormatting += DataGrid_CellFormatting;
			this.DataGrid.Grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.DataGrid.Grid.PreviewKeyDown += DataGrid_PreviewKeyDown;
			DataGrid.Grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
		}

		public IList<T> GetItems<T>()
		{
			List<T> list = new List<T>();
			foreach (DataGridViewRow item in DataGrid.Grid.Rows)
			{

				if (!groupHeader.Keys.Contains(item.Index)
					&& item.DataBoundItem is T)
					list.Add((T)item.DataBoundItem);
			}
			return list;
		}

		private void DataGrid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyData == Keys.Delete)
				this.RaiseEvent(new System.Windows.Input.KeyEventArgs(
					Keyboard.PrimaryDevice, presentationSource, 0, Key.Delete)
						{ RoutedEvent = Keyboard.KeyDownEvent });
			if (e.KeyData == Keys.F2)
				this.RaiseEvent(new System.Windows.Input.KeyEventArgs(
					Keyboard.PrimaryDevice, presentationSource, 0, Key.F2)
						{ RoutedEvent = Keyboard.KeyDownEvent });
		}

		private void DataGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			ApplyStyle();
		}

		private void DataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (groupHeader.Keys.Contains(e.RowIndex) && e.ColumnIndex == 0)
			{
				e.Value = groupHeader[e.RowIndex].Name;
				e.FormattingApplied = true;
			}
			if (groupHeader.Keys.Contains(e.RowIndex) && e.ColumnIndex > 0 && e.ColumnIndex < DataGrid.Grid.ColumnCount)
			{
				e.Value = string.Empty;
				e.FormattingApplied = true;
			}
		}

		private void ColumnsMenuItem_Click(object sender, EventArgs e)
		{
			var screen = (BaseScreen)this.DataContext;
			if (screen != null)
				ViewModelHelper.ProcessResult(screen.ConfigureGrid(this));
		}

		private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
		{
			GenerateCurrentCellChanged(sender, e);
			DataGrid.Invalidate();
		}

		private void DataGrid_KeyPress(object sender, KeyPressEventArgs e)
		{
			RaiseEvent(new TextCompositionEventArgs(InputManager.Current.PrimaryKeyboardDevice,
				new TextComposition(InputManager.Current, this, e.KeyChar.ToString()))
					{ RoutedEvent = UIElement.TextInputEvent });
			e.Handled = true;
		}

		#region событие ввода MyTextInputEvent
		public delegate void MyTextInputHandler(object sender, TextCompositionEventArgs a);

		public event MyTextInputHandler MyTextInputEvent;

		public void GenerateMyTextInputEvent(object Sender, TextCompositionEventArgs a)
		{
			if (MyTextInputEvent != null)
			{
				MyTextInputEvent(this, a);
			}
		}
		#endregion

		public event EventHandler CurrentCellChanged;

		public void GenerateCurrentCellChanged(object Sender, EventArgs a)
		{
			if (CurrentCellChanged != null)
				CurrentCellChanged(this, a);
		}

		private void DataGrid_Paint(object sender, PaintEventArgs e)
		{
			if (IsStyleAppled)
			{
				ApplyStyle();

			}
		}

		private void InitDataSourceProperty()
		{
			this.DataGrid.Grid.DataSourceChanged += new EventHandler((sender, e) =>
			{
				this.SetValue(MyDataSourceProperty, this.DataGrid.Grid.DataSource);
			});
		}

		private void InitSelectedItemProperty()
		{
			this.DataGrid.Grid.CurrentCellChanged += new EventHandler((sender, e) =>
			{
				if (this.DataGrid.Grid.CurrentRow != null && !groupHeader.Keys.Contains(DataGrid.Grid.CurrentRow.Index))
					this.SetValue(SelectedItemProperty, this.DataGrid.Grid.CurrentRow.DataBoundItem);
			});
		}

		private void InitProducerPromotionsVisibleProperty()
		{
			this.DataGrid.ProducerPromotions.VisibleChanged += new EventHandler((sender, e) =>
			{
				this.SetValue(ProducerPromotionsVisibleProperty, this.DataGrid.ProducerPromotions.Visible);
			});
		}

		private void InitPromotionsVisibleProperty()
		{
			this.DataGrid.Promotions.VisibleChanged += new EventHandler((sender, e) =>
			{
				this.SetValue(PromotionsVisibleProperty, this.DataGrid.Promotions.Visible);
			});
		}

		private void CalcColumnWidthWPF()
		{
			if (!calculatedWidth)
			{
				int x = 0;
				int y = 0;
				foreach (DataGridViewTextBoxColumnEx col in DataGrid.Grid.Columns)
				{
					if (col.Visible)
					{
						y += col.Width;
					}
				}
				if (y != 0)
				{
					x = DataGrid.Width / y;
				}
				foreach (DataGridViewTextBoxColumnEx col in DataGrid.Grid.Columns)
				{
					if (col.Visible)
					{
						col.WidthWPF = col.Width * x;
					}
				}
			}
		}

		private void DataGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			CalcColumnWidthWPF();
		}

		private void CalcColumnWidth()
		{
			calculatedWidth = true;
			int x = 0;
			int y = 0;
			foreach (DataGridViewTextBoxColumnEx col in DataGrid.Grid.Columns)
			{
				if (col.Visible)
				{
					y += col.WidthWPF;
				}
			}
			if (y != 0)
			{
				x = DataGrid.Width / y;
			}
			foreach (DataGridViewTextBoxColumnEx col in DataGrid.Grid.Columns)
			{
				if (col.Visible)
				{
					col.Width = col.WidthWPF * x;
				}
			}
			calculatedWidth = false;
		}

		private void DataGrid_SizeChanged(object sender, EventArgs e)
		{
			CalcColumnWidth();
		}

		private void ApplySetter(SetterBase setter, bool isSelected, bool onlyRow, int rowIndex, int columnIndex)
		{
			{
				if (onlyRow)
				{
					#region Строки
					if (isSelected)
					{
						if (((Setter)setter).Property.Name == "Background")
						{
							DataGrid.Grid.Rows[rowIndex].DefaultCellStyle.SelectionBackColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
						if (((Setter)setter).Property.Name == "Foreground")
						{
							DataGrid.Grid.Rows[rowIndex].DefaultCellStyle.SelectionForeColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
					}
					else
					{
						if (((Setter)setter).Property.Name == "Background")
						{
							DataGrid.Grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
						if (((Setter)setter).Property.Name == "Foreground")
						{
							DataGrid.Grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
					}
					#endregion
				}
				else
				{
					#region Ячейки
					if (isSelected)
					{
						if (((Setter)setter).Property.Name == "Background")
						{
							DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.SelectionBackColor =
								Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
							if (DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.SelectionBackColor != Color.Black)
								DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.SelectionForeColor = Color.Black;
						}
						if (((Setter)setter).Property.Name == "Foreground")
						{
							DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.SelectionForeColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
					}
					else
					{
						if (((Setter)setter).Property.Name == "Background")
						{
							DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.BackColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
						if (((Setter)setter).Property.Name == "Foreground")
						{
							DataGrid.Grid.Rows[rowIndex].Cells[columnIndex].Style.ForeColor = Color.FromArgb(((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.A,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.R,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.G,
								((System.Windows.Media.SolidColorBrush)(((Setter)setter).Value)).Color.B);
						}
					}
					#endregion
				}
			}
		}

		public void ApplyStyle()
		{
			if (Type != null)
				style = (Style)styleresources[Type.Name + "Row"];
			foreach (KeyValuePair<int, GroupHeader> k in groupHeader)
				((DataGridViewTextBoxCellEx)DataGrid.Grid[0, k.Key]).ColumnSpan = DataGrid.Grid.ColumnCount;
			foreach (DataGridViewRow e in DataGrid.Grid.Rows)
			{
				if (groupHeader.Keys.Contains(e.Index))
				{
					DataGrid.Grid.Rows[e.Index].DefaultCellStyle.Font = new System.Drawing.Font(DataGrid.Grid.DefaultCellStyle.Font, System.Drawing.FontStyle.Bold);
					DataGrid.Grid.Rows[e.Index].DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 226, 231, 234);
					DataGrid.Grid.Rows[e.Index].DefaultCellStyle.BackColor = Color.FromArgb(255, 238, 248, 255);
				}
				else
				{
					#region Ячейки
					foreach (DataGridViewColumn column in DataGrid.Grid.Columns)
					{
						Style styleCell;
						string key;
						if (column is DataGridViewTextBoxColumnEx && !String.IsNullOrEmpty(((DataGridViewTextBoxColumnEx)column).PropertyPath))
							key = ((DataGridViewTextBoxColumnEx)column).PropertyPath;
						else
							key = column.DataPropertyName;
						if (String.IsNullOrEmpty(key))
							continue;
						styleCell = StyleResources[Type.Name + key + "Cell"] as Style;
						if (styleCell == null)
							continue;

						foreach (TriggerBase tr in styleCell.Triggers)
						{
							#region MultiDataTrigger
							if (tr is MultiDataTrigger)
							{
								bool IsSelected = false;
								//Проверка условий
								bool flag = true;
								foreach (Condition c in (tr as MultiDataTrigger).Conditions)
								{
									if (((System.Windows.Data.Binding)c.Binding).Path.Path == "(IsSelected)")
									{
										IsSelected = (bool)c.Value;
									}
									//соотвествие свойства условия
									if (((System.Windows.Data.Binding)c.Binding).Path.Path == "(Selector.IsSelectionActive)" && (bool)c.Value)
										flag = false;
									if (!((System.Windows.Data.Binding)c.Binding).Path.Path.StartsWith("(Is"))
									{
										if (Type.GetProperty(((System.Windows.Data.Binding)c.Binding).Path.Path) != null && DataGrid.Grid.Rows[e.Index].DataBoundItem != null
											&& !Type.GetProperty(((System.Windows.Data.Binding)c.Binding).Path.Path).GetValue(DataGrid.Grid.Rows[e.Index].DataBoundItem, new object[] { }).Equals(c.Value))
										{
											flag = false;
										}
									}
								}
								//применение 
								if (flag)
								{
									if (IsSelected)
									{
										foreach (SetterBase setter in (tr as MultiDataTrigger).Setters)
										{

											ApplySetter(setter, IsSelected, false, e.Index, column.Index);
										}
									}
									else
									{
										foreach (SetterBase setter in (tr as MultiDataTrigger).Setters)
										{
											ApplySetter(setter, IsSelected, false, e.Index, column.Index);
										}
									}
								}
							}
							#endregion

							#region DataTrigger
							else if (tr is DataTrigger && DataGrid.Grid.Rows[e.Index].DataBoundItem != null)
							{
								if (Type.GetProperty(((System.Windows.Data.Binding)((DataTrigger)tr).Binding).Path.Path).GetValue(DataGrid.Grid.Rows[e.Index].DataBoundItem, new object[] { }).Equals(((DataTrigger)tr).Value))
								{
									foreach (SetterBase setter in (tr as DataTrigger).Setters)
									{
										ApplySetter(setter, true, false, e.Index, column.Index);
										ApplySetter(setter, false, false, e.Index, column.Index);
									}
								}
							}
							#endregion
						}
					}
					#endregion

					#region Строки
					foreach (TriggerBase tr in style.Triggers)
					{
						#region MultiDataTrigger
						if (tr is MultiDataTrigger)
						{
							bool flag = true;
							bool IsSelected = false;
							//Проверка условий
							foreach (Condition c in (tr as MultiDataTrigger).Conditions)
							{
								if (((System.Windows.Data.Binding)c.Binding).Path.Path == "(IsSelected)")
								{
									IsSelected = (bool)c.Value;
								}
								//соотвествие свойства условия
								if (((System.Windows.Data.Binding)c.Binding).Path.Path != "(IsSelected)" && ((System.Windows.Data.Binding)c.Binding).Path.Path != "(Selector.IsSelectionActive)")
								{
									if (DataGrid.Grid.Rows[e.Index].DataBoundItem != null && !Type.GetProperty(((System.Windows.Data.Binding)c.Binding).Path.Path).GetValue(DataGrid.Grid.Rows[e.Index].DataBoundItem, new object[] { }).Equals(c.Value))
									{
										flag = false;
									}
								}
							}
							//применение 
							if (flag)
							{
								if (IsSelected)
								{
									foreach (SetterBase setter in (tr as MultiDataTrigger).Setters)
									{
										ApplySetter(setter, IsSelected, true, e.Index, 0);
									}
								}
								else
								{
									foreach (SetterBase setter in (tr as MultiDataTrigger).Setters)
									{
										ApplySetter(setter, IsSelected, true, e.Index, 0);
									}
								}
							}
						}
						#endregion
						#region DataTrigger
						else if (tr is DataTrigger && DataGrid.Grid.Rows[e.Index].DataBoundItem != null)
						{
							if (Type.GetProperty(((System.Windows.Data.Binding)((DataTrigger)tr).Binding).Path.Path).GetValue(DataGrid.Grid.Rows[e.Index].DataBoundItem, new object[] { }).Equals(((DataTrigger)tr).Value))
							{
								foreach (SetterBase setter in (tr as DataTrigger).Setters)
								{
									ApplySetter(setter, true, true, e.Index, 0);
									ApplySetter(setter, false, true, e.Index, 0);
								}

							}
						}
						#endregion
					}
					#endregion
				}
			}
			IsStyleAppled = false;
		}

		public static void SetDoubleBuffered(Control control)
		{
			typeof(Control).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, control, new object[] { true });
		}

		public void SaveColumnOrder()
		{
			if (DataGrid.Grid.AllowUserToOrderColumns)
			{
				List<ColumnOrderItem> columnOrder = new List<ColumnOrderItem>();
				DataGridViewColumnCollection columns = DataGrid.Grid.Columns;
				for (int i = 0; i < columns.Count; i++)
				{
					columnOrder.Add(new ColumnOrderItem
					{
						ColumnIndex = i,
						DisplayIndex = columns[i].DisplayIndex,
						Visible = columns[i].Visible,
						Width = ((DataGridViewTextBoxColumnEx)columns[i]).Width
					});
				}
				DataGridViewSetting.Default.ColumnOrder[this.Name] = columnOrder;
				DataGridViewSetting.Default.Save();
			}
		}

		public void SetColumnOrder()
		{
			try
			{
				if (!DataGridViewSetting.Default.ColumnOrder.ContainsKey(this.Name))
					return;

				List<ColumnOrderItem> columnOrder =
					DataGridViewSetting.Default.ColumnOrder[this.Name];

				if (columnOrder != null)
				{
					var sorted = columnOrder.OrderBy(i => i.DisplayIndex);
					foreach (var item in sorted)
					{
						DataGrid.Grid.Columns[item.ColumnIndex].DisplayIndex = item.DisplayIndex;
						DataGrid.Grid.Columns[item.ColumnIndex].Visible = item.Visible;
						((DataGridViewTextBoxColumnEx)DataGrid.Grid.Columns[item.ColumnIndex]).Width = item.Width;
					}
				}
			}
			catch { }
		}

		public void CurrentRowRefresh()
		{
			DataGrid.Grid.CurrentRow.Selected = false;
			DataGrid.Grid.CurrentRow.Selected = true;
		}
	}

	public class DataGridViewTextBoxColumnEx : DataGridViewColumn, INotifyPropertyChanged
	{
		int widthWPF;

		public string PropertyPath { get; set; }
		public int WidthWPF
		{
			get { return widthWPF == 0 ? 1 : widthWPF; }
			set { widthWPF = value; }
		}

		public bool BindVisible
		{
			get { return Visible; }
			set { Visible = value; OnPropertyChanged(); }
		}

		public int BindDisplayIndex
		{
			get { return DisplayIndex; }
			set { DisplayIndex = value; OnPropertyChanged(); }
		}

		public int BindWidth
		{
			get { return Width; }
			set { Width = value; OnPropertyChanged(); }
		}

		public DataGridViewTextBoxColumnEx() : base(new DataGridViewTextBoxCellEx())
		{
		}

		public virtual event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class DataGridViewTextBoxCellEx : DataGridViewTextBoxCell, ISpannedCell
	{
		#region Fields
		private int m_ColumnSpan = 1;
		private int m_RowSpan = 1;
		private DataGridViewTextBoxCellEx m_OwnerCell;
		#endregion

		#region Properties

		public int ColumnSpan
		{
			get { return m_ColumnSpan; }
			set
			{
				if (DataGridView == null || m_OwnerCell != null)
					return;
				if (value < 1 || ColumnIndex + value - 1 >= DataGridView.ColumnCount)
					throw new System.ArgumentOutOfRangeException("value");
				if (m_ColumnSpan != value)
					SetSpan(value, m_RowSpan);
			}
		}

		public int RowSpan
		{
			get { return m_RowSpan; }
			set
			{
				if (DataGridView == null || m_OwnerCell != null)
					return;
				if (value < 1 || RowIndex + value - 1 >= DataGridView.RowCount)
					throw new System.ArgumentOutOfRangeException("value");
				if (m_RowSpan != value)
					SetSpan(m_ColumnSpan, value);
			}
		}

		public DataGridViewCell OwnerCell
		{
			get { return m_OwnerCell; }
			private set { m_OwnerCell = value as DataGridViewTextBoxCellEx; }
		}

		public override bool ReadOnly
		{
			get
			{
				return base.ReadOnly;
			}
			set
			{
				base.ReadOnly = value;

				if (m_OwnerCell == null
					&& (m_ColumnSpan > 1 || m_RowSpan > 1)
					&& DataGridView != null)
				{
					for (int col = ColumnIndex; col < ColumnIndex + m_ColumnSpan; col++)
						for (int row = RowIndex; row < RowIndex + m_RowSpan; row++)
							if (col != ColumnIndex || row != RowIndex)
							{
								DataGridView[col, row].ReadOnly = value;
							}
				}
			}
		}

		#endregion

		#region Painting.

		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			if (m_OwnerCell != null && m_OwnerCell.DataGridView == null)
				m_OwnerCell = null; //owner cell was removed.

			if (DataGridView == null
				|| (m_OwnerCell == null && m_ColumnSpan == 1 && m_RowSpan == 1))
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
						paintParts);
				return;
			}

			var ownerCell = this;
			var columnIndex = ColumnIndex;
			var columnSpan = m_ColumnSpan;
			var rowSpan = m_RowSpan;
			if (m_OwnerCell != null)
			{
				ownerCell = m_OwnerCell;
				columnIndex = m_OwnerCell.ColumnIndex;
				rowIndex = m_OwnerCell.RowIndex;
				columnSpan = m_OwnerCell.ColumnSpan;
				rowSpan = m_OwnerCell.RowSpan;
				value = m_OwnerCell.GetValue(rowIndex);
				errorText = m_OwnerCell.GetErrorText(rowIndex);
				cellState = m_OwnerCell.State;
				cellStyle = m_OwnerCell.GetInheritedStyle(null, rowIndex, true);
				formattedValue = m_OwnerCell.GetFormattedValue(value,
					rowIndex, ref cellStyle, null, null, DataGridViewDataErrorContexts.Display);
			}
			if (CellsRegionContainsSelectedCell(columnIndex, rowIndex, columnSpan, rowSpan))
				cellState |= DataGridViewElementStates.Selected;

			var cellBounds2 = DataGridViewCellExHelper.GetSpannedCellBoundsFromChildCellBounds(
				this,
				cellBounds,
				DataGridViewHelper.SingleVerticalBorderAdded(DataGridView),
				DataGridViewHelper.SingleHorizontalBorderAdded(DataGridView));
			clipBounds = DataGridViewCellExHelper.GetSpannedCellClipBounds(ownerCell, cellBounds2,
				DataGridViewHelper.SingleVerticalBorderAdded(DataGridView),
				DataGridViewHelper.SingleHorizontalBorderAdded(DataGridView));
			//using (var g = DataGridView.CreateGraphics())
			{
				//g.SetClip(clipBounds);
				//Paint the content.
				advancedBorderStyle = DataGridViewCellExHelper.AdjustCellBorderStyle(ownerCell);
				ownerCell.NativePaint(graphics, clipBounds, cellBounds2, rowIndex, cellState,
					value, formattedValue, errorText,
					cellStyle, advancedBorderStyle,
					paintParts & ~DataGridViewPaintParts.Border);
				//Paint the borders.
				if ((paintParts & DataGridViewPaintParts.Border) != DataGridViewPaintParts.None)
				{
					var leftTopCell = ownerCell;
					var advancedBorderStyle2 = new DataGridViewAdvancedBorderStyle
					{
						Left = advancedBorderStyle.Left,
						Top = advancedBorderStyle.Top,
						Right = DataGridViewAdvancedCellBorderStyle.None,
						Bottom = DataGridViewAdvancedCellBorderStyle.None
					};
					leftTopCell.PaintBorder(graphics, clipBounds, cellBounds2, cellStyle, advancedBorderStyle2);

					var rightBottomCell = DataGridView[columnIndex + columnSpan - 1, rowIndex + rowSpan - 1] as DataGridViewTextBoxCellEx
										  ?? this;
					var advancedBorderStyle3 = new DataGridViewAdvancedBorderStyle
					{
						Left = DataGridViewAdvancedCellBorderStyle.None,
						Top = DataGridViewAdvancedCellBorderStyle.None,
						Right = advancedBorderStyle.Right,
						Bottom = advancedBorderStyle.Bottom
					};
					rightBottomCell.PaintBorder(graphics, clipBounds, cellBounds2, cellStyle, advancedBorderStyle3);
				}
			}
		}

		private void NativePaint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
		}

		#endregion
		#region Spanning.

		private void SetSpan(int columnSpan, int rowSpan)
		{
			int prevColumnSpan = m_ColumnSpan;
			int prevRowSpan = m_RowSpan;
			m_ColumnSpan = columnSpan;
			m_RowSpan = rowSpan;

			if (DataGridView != null)
			{
				// clear.
				for (int rowIndex = RowIndex; rowIndex < RowIndex + prevRowSpan; rowIndex++)
					for (int columnIndex = ColumnIndex; columnIndex < ColumnIndex + prevColumnSpan; columnIndex++)
					{
						var cell = DataGridView[columnIndex, rowIndex] as DataGridViewTextBoxCellEx;
						if (cell != null)
							cell.OwnerCell = null;
					}

				// set.
				for (int rowIndex = RowIndex; rowIndex < RowIndex + m_RowSpan; rowIndex++)
					for (int columnIndex = ColumnIndex; columnIndex < ColumnIndex + m_ColumnSpan; columnIndex++)
					{
						var cell = DataGridView[columnIndex, rowIndex] as DataGridViewTextBoxCellEx;
						if (cell != null && cell != this)
						{
							if (cell.ColumnSpan > 1) cell.ColumnSpan = 1;
							if (cell.RowSpan > 1) cell.RowSpan = 1;
							cell.OwnerCell = this;
						}
					}

				OwnerCell = null;
				DataGridView.Invalidate();
			}
		}

		#endregion

		#region Editing.

		public override Rectangle PositionEditingPanel(Rectangle cellBounds, Rectangle cellClip, DataGridViewCellStyle cellStyle, bool singleVerticalBorderAdded, bool singleHorizontalBorderAdded, bool isFirstDisplayedColumn, bool isFirstDisplayedRow)
		{
			if (m_OwnerCell == null
				&& m_ColumnSpan == 1 && m_RowSpan == 1)
			{
				return base.PositionEditingPanel(cellBounds, cellClip, cellStyle, singleVerticalBorderAdded, singleHorizontalBorderAdded, isFirstDisplayedColumn, isFirstDisplayedRow);
			}

			var ownerCell = this;
			if (m_OwnerCell != null)
			{
				var rowIndex = m_OwnerCell.RowIndex;
				cellStyle = m_OwnerCell.GetInheritedStyle(null, rowIndex, true);
				m_OwnerCell.GetFormattedValue(m_OwnerCell.Value, rowIndex, ref cellStyle, null, null, DataGridViewDataErrorContexts.Formatting);
				var editingControl = DataGridView.EditingControl as IDataGridViewEditingControl;
				if (editingControl != null)
				{
					editingControl.ApplyCellStyleToEditingControl(cellStyle);
					var editingPanel = DataGridView.EditingControl.Parent;
					if (editingPanel != null)
						editingPanel.BackColor = cellStyle.BackColor;
				}
				ownerCell = m_OwnerCell;
			}
			cellBounds = DataGridViewCellExHelper.GetSpannedCellBoundsFromChildCellBounds(
				this,
				cellBounds,
				singleVerticalBorderAdded,
				singleHorizontalBorderAdded);
			cellClip = DataGridViewCellExHelper.GetSpannedCellClipBounds(ownerCell, cellBounds, singleVerticalBorderAdded, singleHorizontalBorderAdded);
			return base.PositionEditingPanel(
				 cellBounds, cellClip, cellStyle,
				 singleVerticalBorderAdded,
				 singleHorizontalBorderAdded,
				 DataGridViewCellExHelper.InFirstDisplayedColumn(ownerCell),
				 DataGridViewCellExHelper.InFirstDisplayedRow(ownerCell));
		}

		protected override object GetValue(int rowIndex)
		{
			if (m_OwnerCell != null)
				return m_OwnerCell.GetValue(m_OwnerCell.RowIndex);
			return base.GetValue(rowIndex);
		}

		protected override bool SetValue(int rowIndex, object value)
		{
			if (m_OwnerCell != null)
				return m_OwnerCell.SetValue(m_OwnerCell.RowIndex, value);
			return base.SetValue(rowIndex, value);
		}

		#endregion

		#region Other overridden

		protected override void OnDataGridViewChanged()
		{
			base.OnDataGridViewChanged();

			if (DataGridView == null)
			{
				m_ColumnSpan = 1;
				m_RowSpan = 1;
			}
		}

		protected override Rectangle BorderWidths(DataGridViewAdvancedBorderStyle advancedBorderStyle)
		{
			if (m_OwnerCell == null
				&& m_ColumnSpan == 1 && m_RowSpan == 1)
			{
				return base.BorderWidths(advancedBorderStyle);
			}

			if (m_OwnerCell != null)
				return m_OwnerCell.BorderWidths(advancedBorderStyle);

			var leftTop = base.BorderWidths(advancedBorderStyle);
			var rightBottomCell = DataGridView[
				ColumnIndex + ColumnSpan - 1,
				RowIndex + RowSpan - 1] as DataGridViewTextBoxCellEx;
			var rightBottom = rightBottomCell != null
				? rightBottomCell.NativeBorderWidths(advancedBorderStyle)
				: leftTop;
			return new Rectangle(leftTop.X, leftTop.Y, rightBottom.Width, rightBottom.Height);
		}

		private Rectangle NativeBorderWidths(DataGridViewAdvancedBorderStyle advancedBorderStyle)
		{
			return base.BorderWidths(advancedBorderStyle);
		}

		protected override System.Drawing.Size GetPreferredSize(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex, System.Drawing.Size constraintSize)
		{
			if (OwnerCell != null) return new System.Drawing.Size(0, 0);
			System.Drawing.Size size = base.GetPreferredSize(graphics, cellStyle, rowIndex, constraintSize);
			DataGridView grid = DataGridView;
			int width = size.Width;
			for (int col = ColumnIndex + 1; col < ColumnIndex + ColumnSpan; col++)
				width -= grid.Columns[col].Width;
			int height = size.Height;
			for (int row = RowIndex + 1; row < RowIndex + RowSpan; row++)
				height -= grid.Rows[row].Height;
			return new System.Drawing.Size(width, height);
		}

		#endregion

		#region Private Methods

		private bool CellsRegionContainsSelectedCell(int columnIndex, int rowIndex, int columnSpan, int rowSpan)
		{
			if (DataGridView == null)
				return false;

			for (int col = columnIndex; col < columnIndex + columnSpan; col++)
				for (int row = rowIndex; row < rowIndex + rowSpan; row++)
					if (DataGridView[col, row].Selected) return true;
			return false;
		}

		#endregion
	}

	interface ISpannedCell
	{
		int ColumnSpan { get; }
		int RowSpan { get; }
		DataGridViewCell OwnerCell { get; }
	}

	static class DataGridViewHelper
	{
		public static bool SingleHorizontalBorderAdded(DataGridView dataGridView)
		{
			return !dataGridView.ColumnHeadersVisible &&
				(dataGridView.AdvancedCellBorderStyle.All == DataGridViewAdvancedCellBorderStyle.Single ||
				 dataGridView.CellBorderStyle == DataGridViewCellBorderStyle.SingleHorizontal);
		}

		public static bool SingleVerticalBorderAdded(DataGridView dataGridView)
		{
			return !dataGridView.RowHeadersVisible &&
				(dataGridView.AdvancedCellBorderStyle.All == DataGridViewAdvancedCellBorderStyle.Single ||
				 dataGridView.CellBorderStyle == DataGridViewCellBorderStyle.SingleVertical);
		}
	}

	static class DataGridViewCellExHelper
	{
		public static Rectangle GetSpannedCellClipBounds<TCell>(
			TCell ownerCell,
			Rectangle cellBounds,
			bool singleVerticalBorderAdded,
			bool singleHorizontalBorderAdded)
			where TCell : DataGridViewCell, ISpannedCell
		{
			var dataGridView = ownerCell.DataGridView;
			var clipBounds = cellBounds;
			//Setting X (skip invisible columns).
			for (int columnIndex = ownerCell.ColumnIndex; columnIndex < ownerCell.ColumnIndex + ownerCell.ColumnSpan; columnIndex++)
			{
				DataGridViewColumn column = dataGridView.Columns[columnIndex];
				if (!column.Visible)
					continue;
				if (column.Frozen
					|| columnIndex > dataGridView.FirstDisplayedScrollingColumnIndex)
				{
					break;
				}
				if (columnIndex == dataGridView.FirstDisplayedScrollingColumnIndex)
				{
					clipBounds.Width -= dataGridView.FirstDisplayedScrollingColumnHiddenWidth;
					if (dataGridView.RightToLeft != RightToLeft.Yes)
					{
						clipBounds.X += dataGridView.FirstDisplayedScrollingColumnHiddenWidth;
					}
					break;
				}
				clipBounds.Width -= column.Width;
				if (dataGridView.RightToLeft != RightToLeft.Yes)
				{
					clipBounds.X += column.Width;
				}
			}

			//Setting Y.
			for (int rowIndex = ownerCell.RowIndex; rowIndex < ownerCell.RowIndex + ownerCell.RowSpan; rowIndex++)
			{
				DataGridViewRow row = dataGridView.Rows[rowIndex];
				if (!row.Visible)
					continue;
				if (row.Frozen || rowIndex >= dataGridView.FirstDisplayedScrollingRowIndex)
				{
					break;
				}
				clipBounds.Y += row.Height;
				clipBounds.Height -= row.Height;
			}

			// exclude borders.
			if (dataGridView.BorderStyle != BorderStyle.None)
			{
				var clientRectangle = dataGridView.ClientRectangle;
				clientRectangle.Width--;
				clientRectangle.Height--;
				if (dataGridView.RightToLeft == RightToLeft.Yes)
				{
					clientRectangle.X++;
					clientRectangle.Y++;
				}
				clipBounds.Intersect(clientRectangle);
			}
			return clipBounds;
		}

		public static Rectangle GetSpannedCellBoundsFromChildCellBounds<TCell>(
			TCell childCell,
			Rectangle childCellBounds,
			bool singleVerticalBorderAdded,
			bool singleHorizontalBorderAdded)
			where TCell : DataGridViewCell, ISpannedCell
		{
			var dataGridView = childCell.DataGridView;
			var ownerCell = childCell.OwnerCell as TCell ?? childCell;
			var spannedCellBounds = childCellBounds;
			//
			int firstVisibleColumnIndex = GetFirstVisibleColumnIndex(dataGridView, ownerCell.ColumnIndex,
																	 ownerCell.ColumnSpan);
			if (dataGridView.Columns[firstVisibleColumnIndex].Frozen)
			{
				spannedCellBounds.X = dataGridView.GetColumnDisplayRectangle(firstVisibleColumnIndex, false).X;
			}
			else
			{
				int dx = 0;
				for (int i = firstVisibleColumnIndex; i < childCell.ColumnIndex; i++)
				{
					DataGridViewColumn column = dataGridView.Columns[i];
					if (!column.Visible) continue;
					dx += column.Width;
				}
				spannedCellBounds.X = dataGridView.RightToLeft == RightToLeft.Yes
										  ? spannedCellBounds.X + dx
										  : spannedCellBounds.X - dx;
			}
			//
			var firstVisibleRowIndex = GetFirstVisibleRowIndex(dataGridView, ownerCell.RowIndex, ownerCell.RowSpan);
			if (dataGridView.Rows[firstVisibleRowIndex].Frozen)
			{
				spannedCellBounds.Y = dataGridView.GetRowDisplayRectangle(firstVisibleRowIndex, false).Y;
			}
			else
			{
				int dy = 0;
				for (int i = firstVisibleRowIndex; i < childCell.RowIndex; i++)
				{
					DataGridViewRow row = dataGridView.Rows[i];
					if (!row.Visible) continue;
					dy += row.Height;
				}
				spannedCellBounds.Y -= dy;
			}
			//
			int spannedCellWidth = 0;
			for (int i = ownerCell.ColumnIndex; i < ownerCell.ColumnIndex + ownerCell.ColumnSpan; i++)
			{
				DataGridViewColumn column = dataGridView.Columns[i];
				if (!column.Visible) continue;
				spannedCellWidth += column.Width;
			}

			if (dataGridView.RightToLeft == RightToLeft.Yes)
			{
				spannedCellBounds.X = spannedCellBounds.Right - spannedCellWidth;
			}
			spannedCellBounds.Width = spannedCellWidth;
			//
			int spannedCellHieght = 0;
			for (int i = ownerCell.RowIndex; i < ownerCell.RowIndex + ownerCell.RowSpan; i++)
			{
				DataGridViewRow row = dataGridView.Rows[i];
				if (!row.Visible) continue;
				spannedCellHieght += row.Height;
			}
			spannedCellBounds.Height = spannedCellHieght;

			if (singleVerticalBorderAdded && InFirstDisplayedColumn(ownerCell))
			{
				spannedCellBounds.Width++;
				if (dataGridView.RightToLeft != RightToLeft.Yes)
				{
					if (childCell.ColumnIndex != dataGridView.FirstDisplayedScrollingColumnIndex)
					{
						spannedCellBounds.X--;
					}
				}
				else
				{
					if (childCell.ColumnIndex == dataGridView.FirstDisplayedScrollingColumnIndex)
					{
						spannedCellBounds.X--;
					}
				}
			}
			if (singleHorizontalBorderAdded && InFirstDisplayedRow(ownerCell))
			{
				spannedCellBounds.Height++;
				if (childCell.RowIndex != dataGridView.FirstDisplayedScrollingRowIndex)
				{
					spannedCellBounds.Y--;
				}
			}
			return spannedCellBounds;
		}

		public static DataGridViewAdvancedBorderStyle AdjustCellBorderStyle<TCell>(TCell cell)
			where TCell : DataGridViewCell, ISpannedCell
		{
			var dataGridViewAdvancedBorderStylePlaceholder = new DataGridViewAdvancedBorderStyle();
			var dataGridView = cell.DataGridView;
			return cell.AdjustCellBorderStyle(
				dataGridView.AdvancedCellBorderStyle,
				dataGridViewAdvancedBorderStylePlaceholder,
				DataGridViewHelper.SingleVerticalBorderAdded(dataGridView),
				DataGridViewHelper.SingleHorizontalBorderAdded(dataGridView),
				InFirstDisplayedColumn(cell),
				InFirstDisplayedRow(cell));
		}

		public static bool InFirstDisplayedColumn<TCell>(TCell cell)
			where TCell : DataGridViewCell, ISpannedCell
		{
			var dataGridView = cell.DataGridView;
			return dataGridView.FirstDisplayedScrollingColumnIndex >= cell.ColumnIndex
				   && dataGridView.FirstDisplayedScrollingColumnIndex < cell.ColumnIndex + cell.ColumnSpan;
		}

		public static bool InFirstDisplayedRow<TCell>(TCell cell)
			where TCell : DataGridViewCell, ISpannedCell
		{
			var dataGridView = cell.DataGridView;
			return dataGridView.FirstDisplayedScrollingRowIndex >= cell.RowIndex
				   && dataGridView.FirstDisplayedScrollingRowIndex < cell.RowIndex + cell.RowSpan;
		}


		#region Private Methods

		private static int GetFirstVisibleColumnIndex(DataGridView dataGridView, int startIndex, int span)
		{
			for (int i = startIndex; i < startIndex + span; i++)
			{
				if (dataGridView.Columns[i].Visible)
				{
					return i;
				}
			}
			return -1;
		}

		private static int GetFirstVisibleRowIndex(DataGridView dataGridView, int startIndex, int span)
		{
			for (int i = startIndex; i < startIndex + span; i++)
			{
				if (dataGridView.Rows[i].Visible)
				{
					return i;
				}
			}
			return -1;
		}

		#endregion
	}

	public class DataGridViewSetting : ApplicationSettingsBase
	{
		private static DataGridViewSetting _defaultInstace =
			(DataGridViewSetting)ApplicationSettingsBase.Synchronized(new DataGridViewSetting());

		public static DataGridViewSetting Default
		{
			get { return _defaultInstace; }
		}

		[UserScopedSetting]
		[SettingsSerializeAs(SettingsSerializeAs.Binary)]
		[DefaultSettingValue("")]

		public Dictionary<string, List<ColumnOrderItem>> ColumnOrder
		{
			get { return this["ColumnOrder"] as Dictionary<string, List<ColumnOrderItem>>; }
			set { this["ColumnOrder"] = value; }
		}
	}

	[Serializable]
	public class ColumnOrderItem 
	{
		public int DisplayIndex
		{
			get; set;
		}
		public int Width
		{
			get; set;
		}
		public int WidthWPF
		{
			get; set;
		}
		public bool Visible
		{
			get; set;
		}
		public int ColumnIndex
		{
			get; set;
		}
	}
	
}
