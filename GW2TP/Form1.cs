using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Threading;
using System.Configuration;
using System.Speech.Synthesis;
using GW2Miner.Engine;
using GW2Miner.Domain;
using CustomUIControls;
using ListViewEmbeddedControls;
using ExpanderApp;
using System.Reflection;
using System.Diagnostics;

namespace GW2TP
{
    public partial class GW2TP : Form, ISettingsTab, ISearchTab, IGemsTab, ITransaction
    {
        private TradeWorker trader = new TradeWorker();
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); // potential exception if config file cannot be loaded!
        private Logger fileLogger = Logger.Instance;
        private SpeechSynthesizer reader = new SpeechSynthesizer();
        private LoginInstructions instructionsDlg;
        private TaskbarNotifier taskbarNotifier;
        private System.Timers.Timer taskTimer = new System.Timers.Timer();

        private Gems gemsDisplay;
        private Settings settings;
        private Transactions sellTransactions;
        private Transactions buyTransactions;
        private Transactions boughtTransactions;
        private Transactions soldTransactions;

        private Search search;

        private Object classLock = typeof(GW2TP);

        public GW2TP()
        {
            InitializeComponent();
            this.SearchExpander.Content = this.SearchFilterPanel;            
            this.settings = new Settings(this);

            LoadConfig();

            this.search = new Search(this);

            this.gemsDisplay = new Gems(this);
            this.buyTransactions = new Transactions(this, true, false);
            this.sellTransactions = new Transactions(this, false, false);
            this.boughtTransactions = new Transactions(this, true, true);
            this.soldTransactions = new Transactions(this, false, true);

            UpdateStatus("Search");

            taskbarNotifier = new TaskbarNotifier();
            taskbarNotifier.SetBackgroundBitmap(new Bitmap(Assembly.GetEntryAssembly().GetManifestResourceStream("GW2TP.Resources.skin.bmp")), Color.FromArgb(255, 0, 255));
            taskbarNotifier.SetCloseBitmap(new Bitmap(Assembly.GetEntryAssembly().GetManifestResourceStream("GW2TP.Resources.close.bmp")), Color.FromArgb(255, 0, 255), new Point(127, 8));
            taskbarNotifier.TitleRectangle = new Rectangle(40, 9, 70, 25);
            taskbarNotifier.ContentRectangle = new Rectangle(8, 41, 133, 68);

            //this.AcceptButton = this.SearchButton;

            this.fileLogger.Open(this.settings.logFilePath, false);
            this.fileLogger.CreateEntry(Globals.APPNAME + " Started!");
        }

