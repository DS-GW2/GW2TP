using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GW2TP
{
    public interface ISettingsTab
    {
        string TimerIntervalTB { get; set; }

        string GoldToGemTB { get; set; }

        string GemToGoldTB { get; set; }

        bool GoldToGemCB { get; set; }

        bool GemToGoldCB { get; set; }

        string LogFilePathTB { get; set; }

        Button LogFilePathBtn { get; }

        void ResetTimer();

        void SaveConfig();

        void ReOpenLogFile();
    }
}
