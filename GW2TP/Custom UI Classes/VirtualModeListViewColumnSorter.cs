using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Windows.Forms;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2TP
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    class VirtualModeListViewColumnSorter : IComparer<Item>
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;

        private ColumnInfo[] Columns;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public VirtualModeListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two items passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First Item to be compared</param>
        /// <param name="y">Second Item to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(Item x, Item y)
        {
            if (OrderOfSort == SortOrder.None) return 0;

            int compareResult = 0;

            if (Columns[ColumnToSort].ColumnValueFunction != null)
            {
                compareResult = Compare(Columns[ColumnToSort].ColumnValueFunction(x), Columns[ColumnToSort].ColumnValueFunction(y));
            }

            return compareResult;
        }

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public VirtualModeListViewColumnSorter(int column, SortOrder order)
        {
            // Initialize the column
            ColumnToSort = column;

            // Initialize the sort order
            OrderOfSort = order;
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }

        public ColumnInfo[] ColumnInfo
        {
            set
            {
                Columns = value;
            }
            get
            {
                return Columns;
            }
        }

        /// <summary>
        /// This method compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        private int Compare(object x, object y)
        {
            if (OrderOfSort == SortOrder.None) return 0;

            int compareResult = 0;

            //Determine whether the type being compared is a date type.
            //try
            //{
            //    // Parse the two objects passed as a parameter as a DateTime.
            //    System.DateTime firstDate =
            //            DateTime.Parse(listviewX.SubItems[ColumnToSort].Text);
            //    System.DateTime secondDate =
            //            DateTime.Parse(listviewY.SubItems[ColumnToSort].Text);
            //    // Compare the two dates.
            //    compareResult = DateTime.Compare(firstDate, secondDate);
            //}
            // If neither compared object has a valid int format, compare
            // as a string.
            //catch
            //{                
            // Compare the two items as a string

            if (x is int)
            {
                int numX = (int)x, numY = (int)y;
                compareResult = (numX == numY ? 0 : (numX > numY ? 1 : -1));
            }
            else if (x is double)
            {
                double numX = (double)x, numY = (double)y;
                compareResult = (numX == numY ? 0 : (numX > numY ? 1 : -1));
            }
            else if (x is string)
            {
                string stringX = (string)x, stringY = (string)y;
                compareResult = String.Compare(stringX, stringY, true);
            }
            else if (x is TimeSpan)
            {
                TimeSpan tsX = (TimeSpan)x, tsY = (TimeSpan)y;
                compareResult = (tsX == tsY ? 0 : (tsX > tsY ? 1 : -1));
            }
            else
            {
                return 0;
            }
            //}

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        ///// <summary>
        ///// This method compares the two items passed using a case insensitive comparison.
        ///// </summary>
        ///// <param name="x">First Item to be compared</param>
        ///// <param name="y">Second Item to be compared</param>
        ///// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        //private int Compare(ListViewItem.ListViewSubItem listviewX, ListViewItem.ListViewSubItem listviewY)
        //{
        //    if (OrderOfSort == SortOrder.None) return 0;

        //    int compareResult;

        //    //Determine whether the type being compared is a date type.
        //    //try
        //    //{
        //    //    // Parse the two objects passed as a parameter as a DateTime.
        //    //    System.DateTime firstDate =
        //    //            DateTime.Parse(listviewX.SubItems[ColumnToSort].Text);
        //    //    System.DateTime secondDate =
        //    //            DateTime.Parse(listviewY.SubItems[ColumnToSort].Text);
        //    //    // Compare the two dates.
        //    //    compareResult = DateTime.Compare(firstDate, secondDate);
        //    //}
        //    // If neither compared object has a valid int format, compare
        //    // as a string.
        //    //catch
        //    //{                
        //    // Compare the two items as a string
        //    int numX, numY;
        //    string firstX = listviewX.Text.Split(' ')[0];
        //    string firstY = listviewY.Text.Split(' ')[0];

        //    if (int.TryParse(firstX, out numX) && int.TryParse(firstY, out numY))
        //    {
        //        compareResult = (numX == numY ? 0 : (numX > numY ? 1 : -1));
        //    }
        //    else
        //    {
        //        compareResult = String.Compare(listviewX.Text, listviewY.Text, true);
        //    }
        //    //}

        //    // Calculate correct return value based on object comparison
        //    if (OrderOfSort == SortOrder.Ascending)
        //    {
        //        // Ascending sort is selected, return normal result of compare operation
        //        return compareResult;
        //    }
        //    else if (OrderOfSort == SortOrder.Descending)
        //    {
        //        // Descending sort is selected, return negative result of compare operation
        //        return (-compareResult);
        //    }
        //    else
        //    {
        //        // Return '0' to indicate they are equal
        //        return 0;
        //    }
        //}
    }
}
