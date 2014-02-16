using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using GW2Miner.Engine;
using GW2Miner.Domain;
using ListViewEmbeddedControls;
using CustomToolTipDemo;

namespace GW2TP
{
    public class Transactions : DataSource
    {
        private const int TRANSACTION_PAGE_SIZE = 10;
        private object classLock = typeof(Transactions);

        private ITransaction transTab;
        private ListViewEx listView;

        private bool buy, past;
        private TradeWorker trader = new TradeWorker();
        private int offset = 1;

        private ColumnInfo[] buyColumns = { 
            new ColumnInfo("Image", null, null, ""), new ColumnInfo("Name", ColumnInfo.getNameSubItem, ColumnInfo.getName, ""), 
            new ColumnInfo("Rarity", ColumnInfo.getRaritySubItem, ColumnInfo.getRarity, ""), new ColumnInfo("Qty", ColumnInfo.getQuantitySubItem, ColumnInfo.getQuantity, ""), 
            new ColumnInfo("Price Per Unit", ColumnInfo.getUnitPriceSubItem, ColumnInfo.getUnitPrice, ""), new ColumnInfo("Sell", ColumnInfo.getSellSubItem, ColumnInfo.getSell, ""), 
            new ColumnInfo("Buy", ColumnInfo.getBuySubItem, ColumnInfo.getBuy, ""), new ColumnInfo("Posted", ColumnInfo.getDaysPostedSubItem, ColumnInfo.getDaysPosted, ""),
            new ColumnInfo("Break-Even", ColumnInfo.getWorthSubItem, ColumnInfo.getWorth, ""), new ColumnInfo("Bid", ColumnInfo.getOutBidSubItem, null, ""),
            new ColumnInfo("Sell Listings", ColumnInfo.GetSellListingsSubItem, null, ""),  new ColumnInfo("Buy Listings", ColumnInfo.GetBuyListingsSubItem, null, ""), 
            new ColumnInfo("Remove", ColumnInfo.RemoveBuyTransactionSubItem, null, "")
       };

        private ColumnInfo[] sellColumns = {
            new ColumnInfo("Image", null, null, ""), new ColumnInfo("Name", ColumnInfo.getNameSubItem, ColumnInfo.getName, ""), 
            new ColumnInfo("Rarity", ColumnInfo.getRaritySubItem, ColumnInfo.getRarity, ""), new ColumnInfo("Qty", ColumnInfo.getQuantitySubItem, ColumnInfo.getQuantity, ""), 
            new ColumnInfo("Price Per Unit", ColumnInfo.getUnitPriceSubItem, ColumnInfo.getUnitPrice, ""), new ColumnInfo("Sell", ColumnInfo.getSellSubItem, ColumnInfo.getSell, ""), 
            new ColumnInfo("Buy", ColumnInfo.getBuySubItem, ColumnInfo.getBuy, ""), new ColumnInfo("Posted", ColumnInfo.getDaysPostedSubItem, ColumnInfo.getDaysPosted, ""),
            new ColumnInfo("Bought", ColumnInfo.getBoughtPriceSubItem, ColumnInfo.getBoughtPrice, ""), new ColumnInfo("Profit", ColumnInfo.getProfitSubItem, ColumnInfo.getProfit, ""), 
            new ColumnInfo("Profit Now", ColumnInfo.getProfitNowSubItem, ColumnInfo.getProfitNow, ""), new ColumnInfo("UnderCut", ColumnInfo.getSellUnderCutSubItem, null, ""), 
            new ColumnInfo("Sell Listings", ColumnInfo.GetSellListingsSubItem, null, ""), new ColumnInfo("Buy Listings", ColumnInfo.GetBuyListingsSubItem, null, ""), 
            new ColumnInfo("Remove", ColumnInfo.RemoveSellTransactionSubItem, null, "")
        };

        private ColumnInfo[] boughtColumns = { 
            new ColumnInfo("Image", null, null, ""), new ColumnInfo("Name", ColumnInfo.getNameSubItem, ColumnInfo.getName, ""), 
            new ColumnInfo("Rarity", ColumnInfo.getRaritySubItem, ColumnInfo.getRarity, ""), new ColumnInfo("Qty", ColumnInfo.getQuantitySubItem, ColumnInfo.getQuantity, ""), 
            new ColumnInfo("Price Per Unit", ColumnInfo.getUnitPriceSubItem, ColumnInfo.getUnitPrice, ""), new ColumnInfo("Sell", ColumnInfo.getSellSubItem, ColumnInfo.getSell, ""), 
            new ColumnInfo("Buy", ColumnInfo.getBuySubItem, ColumnInfo.getBuy, ""), new ColumnInfo("Posted", ColumnInfo.getDaysPostedSubItem, ColumnInfo.getDaysPosted, ""),
            new ColumnInfo("Sell Listings", ColumnInfo.GetSellListingsSubItem, null, ""),  new ColumnInfo("Buy Listings", ColumnInfo.GetBuyListingsSubItem, null, "")
       };

