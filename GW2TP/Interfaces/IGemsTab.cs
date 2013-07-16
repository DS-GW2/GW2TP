using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GW2TP
{
    public interface IGemsTab
    {
        string GemCostPrefixLabel { get; set; }
        Label GemCostLabel { get; }
        string GoldWorthPrefixLabel { get; set; }
        Label GoldWorthLabel { get; }
        Label GoldToUSDLabel { get; }
        string NotifyIconText { get; set; }
        Settings GW2TPSettings { get; }
        void Alert(string title, string msg);
    }
}