        public void checkMail()
        {
            try
            {
                //Set a working icon
                this.notifyIcon1.Icon = new Icon(GetType(), "workingIcon.ico");
                //... do some checking here...

                //if you have new mail
                this.notifyIcon1.Icon = new Icon(GetType(), "newmailIcon.ico");
                //else reset the icon
                this.notifyIcon1.Icon = new Icon(GetType(), "normalIcon.ico");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void CleanUp()
        {
            StopTaskTimer();
            if (this.taskTimer != null)
            {
                this.taskTimer.Dispose();
                this.taskTimer = null;
            }
            trader.FnLoginInstructions -= OnOpenLoginInstructions;
            SaveConfig();
            if (this.fileLogger != null)
            {
                this.fileLogger.CreateEntry(Globals.APPNAME + " Closing!");
                this.fileLogger.Close();
                this.fileLogger = null;
            }
        }

        public void UpdateStatus(string update)
        {
            this.Text = String.Format("{0} - {1}", Globals.APPNAME, update);
        }

        private void LoadConfig()
        {
            double timerInterval;
            if (_config.AppSettings.Settings["TimerInterval"] != null && Double.TryParse(_config.AppSettings.Settings["TimerInterval"].Value, out timerInterval))
                this.settings.timeInterval = timerInterval;

            int maxGoldToGemPrice;
            if (_config.AppSettings.Settings["MaxGoldToGemPrice"] != null && Int32.TryParse(_config.AppSettings.Settings["MaxGoldToGemPrice"].Value, out maxGoldToGemPrice))
                this.settings.maxGoldToGem = maxGoldToGemPrice;

            bool goldToGemAlertSetting;
            if (_config.AppSettings.Settings["GoldToGemAlert"] != null && Boolean.TryParse(_config.AppSettings.Settings["GoldToGemAlert"].Value, out goldToGemAlertSetting))
                this.settings.goldToGemAlert = goldToGemAlertSetting;

            int minGemToGoldPrice;
            if (_config.AppSettings.Settings["MinGemToGoldPrice"] != null && Int32.TryParse(_config.AppSettings.Settings["MinGemToGoldPrice"].Value, out minGemToGoldPrice))
                this.settings.minGemToGold = minGemToGoldPrice;

            bool gemToGoldAlertSetting;
            if (_config.AppSettings.Settings["GemToGoldAlert"] != null && Boolean.TryParse(_config.AppSettings.Settings["GemToGoldAlert"].Value, out gemToGoldAlertSetting))
                this.settings.gemToGoldAlert = gemToGoldAlertSetting;

            if (_config.AppSettings.Settings["LogFilePath"] != null)
                this.settings.logFilePath = _config.AppSettings.Settings["LogFilePath"].Value;

            // Set window Location
            if (Properties.Settings.Default.WindowLocation != null)
            {
                this.Location = Properties.Settings.Default.WindowLocation;
            }

            // Set window size
            if (Properties.Settings.Default.WindowSize != null)
            {
                this.Size = Properties.Settings.Default.WindowSize;
            }
        }

        #region Start Stop Timer
        private void StartTaskTimer()
        {
            ThreadPool.QueueUserWorkItem((obj) => DoWork());
            this.taskTimer.Interval = this.settings.timeInterval;
            this.taskTimer.Elapsed += taskTimer_Elapsed;
            this.taskTimer.Enabled = true;
            this.taskTimer.Start();
        }

        private void StopTaskTimer()
        {
            if (this.taskTimer != null && this.taskTimer.Enabled)
            {
                this.taskTimer.Stop();
                this.taskTimer.Enabled = false;
                this.taskTimer.Elapsed -= taskTimer_Elapsed;
            }
        }

        private void taskTimer_Elapsed(Object source, ElapsedEventArgs e)
        {
            DoWork();
        }

        private void DoWork()
        {
            lock (classLock)
            {
                try
                {
                    this.gemsDisplay.Update();
                }
                catch (Exception e)
                {
                    fileLogger.CreateEntry("Error calling gemDisplay.Update");
                    logException(e);
                }

                try
                {
                    // first transaction to do a true update
                    this.buyTransactions.Update(true);
                }
                catch (Exception e)
                {
                    fileLogger.CreateEntry("Error calling buyTransactions.Update");
                    logException(e);
                }

                try
                {
                    this.sellTransactions.Update();
                }
                catch (Exception e)
                {
                    fileLogger.CreateEntry("Error calling sellTransactions.Update");
                    logException(e);
                }

                try
                {
                    this.boughtTransactions.Update();
                }
                catch (Exception e)
                {
                    fileLogger.CreateEntry("Error calling boughtTransactions.Update");
                    logException(e);
                }

                try
                {
                    this.soldTransactions.Update();
                }
                catch (Exception e)
                {
                    fileLogger.CreateEntry("Error calling soldTransactions.Update");
                    logException(e);
                }
            }
        }

        private void logException(Exception e)
        {
            string errorMsg = e.ToString();
            this.fileLogger.CreateEntry(errorMsg);
        }
        #endregion

        #region Tab Select/Deselect
        private void Tabs_Selected(object sender, TabControlEventArgs e)
        {
            switch(e.TabPageIndex)
            {
                case 0:
                    UpdateStatus("Search");
                    //this.AcceptButton = this.SearchButton;
                    break;

                case 1:
                    UpdateStatus("Gems");
                    break;

                case 2:
                    UpdateStatus("Buy Transactions");
                    break;

                case 3:
                    UpdateStatus("Sell Transactions");
                    break;

                case 4:
                    UpdateStatus("Bought Transactions");
                    break;

                case 5:
                    UpdateStatus("Sold Transactions");
                    //ThreadPool.QueueUserWorkItem((obj) => this.soldTransactions.Update());
                    break;

                case 6:
                    UpdateStatus("Settings");
                    settings.Load();
                    break;

                case 7:
                    UpdateStatus("About");
                    break;
            }
        }

        private void Tabs_Deselected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == SettingsTabPage)
            {
                settings.Save();
            }
            //else if (e.TabPage == SearchTabPage)
            //{
            //    this.AcceptButton = null;
            //}
        }
        #endregion

        #region Session Key Instructions CallBacks
        private void OnOpenLoginInstructions(object sender, EventArgs e)
        {
            Globals.gettingSessionKey = true;
            StopTaskTimer();
            this.fileLogger.CreateEntry("getting new session key from game client...");
            // Calling ShowDialog instead would block
            Invoke(new MethodInvoker(delegate() { this.instructionsDlg = new LoginInstructions(this); this.instructionsDlg.Show(); }));
        }

        private void OnResume(object sender, EventArgs e)
        {
            if (this.instructionsDlg.Visible) Invoke(new MethodInvoker(delegate() { this.instructionsDlg.Close(); }));
            this.fileLogger.CreateEntry("completed getting new session key from game client...");
            StartTaskTimer();
            Globals.gettingSessionKey = false;
        }
        #endregion