        private ColumnInfo[] soldColumns = {
            new ColumnInfo("Image", null, null, ""), new ColumnInfo("Name", ColumnInfo.getNameSubItem, ColumnInfo.getName, ""), 
            new ColumnInfo("Rarity", ColumnInfo.getRaritySubItem, ColumnInfo.getRarity, ""), new ColumnInfo("Qty", ColumnInfo.getQuantitySubItem, ColumnInfo.getQuantity, ""), 
            new ColumnInfo("Price Per Unit", ColumnInfo.getUnitPriceSubItem, ColumnInfo.getUnitPrice, ""), new ColumnInfo("Sell", ColumnInfo.getSellSubItem, ColumnInfo.getSell, ""), 
            new ColumnInfo("Buy", ColumnInfo.getBuySubItem, ColumnInfo.getBuy, ""), new ColumnInfo("Posted", ColumnInfo.getDaysPostedSubItem, ColumnInfo.getDaysPosted, ""),
            new ColumnInfo("Bought", ColumnInfo.getBoughtPriceSubItem, ColumnInfo.getBoughtPrice, ""), new ColumnInfo("Profit", ColumnInfo.getProfitSubItem, ColumnInfo.getProfit, ""), 
            new ColumnInfo("Sell Listings", ColumnInfo.GetSellListingsSubItem, null, ""), new ColumnInfo("Buy Listings", ColumnInfo.GetBuyListingsSubItem, null, "")
        };

        public Transactions(ITransaction transTab, bool buy, bool past)
        {
            this.transTab = transTab;
            this.buy = buy;
            this.past = past;

            if (this.buy && !this.past)
            {
                this.listView = this.transTab.BuyListViewLV;
                this.searchResult = new SearchResult(this.transTab.BuyListViewLV, this.buyColumns, this, this.transTab.Alert);
                this.dataPager = new SimpleDataPager(TRANSACTION_PAGE_SIZE, this.searchResult, this.transTab.BuyStatusStripSS);
            }
            else if (!this.buy && !this.past)
            {
                this.listView = this.transTab.SellListViewLV;
                this.searchResult = new SearchResult(this.transTab.SellListViewLV, this.sellColumns, this, this.transTab.Alert);
                this.dataPager = new SimpleDataPager(TRANSACTION_PAGE_SIZE, this.searchResult, this.transTab.SellStatusStripSS);
            }
            else if (this.buy)
            {
                this.listView = this.transTab.BoughtListViewLV;
                this.searchResult = new SearchResult(this.transTab.BoughtListViewLV, this.boughtColumns, this, this.transTab.Alert);
                this.dataPager = new SimpleDataPager(TRANSACTION_PAGE_SIZE, this.searchResult, this.transTab.BoughtStatusStripSS);
            }
            else
            {
                this.listView = this.transTab.SoldListViewLV;
                this.searchResult = new SearchResult(this.transTab.SoldListViewLV, this.soldColumns, this, this.transTab.Alert);
                this.dataPager = new SimpleDataPager(TRANSACTION_PAGE_SIZE, this.searchResult, this.transTab.SoldStatusStripSS);
            }

            base.Init();
        }

        public void Update(bool forced = false)
        {
            lock (classLock)
            {
                if (!Globals.gettingSessionKey)
                {
                    if (ColumnInfo.itemBoughtList == null || forced)
                    {
                        Task t1 = Task.Factory.StartNew(() => ColumnInfo.InitializeItemBoughtList());
                        t1.Wait();
                    }
                    if (ColumnInfo.itemSellList == null || forced)
                    {
                        Task t1 = Task.Factory.StartNew(() => ColumnInfo.InitializeItemSellList());
                        t1.Wait();
                    }

                    if (this.past && this.buy && ColumnInfo.itemBoughtList != null)
                    {
                        this.cachedResult = ColumnInfo.itemBoughtList;
                        SetupCacheData();
                    }
                    else if (!this.past && !this.buy && ColumnInfo.itemSellList != null)
                    {
                        this.cachedResult = ColumnInfo.itemSellList;
                        SetupCacheData();
                    }
                    else
                    {
                        this.dataPager.RefreshPage();
                    }
                }
            }
        }

        public override void fetchData(int offset, int count, DataReadyFunc OnDataReady)
        {
            ThreadPool.QueueUserWorkItem((obj) => 
            {
                lock (classLock)
                {
                    this.offset = offset;

                    if (this.cachedResult != null && this.totalItems == this.cachedResult.Count && this.getAllPages)
                    {
                        OnDataReady(this.cachedResult, this.offset, count, this.totalItems, false);
                    }
                    else if (!Globals.gettingSessionKey)
                    {
                        if (this.buy && !this.past)
                        {
                            Globals.BLSalvageCost = trader.BlackLionKitSalvageCost;
                            trader.ClearInsigniaPrices();
                        }
                        if (getAllPages) offset = 1; // if we are getting all pages we start at offset 1
                        //trader.get_my_buys_sells_transactions(this.buy, getAllPages, this.past, offset, TRANSACTION_PAGE_SIZE, orderBy, sortDescending).ContinueWith((fetchDataTask) => 
                        trader.get_my_buys_sells_transactions(this.buy, getAllPages, this.past, offset, TRANSACTION_PAGE_SIZE).ContinueWith((fetchDataTask) =>
                                                {
                                                    TradeWorker.Args transArgs = trader.LastTransactionArgs;
                                                    this.totalItems = transArgs.max;
                                                    fetchDataTask.Result.ForEach(x => trader.add_GW2DB_data(x));

                                                    this.cachedResult = fetchDataTask.Result;
                                                    OnDataReady(this.cachedResult, this.offset, count, this.totalItems, orderBy == string.Empty);
                                                });
                    }
                }
            });
        }

        private void SetupCacheData()
        {
            this.getAllPages = true;
            this.totalItems = this.cachedResult.Count;
            this.dataPager.RefreshPage(false);
        }
    }
}
