using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ListViewEmbeddedControls
{
	/// <summary>
	/// Zusammenfassung für ListViewEx.
	/// </summary>
	public class ListViewEx : ListView
	{
		#region Interop-Defines
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private	static extern IntPtr SendMessage(IntPtr hWnd, int msg,	IntPtr wPar, IntPtr	lPar);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, ref HDITEM lParam);

		// ListView messages
		private const int LVM_FIRST					= 0x1000;
        private const int LVM_GETHEADER = (LVM_FIRST + 31);
        private const int LVM_SETCOLUMNORDERARRAY = (LVM_FIRST + 58);
		private const int LVM_GETCOLUMNORDERARRAY	= (LVM_FIRST + 59);

        private const int HDM_FIRST = 0x1200;
        private const int HDM_GETITEM = HDM_FIRST + 11;
        private const int HDM_SETITEM = HDM_FIRST + 12;
		
		// Windows Messages
		private const int WM_PAINT = 0x000F;
        private const int WM_LBUTTONDBLCLK = 0x203;

        [StructLayout(LayoutKind.Sequential)]
        public struct HDITEM
        {
            public Mask mask;
            public int cxy;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public Format fmt;
            public IntPtr lParam;
            // _WIN32_IE >= 0x0300 
            public int iImage;
            public int iOrder;
            // _WIN32_IE >= 0x0500
            public uint type;
            public IntPtr pvFilter;
            // _WIN32_WINNT >= 0x0600
            public uint state;

            [Flags]
            public enum Mask
            {
                Format = 0x4,       // HDI_FORMAT
            };

            [Flags]
            public enum Format
            {
                SortDown = 0x200,   // HDF_SORTDOWN
                SortUp = 0x400,     // HDF_SORTUP
            };
        };
        #endregion

        /// <summary>
		/// Structure to hold an embedded control's info
		/// </summary>
		private struct EmbeddedControl
		{
			public Control Control;
			public int Column;
			public int Row;
			public DockStyle Dock;
			public ListViewItem Item;
		}

		private ArrayList _embeddedControls = new ArrayList();

        public ListViewEx() { this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);  }

        public void SetSortIcon(int columnIndex, SortOrder order)
        {
            IntPtr columnHeader = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            for (int columnNumber = 0; columnNumber <= Columns.Count - 1; columnNumber++)
            {
                var columnPtr = new IntPtr(columnNumber);
                var item = new HDITEM
                {
                    mask = HDITEM.Mask.Format
                };

                if (SendMessage(columnHeader, HDM_GETITEM, columnPtr, ref item) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                if (order != SortOrder.None && columnNumber == columnIndex)
                {
                    switch (order)
                    {
                        case SortOrder.Ascending:
                            item.fmt &= ~HDITEM.Format.SortDown;
                            item.fmt |= HDITEM.Format.SortUp;
                            break;
                        case SortOrder.Descending:
                            item.fmt &= ~HDITEM.Format.SortUp;
                            item.fmt |= HDITEM.Format.SortDown;
                            break;
                    }
                }
                else
                {
                    item.fmt &= ~HDITEM.Format.SortDown & ~HDITEM.Format.SortUp;
                }

                if (SendMessage(columnHeader, HDM_SETITEM, columnPtr, ref item) == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }
        }

		/// <summary>
		/// Retrieve the order in which columns appear
		/// </summary>
		/// <returns>Current display order of column indices</returns>
		public int[] GetColumnOrder()
		{
			IntPtr lPar	= Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * Columns.Count);

			IntPtr res = SendMessage(Handle, LVM_GETCOLUMNORDERARRAY, new IntPtr(Columns.Count), lPar);
			if (res.ToInt32() == 0)	// Something went wrong
			{
				Marshal.FreeHGlobal(lPar);
				return null;
			}

			int	[] order = new int[Columns.Count];
			Marshal.Copy(lPar, order, 0, Columns.Count);

			Marshal.FreeHGlobal(lPar);

			return order;
		}

        public void SetColumnOrder(int[] SortOrder)
		{
			if( SortOrder.Length < 1 ) return;
			if( SortOrder.Length > Columns.Count) return;

            GCHandle pinnedArray = GCHandle.Alloc(SortOrder, GCHandleType.Pinned);
            IntPtr lPar = pinnedArray.AddrOfPinnedObject();

            IntPtr res = SendMessage(Handle, LVM_SETCOLUMNORDERARRAY, new IntPtr(SortOrder.Length), lPar);
            if (res.ToInt32() != 0) 
			{
				this.Refresh();
			}
            pinnedArray.Free();
		}

		/// <summary>
		/// Retrieve the bounds of a ListViewSubItem
		/// </summary>
		/// <param name="Item">The Item containing the SubItem</param>
		/// <param name="SubItem">Index of the SubItem</param>
		/// <returns>Subitem's bounds</returns>
		protected Rectangle GetSubItemBounds(ListViewItem Item, int SubItem)
		{
			Rectangle subItemRect = Rectangle.Empty;

			if (Item == null)
				throw new ArgumentNullException("Item");

			int[] order = GetColumnOrder();
			if (order == null) // No Columns
				return subItemRect;

			if (SubItem >= order.Length)
				throw new IndexOutOfRangeException("SubItem "+SubItem+" out of range");

			// Retrieve the bounds of the entire ListViewItem (all subitems)
			Rectangle lviBounds = Item.GetBounds(ItemBoundsPortion.Entire);
			int	subItemX = lviBounds.Left;

			// Calculate the X position of the SubItem.
			// Because the columns can be reordered we have to use Columns[order[i]] instead of Columns[i] !
			ColumnHeader col;
			int i;
			for (i=0; i<order.Length; i++)
			{
				col = this.Columns[order[i]];
				if (col.Index == SubItem)
					break;
				subItemX += col.Width;
			}
 
			subItemRect	= new Rectangle(subItemX, lviBounds.Top, this.Columns[order[i]].Width, lviBounds.Height);

			return subItemRect;
		}

		/// <summary>
		/// Add a control to the ListView
		/// </summary>
		/// <param name="c">Control to be added</param>
		/// <param name="col">Index of column</param>
		/// <param name="row">Index of row</param>
		public void AddEmbeddedControl(Control c, int col, int row)
		{
			AddEmbeddedControl(c,col,row,DockStyle.Fill);
		}
		/// <summary>
		/// Add a control to the ListView
		/// </summary>
		/// <param name="c">Control to be added</param>
		/// <param name="col">Index of column</param>
		/// <param name="row">Index of row</param>
		/// <param name="dock">Location and resize behavior of embedded control</param>
		public void AddEmbeddedControl(Control c, int col, int row, DockStyle dock)
		{
			if (c==null)
				throw new ArgumentNullException();
			if (col>=Columns.Count || row>=Items.Count)
				throw new ArgumentOutOfRangeException();

			EmbeddedControl ec;
			ec.Control = c;
			ec.Column = col;
			ec.Row = row;
			ec.Dock = dock;
			ec.Item = Items[row];

			_embeddedControls.Add(ec);

			// Add a Click event handler to select the ListView row when an embedded control is clicked
			c.Click += new EventHandler(_embeddedControl_Click);
			
			this.Controls.Add(c);
		}
		
		/// <summary>
		/// Remove a control from the ListView
		/// </summary>
		/// <param name="c">Control to be removed</param>
		public void RemoveEmbeddedControl(Control c)
		{
			if (c == null)
				throw new ArgumentNullException();

			for (int i=0; i<_embeddedControls.Count; i++)
			{
				EmbeddedControl ec = (EmbeddedControl)_embeddedControls[i];
				if (ec.Control == c)
				{
					c.Click -= new EventHandler(_embeddedControl_Click);
					this.Controls.Remove(c);
					_embeddedControls.RemoveAt(i);
					return;
				}
			}
			throw new Exception("Control not found!");
		}

        public ListViewItem GetListViewItem(Control control)
        {
            foreach (EmbeddedControl ec in _embeddedControls)
            {
                if (ec.Control == control)
                {
                    return ec.Item;
                }
            }

            return null;
        }
		
		/// <summary>
		/// Retrieve the control embedded at a given location
		/// </summary>
		/// <param name="col">Index of Column</param>
		/// <param name="row">Index of Row</param>
		/// <returns>Control found at given location or null if none assigned.</returns>
		public Control GetEmbeddedControl(int col, int row)
		{
			foreach (EmbeddedControl ec in _embeddedControls)
				if (ec.Row == row && ec.Column == col)
					return ec.Control;

			return null;
		}

		[DefaultValue(View.LargeIcon)]
		public new View View
		{
			get 
			{
				return base.View;
			}
			set
			{
				// Embedded controls are rendered only when we're in Details mode
				foreach (EmbeddedControl ec in _embeddedControls)
					ec.Control.Visible = (value == View.Details);

				base.View = value;
			}
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
                //case WM_LBUTTONDBLCLK:
                //    Int16 x = (Int16)m.LParam;
                //    Int16 y = (Int16)((int)m.LParam >> 16);
 
                //    MouseEventArgs e =
                //        new MouseEventArgs(MouseButtons.Left, 2, x, y, 0);
               
                //    this.OnMouseDoubleClick(e);
                //    break;
				case WM_PAINT:
					if (View != View.Details)
						break;

					// Calculate the position of all embedded controls
					foreach (EmbeddedControl ec in _embeddedControls)
					{
						Rectangle rc = this.GetSubItemBounds(ec.Item, ec.Column);

						if ((this.HeaderStyle != ColumnHeaderStyle.None) &&
							(rc.Top<this.Font.Height)) // Control overlaps ColumnHeader
						{
							ec.Control.Visible = false;
							continue;
						}
						else
						{
							ec.Control.Visible = true;
						}

						switch (ec.Dock)
						{
							case DockStyle.Fill:
								break;
							case DockStyle.Top:
								rc.Height = ec.Control.Height;
								break;
							case DockStyle.Left:
								rc.Width = ec.Control.Width;
								break;
							case DockStyle.Bottom:
								rc.Offset(0, rc.Height-ec.Control.Height);
								rc.Height = ec.Control.Height;
								break;
							case DockStyle.Right:
								rc.Offset(rc.Width-ec.Control.Width, 0);
								rc.Width = ec.Control.Width;
								break;
							case DockStyle.None:
								rc.Size = ec.Control.Size;
								break;
						}

						// Set embedded control's bounds
						ec.Control.Bounds = rc;
					}
					break;
			}
			base.WndProc (ref m);
		}

		private void _embeddedControl_Click(object sender, EventArgs e)
		{
			// When a control is clicked the ListViewItem holding it is selected
			foreach (EmbeddedControl ec in _embeddedControls)
			{
				if (ec.Control == (Control)sender)
				{
					this.SelectedItems.Clear();
					ec.Item.Selected = true;
				}
			}
		}
	}
}
