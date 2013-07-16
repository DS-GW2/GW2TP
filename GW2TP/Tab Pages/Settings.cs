using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

namespace GW2TP
{
    public class Settings
    {
        const double TIMER_INTERVAL = 900000.0;

        public double timeInterval = TIMER_INTERVAL;
        public int maxGoldToGem = 12500;
        public int minGemToGold = 20000;
        public bool goldToGemAlert = false, gemToGoldAlert = false;
        public string logFilePath;
        private ISettingsTab SettingsTab;

        public Settings(ISettingsTab mainForm)
        {
            this.SettingsTab = mainForm;

            // Setup log file path
            String path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AssemblyName asmName = Assembly.GetEntryAssembly().GetName(); // name of the main executing project
            path = System.IO.Path.Combine(path, asmName.Name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            logFilePath = path + "\\" + Globals.APPNAME + ".Log";

            this.SettingsTab.LogFilePathTB = this.logFilePath;
            this.SettingsTab.LogFilePathBtn.Click += LogFilePathBtn_Click;
        }

        public void Load()
        {
            double timeInterval = this.timeInterval / 60000;
            this.SettingsTab.TimerIntervalTB = timeInterval.ToString();

            this.SettingsTab.GoldToGemCB = this.goldToGemAlert;
            this.SettingsTab.GemToGoldCB = this.gemToGoldAlert;

            int maxGoldToGem = this.maxGoldToGem;
            this.SettingsTab.GoldToGemTB = maxGoldToGem.ToString();

            int minGemToGold = this.minGemToGold;
            this.SettingsTab.GemToGoldTB = minGemToGold.ToString();
        }

        public void Save()
        {
            bool dirty = false;

            double timerInterval;
            if (Double.TryParse(this.SettingsTab.TimerIntervalTB, out timerInterval) && (timerInterval * 60000) != this.timeInterval)
            {
                dirty = true;
                this.timeInterval = timerInterval * 60000;
                SettingsTab.ResetTimer();
            }

            if (this.SettingsTab.GoldToGemCB != this.goldToGemAlert)
            {
                dirty = true;
                this.goldToGemAlert = this.SettingsTab.GoldToGemCB;
            }

            if (this.SettingsTab.GemToGoldCB != this.gemToGoldAlert)
            {
                dirty = true;
                this.gemToGoldAlert = this.SettingsTab.GemToGoldCB;
            }

            int maxGoldToGemPrice;
            if (Int32.TryParse(SettingsTab.GoldToGemTB, out maxGoldToGemPrice) && maxGoldToGemPrice != this.maxGoldToGem)
            {
                dirty = true;
                this.maxGoldToGem = maxGoldToGemPrice;
            }

            int minGemToGoldPrice;
            if (Int32.TryParse(SettingsTab.GemToGoldTB, out minGemToGoldPrice) && minGemToGoldPrice != this.minGemToGold)
            {
                dirty = true;
                this.minGemToGold = minGemToGoldPrice;
            }

            if (String.Compare(SettingsTab.LogFilePathTB, this.logFilePath, true) != 0)
            {
                dirty = true;
                this.logFilePath = SettingsTab.LogFilePathTB;
                SettingsTab.ReOpenLogFile();
            }

            if (dirty)
            {
                SettingsTab.SaveConfig();
            }
        }

        private void LogFilePathBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = false;
            ofd.CheckPathExists = true;
            ofd.InitialDirectory = this.SettingsTab.LogFilePathTB;
            ofd.Filter = "Log files (*.log)|*.log|All files (*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "Select Log File...";
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.SettingsTab.LogFilePathTB = ofd.FileName;
            }
        }
    }
}