        #region System Tray
        private void GW2TP_Shown(object sender, EventArgs e)
        {
            trader.FnLoginInstructions += OnOpenLoginInstructions;
            trader.FnGW2Logined += OnResume;
            StartTaskTimer();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                settings.Save();
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            showToolStripMenuItem_Click(this, e);
            this.TabControl.SelectedTab = this.SettingsTabPage;
        }
#endregion

        #region IGemsTab
        public string GemCostPrefixLabel
        {
            get
            {
                return GemCostPrefix.Text;
            }
            set
            {
                GemCostPrefix.Text = value;
            }
        }

        public Label GemCostLabel
        {
            get
            {
                return this.GemCost;
            }
        }

        public string GoldWorthPrefixLabel
        {
            get
            {
                return GoldWorthPrefix.Text;
            }
            set
            {
                GoldWorthPrefix.Text = value;
            }
        }

        public Label GoldWorthLabel
        {
            get
            {
                return this.GoldWorth;
            }
        }

        public Label GoldToUSDLabel
        {
            get
            {
                return this.GoldToUSD;
            }
        }

        public string NotifyIconText
        {
            get
            {
                return this.notifyIcon1.Text;
            }
            set
            {
                this.notifyIcon1.Text = value;
            }
        }

        public Settings GW2TPSettings
        {
            get
            {
                return this.settings;
            }
        }

        public void Alert(string title, string msg)
        {
            string ttsString = string.Format("Alert!  {0}.  {1}", title, msg);
            reader.SpeakAsync(ttsString);

            //this.notifyIcon1.BalloonTipTitle = title;
            //this.notifyIcon1.BalloonTipText = msg;
            //this.notifyIcon1.ShowBalloonTip(30000); // show balloon tip for 30s max

            Invoke(new MethodInvoker(delegate()
            {
                taskbarNotifier.CloseClickable = false;
                taskbarNotifier.TitleClickable = false;
                taskbarNotifier.ContentClickable = false;
                taskbarNotifier.EnableSelectionRectangle = true;
                taskbarNotifier.KeepVisibleOnMousOver = true;
                taskbarNotifier.ReShowOnMouseOver = true;
                taskbarNotifier.Show(title, msg, 500, 10000, 500);
            }));

            string logMsg = string.Format("Alert! Title: {0} Msg: {1}", title, msg);
            this.fileLogger.CreateEntry(logMsg);
        }
        #endregion

        #region ISettingsTab Interface
        /// <summary>
        /// //////////////////////////// ISettingsTab Interface //////////////////////////////////////////
        /// </summary>
       
        public string TimerIntervalTB
        {
            get
            {
                return this.timerIntervalTextBox.Text;
            }
            set
            {
                this.timerIntervalTextBox.Text = value;
            }
        }

        public string GoldToGemTB
        {
            get
            {
                return this.goldToGemTextBox.Text;
            }
            set
            {
                this.goldToGemTextBox.Text = value;
            }
        }

        public string GemToGoldTB
        {
            get
            {
                return this.gemToGoldTextBox.Text;
            }
            set
            {
                this.gemToGoldTextBox.Text = value;
            }
        }

        public bool GoldToGemCB
        {
            get
            {
                return this.goldToGemCheckBox.Checked;
            }
            set
            {
                this.goldToGemCheckBox.Checked = value;
            }
        }

        public bool GemToGoldCB
        {
            get
            {
                return this.gemToGoldCheckBox.Checked;
            }
            set
            {
                this.gemToGoldCheckBox.Checked = value;
            }
        }

        public string LogFilePathTB
        {
            get
            {
                return this.LogFilePathTextBox.Text;
            }
            set
            {
                this.LogFilePathTextBox.Text = value;
            }
        }

        public Button LogFilePathBtn
        {
            get
            {
                return this.LogFilePathFolderBrowserDialogButton;
            }
        }

