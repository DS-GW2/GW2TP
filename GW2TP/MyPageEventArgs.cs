using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2TP
{
    public class MyPageEventArgs
    {
        public MyPageEventArgs(int startRowIndex, int maximumRows, int totalRowCount)
        {
            this.StartRowIndex = startRowIndex;
            this.MaxiumRows = maximumRows;
            this.TotalRowCount = totalRowCount;
        }

        public int MaxiumRows { get; set; }
        public int StartRowIndex { get; set; }
        public int TotalRowCount { get; set; }
    }
}
