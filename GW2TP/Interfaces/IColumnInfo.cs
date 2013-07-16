using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ListViewEmbeddedControls;

namespace GW2TP
{
    public interface IColumnInfo
    {
        ListViewEx ListViewLV { get; }
        void Alert(string title, string msg);
    }
}