        public void SaveConfig()
        {
            if (_config.AppSettings.Settings["TimerInterval"] != null) _config.AppSettings.Settings["TimerInterval"].Value = this.settings.timeInterval.ToString();
            else _config.AppSettings.Settings.Add("TimerInterval", this.settings.timeInterval.ToString());

            if (_config.AppSettings.Settings["MaxGoldToGemPrice"] != null) _config.AppSettings.Settings["MaxGoldToGemPrice"].Value = this.settings.maxGoldToGem.ToString();
            else _config.AppSettings.Settings.Add("MaxGoldToGemPrice", this.settings.maxGoldToGem.ToString());

            if (_config.AppSettings.Settings["GoldToGemAlert"] != null) _config.AppSettings.Settings["GoldToGemAlert"].Value = this.settings.goldToGemAlert.ToString();
            else _config.AppSettings.Settings.Add("GoldToGemAlert", this.settings.goldToGemAlert.ToString());

            if (_config.AppSettings.Settings["MinGemToGoldPrice"] != null) _config.AppSettings.Settings["MinGemToGoldPrice"].Value = this.settings.minGemToGold.ToString();
            else _config.AppSettings.Settings.Add("MinGemToGoldPrice", this.settings.minGemToGold.ToString());

            if (_config.AppSettings.Settings["GemToGoldAlert"] != null) _config.AppSettings.Settings["GemToGoldAlert"].Value = this.settings.gemToGoldAlert.ToString();
            else _config.AppSettings.Settings.Add("GemToGoldAlert", this.settings.gemToGoldAlert.ToString());

            if (_config.AppSettings.Settings["LogFilePath"] != null) _config.AppSettings.Settings["LogFilePath"].Value = this.settings.logFilePath;
            else _config.AppSettings.Settings.Add("LogFilePath", this.settings.logFilePath);

            _config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            // Copy window location to app settings
            Properties.Settings.Default.WindowLocation = this.Location;

            // Copy window size to app settings
            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.WindowSize = this.Size;
            }
            else
            {
                Properties.Settings.Default.WindowSize = this.RestoreBounds.Size;
            }

            // Save settings
            Properties.Settings.Default.Save();
        }

        public void ResetTimer()
        {
            StopTaskTimer();
            StartTaskTimer();
        }

        public void ReOpenLogFile()
        {
            if (this.fileLogger != null)
            {
                this.fileLogger.CreateEntry(Globals.APPNAME + " Log File reset!  Closing Log File...");
                this.fileLogger.Close();
                this.fileLogger.Open(this.settings.logFilePath, false);
                this.fileLogger.CreateEntry(Globals.APPNAME + " Log File reset!  New Log File Opened...");
            }
        }

        /// <summary>
        /// //////////////////////////// End of ISettingsTab Interface //////////////////////////////////////////
        /// </summary>
        #endregion

        #region ISearchTab Interface
        public ComboBox SearchCB
        {
            get
            {
                return this.SearchComboBox;
            }
        }

        public Button SearchBtn
        {
            get
            {
                return this.SearchButton;
            }
        }

        public ComboBox SearchCategoryCB
        {
            get
            {
                return this.SearchCategoryComboBox;
            }
        }

        public ComboBox SearchSubcategoryCB
        {
            get
            {
                return this.SearchSubCategoryComboBox;
            }
        }

        public ComboBox SearchRarityCB
        {
            get
            {
                return this.SearchRarityComboBox;
            }
        }

        public ComboBox SearchArmorWeightCB
        {
            get
            {
                return this.ArmorWeightComboBox;
            }
        }

        public string SearchMinLevelTB
        {
            get
            {
                return this.SearchMinLevelTextBox.Text;
            }
            set
            {
                this.SearchMinLevelTextBox.Text = value;
            }
        }

        public string SearchMaxLevelTB
        {
            get
            {
                return this.SearchMaxLevelTextBox.Text;
            }
            set
            {
                this.SearchMaxLevelTextBox.Text = value;
            }
        }

        public ListViewEx SearchListViewLV
        {
            get
            {
                return this.SearchListView;
            }
        }

        public Expander SearchExpanderEx
        {
            get
            {
                return this.SearchExpander;
            }
        }

        public StatusStrip SearchStatusStripSS
        {
            get
            {
                return this.SearchStatusStrip;
            }
        }

        #endregion

        #region ITransaction
        public ListViewEx BuyListViewLV
        {
            get
            {
                return this.BuyListView;
            }
        }

        public ListViewEx SellListViewLV
        {
            get
            {
                return this.SellListView;
            }
        }

        public ListViewEx BoughtListViewLV
        {
            get
            {
                return this.BoughtListView;
            }
        }

        public ListViewEx SoldListViewLV
        {
            get
            {
                return this.SoldListView;
            }
        }

        public StatusStrip BuyStatusStripSS
        {
            get
            {
                return this.BuyStatusStrip;
            }
        }

        public StatusStrip SellStatusStripSS
        {
            get
            {
                return this.SellStatusStrip;
            }
        }

        public StatusStrip SoldStatusStripSS
        {
            get
            {
                return this.SoldStatusStrip;
            }
        }

        public StatusStrip BoughtStatusStripSS
        {
            get
            {
                return this.BoughtStatusStrip;
            }
        }
        #endregion

        private void GW2SpidyLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.gw2spidy.com/");
        }

        private void GW2DBPicture_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.gw2db.com/");
        }
    }
}
