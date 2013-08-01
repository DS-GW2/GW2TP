using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2TP
{
    /// <summary>
    /// TODO: Add a Refresh button
    /// </summary>
    class Gems
    {
        private IGemsTab gemsTab;
        private TradeWorker trader = new TradeWorker();

        private Object classLock = typeof(Gems);

        public Gems(IGemsTab mainForm)
        {
            this.gemsTab = mainForm;
        }

        public void Update()
        {
            lock (classLock)
            {
                if (!Globals.gettingSessionKey)
                {
                    TradeWorker.GemPriceTP gemPrice = trader.get_gem_price().Result;
                    int goldToGemPrice = gemPrice.gold_to_gem;
                    int gemToGoldPrice = gemPrice.gem_to_gold;
                    double GoldToUSDPrice = Math.Round(12500.0 / gemToGoldPrice, 2);

                    string gemCostString = goldToGemPrice.ToString() + " to buy.";
                    string goldWorthString = gemToGoldPrice.ToString();
                    string GoldToUSDString = GoldToUSDPrice + " USD to buy through gems.";

                    this.gemsTab.GemCostLabel.Invoke((MethodInvoker)(() => this.gemsTab.GemCostLabel.Text = gemCostString));
                    this.gemsTab.GoldWorthLabel.Invoke((MethodInvoker)(() => this.gemsTab.GoldWorthLabel.Text = goldWorthString));
                    this.gemsTab.GoldToUSDLabel.Invoke((MethodInvoker)(() => this.gemsTab.GoldToUSDLabel.Text = GoldToUSDString));

                    this.gemsTab.NotifyIconText = this.gemsTab.GemCostPrefixLabel + " " + gemCostString + Environment.NewLine +
                                                                            this.gemsTab.GoldWorthPrefixLabel + " " + goldWorthString;

                    if (this.gemsTab.GW2TPSettings.goldToGemAlert && goldToGemPrice < this.gemsTab.GW2TPSettings.maxGoldToGem)
                    {
                        string msg = string.Format("100 gems now cost {0} which is < {1}", goldToGemPrice, this.gemsTab.GW2TPSettings.maxGoldToGem);
                        this.gemsTab.Alert("Gold to Gem Alert!", msg);
                        this.gemsTab.GW2TPSettings.goldToGemAlert = false; // turn it off after the first alert
                    }

                    if (this.gemsTab.GW2TPSettings.gemToGoldAlert && gemToGoldPrice > this.gemsTab.GW2TPSettings.minGemToGold)
                    {
                        string msg = string.Format("100 gems now sell for {0} which is > {1}", gemToGoldPrice, this.gemsTab.GW2TPSettings.minGemToGold);
                        this.gemsTab.Alert("Gem to Gold Alert!", msg);
                        this.gemsTab.GW2TPSettings.gemToGoldAlert = false; // turn it off after the first alert
                    }
                }
            }
        }
    }
}
