using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ListViewEmbeddedControls;
using ExpanderApp;

namespace GW2TP
{
    public interface ISearchTab
    {
        ComboBox SearchCB { get; }

        Button SearchBtn { get; }

        ComboBox SearchCategoryCB { get; }

        ComboBox SearchSubcategoryCB { get; }

        ComboBox SearchRarityCB { get; }

        ComboBox SearchArmorWeightCB { get; }

        string SearchMinLevelTB { get; set; }

        string SearchMaxLevelTB { get; set; }

        ListViewEx SearchListViewLV { get; }

        Expander SearchExpanderEx { get; }

        StatusStrip SearchStatusStripSS { get; }
    }
}
