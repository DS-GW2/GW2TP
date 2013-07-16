using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ListViewEmbeddedControls;

namespace GW2TP
{
    public interface ITransaction
    {
        ListViewEx BuyListViewLV { get; }
        ListViewEx SellListViewLV { get; }
        ListViewEx SoldListViewLV { get; }
        ListViewEx BoughtListViewLV { get; }
        StatusStrip BuyStatusStripSS { get; }
        StatusStrip SellStatusStripSS { get; }
        StatusStrip SoldStatusStripSS { get; }
        StatusStrip BoughtStatusStripSS { get; }
        void Alert(string title, string msg);
    }
}
