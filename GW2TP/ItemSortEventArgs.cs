using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2TP
{
    public class ItemSortEventArgs : EventArgs
    {
        string orderBy;
        bool sortDescending;
        int column;

        public ItemSortEventArgs()
        {
            this.orderBy = string.Empty;
            this.sortDescending = false;
        }

        public ItemSortEventArgs(string orderBy, bool sortDescending, int column)
        {
            this.orderBy = orderBy;
            this.sortDescending = sortDescending;
            this.column = column;
        }

        public string OrderBy
        {
            get
            {
                return this.orderBy;
            }
            set
            {
                this.orderBy = value;
            }
        }

        public bool SortDescending
        {
            get
            {
                return this.sortDescending;
            }
            set
            {
                this.sortDescending = value;
            }
        }

        public int Column
        {
            get
            {
                return this.column;
            }
            set
            {
                this.column = value;
            }
        }
    }
}
