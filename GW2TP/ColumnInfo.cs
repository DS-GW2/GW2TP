using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using GW2Miner.Engine;
using GW2Miner.Domain;
using ListViewEmbeddedControls;

namespace GW2TP
{
    /// <summary>
    /// TODO: Initialize current Buy lists like what was done for the Sell List.  Need to go through the entire list for both Buy and Sell, to screen alertable filters instead of just 
    ///     filtering the alertable options during display (e.g. getOutBidSubItem).
    /// </summary>
    public class ColumnInfo
    {
        public delegate ListViewItem.ListViewSubItem ColumnSubItemDelegate(Item item, IColumnInfo colInfo);
        public delegate object ColumnValueDelegate(Item item);

        public static List<Item> itemBoughtList = null, itemSellList = null;

        private readonly string columnName;
        private readonly ColumnSubItemDelegate columnSubItemFunction;
        private readonly ColumnValueDelegate columnValueFunction;
        private readonly string orderBy;

        private static object classLock = typeof(ColumnInfo);
        private static TradeWorker trader = new TradeWorker();

        private ColumnInfo() { }

        public ColumnInfo(string columnName, ColumnSubItemDelegate columnFunction, ColumnValueDelegate columnValue, string orderBy)
        {
            this.columnName = columnName;
            this.columnSubItemFunction = columnFunction;
            this.columnValueFunction = columnValue;
            this.orderBy = orderBy;
        }

        public static void InitializeItemBoughtList()
        {
            lock (classLock)
            {
                if (!Globals.gettingSessionKey)
                {
                    itemBoughtList = trader.get_my_buys(true, true).Result;
                    itemBoughtList.ForEach(x => trader.add_GW2DB_data(x));
                }
            }
        }

        public static void InitializeItemSellList()
        {
            lock (classLock)
            {
                if (!Globals.gettingSessionKey)
                {
                    itemSellList = trader.get_my_sells(true, false).Result;
                    itemSellList.ForEach(x => trader.add_GW2DB_data(x));
                }
            }
        }

        #region Properties
        public string ColumnName 
        { 
            get 
            { 
                return this.columnName; 
            } 
        }

        public ColumnSubItemDelegate ColumnSubItemFunction 
        { 
            get 
            { 
                return this.columnSubItemFunction; 
            } 
        }

        public ColumnValueDelegate ColumnValueFunction
        {
            get
            {
                return this.columnValueFunction;
            }
        }

        public string OrderBy 
        { 
            get 
            { 
                return this.orderBy; 
            } 
        }
        #endregion

        #region Column SubItems Accessor Functions
        public static object getName(Item item)
        {
            return item.Name;
        }

        public static ListViewItem.ListViewSubItem getNameSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getName(item).ToString();
            subItem.ForeColor = item.GetItemColor();
            return subItem;
        }

        public static object getLevel(Item item)
        {
            return item.MinLevel;
        }

        public static ListViewItem.ListViewSubItem getLevelSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getLevel(item).ToString();
            return subItem;
        }

        public static object getRarity(Item item)
        {
            return item.RarityId;
        }

        public static ListViewItem.ListViewSubItem getRaritySubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getRarity(item).ToString();
            subItem.ForeColor = item.GetItemColor();
            return subItem;
        }

        public static object getQuantity(Item item)
        {
            return item.Quantity;
        }

        public static ListViewItem.ListViewSubItem getQuantitySubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getQuantity(item).ToString();
            return subItem;
        }

        public static object getUnitPrice(Item item)
        {
            return item.UnitPrice;
        }

        public static ListViewItem.ListViewSubItem getUnitPriceSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getUnitPrice(item).ToString();
            return subItem;
        }

        public static object getDaysPosted(Item item)
        {
            TimeSpan ts = DateTime.Now - item.Created;
            return ts.Duration();
        }

        public static ListViewItem.ListViewSubItem getDaysPostedSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            TimeSpan ts = (TimeSpan) getDaysPosted(item);            
            if (ts.Days > 0) subItem.Text = ts.Days.ToString() + " days ago";
            else if (ts.Hours > 0) subItem.Text = ts.Hours.ToString() + " hours ago";
            else if (ts.Minutes > 0) subItem.Text = ts.Minutes.ToString() + " minutes ago";
            else subItem.Text = ts.Seconds.ToString() + " seconds ago";
            return subItem;
        }

        public static object getBoughtPrice(Item item)
        {
            if (item.BoughtPrice == -2)
            {
                // Threading: If itemBoughtList is null, this will hang.
                item.BoughtPrice = trader.GetMyBoughtPrice(item, item.Created, itemBoughtList);
            }
            return item.BoughtPrice;
        }

        public static ListViewItem.ListViewSubItem getBoughtPriceSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            int boughtPrice = (int)getBoughtPrice(item);
            //if (boughtPrice == -1) subItem.Text = "-";
            //else subItem.Text = boughtPrice.ToString();
            subItem.Text = boughtPrice.ToString();
            return subItem;
        }

        public static object getProfit(Item item)
        {
            if (item.Profit == int.MinValue)
            {
                int boughtPrice = (int)getBoughtPrice(item);
                item.Profit = (int)Math.Ceiling(((item.UnitPrice * 0.85) - boughtPrice) * item.Quantity);
            }
            return item.Profit;
        }

        public static ListViewItem.ListViewSubItem getProfitSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            int profit = (int)getProfit(item);
            //if (profit == int.MinValue) subItem.Text = "-";
            //else subItem.Text = profit.ToString();
            subItem.Text = profit.ToString();
            return subItem;
        }

        public static object getProfitNow(Item item)
        {
            if (item.ProfitNow == int.MinValue)
            {
                int boughtPrice = (int)getBoughtPrice(item);
                //if (boughtPrice > 0)
                //{
                item.ProfitNow = (int)Math.Ceiling((((item.MinSaleUnitPrice - 1) * 0.85) - boughtPrice - (item.UnitPrice * 0.05)) * item.Quantity);
                //}
                //else
                //{
                //    item.ProfitNow = int.MinValue;
                //}
            }
            return item.ProfitNow;
        }

        public static ListViewItem.ListViewSubItem getProfitNowSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            int profitNow = (int)getProfitNow(item);
            //if (profitNow == int.MinValue) subItem.Text = "-";
            //else subItem.Text = profitNow.ToString();
            subItem.Text = profitNow.ToString();
            return subItem;
        }

        public static ListViewItem.ListViewSubItem getOutBidSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                lock (classLock)
                {
                    if (colInfo.ListViewLV != null)
                    {
                        Task<List<ItemBuySellListingItem>> itemBuyListing = trader.get_buy_listings(item.Id);
                        if (itemBuyListing.Result != null && itemBuyListing.Result.Count > 0)
                        {
                            ItemBuySellListingItem listing = itemBuyListing.Result[0];

                            int breakEvenPrice = (int)getWorth(item);
                            if (breakEvenPrice <= item.UnitPrice)
                            {
                                string msg = String.Format("{0} is not worth({1}) the price({2}) you are bidding for anymore.", item.ListingId, item.Name, breakEvenPrice, item.UnitPrice);
                                string title = String.Format("{0} lost its value!", item.Name);
                                colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                                {
                                    colInfo.Alert(title, msg);
                                    subItem.Text = "Cancel Bid!";
                                    subItem.Tag = (long)item.ListingId;
                                }));
                            }
                            else if (listing.PricePerUnit > item.UnitPrice)
                            {
                                if ((!item.IAmSelling || item.sellUnderCut) && breakEvenPrice > item.UnitPrice)
                                {
                                    string msg = String.Format("You bid {0}(quantity: {1}) but others have bid {2}(quantity: {3}) - BreakEven {4}",
                                                                    item.UnitPrice, item.Quantity, listing.PricePerUnit, listing.NumberAvailable, breakEvenPrice);
                                    string title = String.Format("{0} Undercut!", item.Name);
                                    colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                                    {
                                        colInfo.Alert(title, msg);
                                        subItem.Text = "OutBid!";
                                        subItem.Tag = (int)Math.Min(listing.PricePerUnit + 1, breakEvenPrice - 1);
                                    }));
                                }
                            }
                            else if (itemBuyListing.Result.Count > 1 && (item.UnitPrice - itemBuyListing.Result[1].PricePerUnit) > 1)
                            {
                                listing = itemBuyListing.Result[1];
                                string msg = String.Format("You bid {0}(quantity: {1}) but others have bid {2}(quantity: {3}) - BreakEven {4} at Sell Price {7}",
                                item.UnitPrice, item.Quantity, listing.PricePerUnit, listing.NumberAvailable,
                                    (int)Math.Floor((item.MinSaleUnitPrice - 1) * 0.85), item.MinSaleUnitPrice - 1);
                                string title = String.Format("You are paying too much for {0}", item.Name);

                                colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                                {
                                    colInfo.Alert(title, msg);
                                    subItem.Text = "Reduce Bid!";
                                    subItem.Tag = (int)(listing.PricePerUnit + 1);
                                }));
                            }
                            else
                            {
                                colInfo.ListViewLV.Invoke((MethodInvoker)(() => subItem.Text = "-"));
                            }
                        }
                    }
                }
            });
            return subItem;
        }

        public static ListViewItem.ListViewSubItem getSellUnderCutSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                lock (classLock)
                {
                    if (colInfo.ListViewLV != null)
                    {
                        Task<List<ItemBuySellListingItem>> itemSellListing = trader.get_sell_listings(item.Id);
                        if (itemSellListing.Result != null && itemSellListing.Result.Count > 0)
                        {
                            ItemBuySellListingItem listing = itemSellListing.Result[0];
                            if (listing.PricePerUnit < item.UnitPrice)
                            {
                                // someone is undercutting me

                                // we can assume the sellListing is sorted from lowest to highest unit sale price
                                int sum = 0;
                                int profit = 0;
                                bool ridiculous = true;
                                foreach (ItemBuySellListingItem sellListing in itemSellListing.Result)
                                {
                                    if (item.UnitPrice * 0.85 > sellListing.PricePerUnit)
                                    {
                                        profit = profit + (int)Math.Floor(item.UnitPrice * 0.85 - sellListing.PricePerUnit) * sellListing.NumberAvailable;
                                        sum = sum + sellListing.PricePerUnit * sellListing.NumberAvailable;
                                    }
                                    if (sellListing.PricePerUnit == item.UnitPrice)
                                    {
                                        if (ridiculous)
                                        {
                                            string textString = String.Format("Buy all undercut orders(sum={0})!", sum);
                                            string msg = String.Format("My Price = {0} Profit = {1} Cost to rectify = {2}",
                                                                            item.UnitPrice, profit, sum);
                                            string title = String.Format("Ridiculous undercut {0}", item.Name);
                                            colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                                            {
                                                colInfo.Alert(title, msg);
                                                subItem.Text = textString;
                                                subItem.Tag = (List<ItemBuySellListingItem>)itemSellListing.Result;
                                            }));
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        ridiculous = false;
                                    }
                                }
                            }
                        }
                    }
                }
            });
            return subItem;
        }

        public static ListViewItem.ListViewSubItem RemoveBuyTransactionSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            item.IAmSelling = false;
            subItem.Text = "Remove";
            subItem.Tag = (long)item.ListingId;
            return subItem;
        }

        public static ListViewItem.ListViewSubItem RemoveSellTransactionSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            item.IAmSelling = true;
            subItem.Text = "Remove";
            subItem.Tag = (long)item.ListingId;
            return subItem;
        }

        public static object getWorth(Item item)
        {
            if (item.Worth == -1)
            {
                //bool iAmSelling, underCut;
                //Item myItemOnSale;

                item.Worth = trader.Worth(item, itemSellList);
                //item.Worth = Math.Max(item.VendorPrice, (int)(0.85 * trader.GetMySellPrice(itemSellList, item, out iAmSelling, out underCut, out myItemOnSale)));

                //item.IAmSelling = iAmSelling;
                //item.sellUnderCut = underCut;
                //item.myItemOnSale = myItemOnSale;
            }
            return item.Worth;
        }

        public static ListViewItem.ListViewSubItem getWorthSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            if (colInfo.ListViewLV != null)
            {
                ThreadPool.QueueUserWorkItem((obj) =>
                {
                    lock (classLock)
                    {
                        int worth = (int)getWorth(item);
                        subItem.ForeColor = worth <= item.UnitPrice ? Color.IndianRed : Color.LightGreen;
                        colInfo.ListViewLV.Invoke((MethodInvoker)(() => subItem.Text = worth.ToString()));
                    }
                });
            }
            return subItem;
        }

        public static object getSell(Item item)
        {
            return item.MinSaleUnitPrice;
        }

        public static ListViewItem.ListViewSubItem getSellSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getSell(item).ToString();
            return subItem;
        }

        public static object getSupply(Item item)
        {
            return item.SellCount;
        }

        public static ListViewItem.ListViewSubItem getSupplySubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getSupply(item).ToString();
            return subItem;
        }

        public static object getBuy(Item item)
        {
            return item.MaxOfferUnitPrice;
        }

        public static ListViewItem.ListViewSubItem getBuySubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getBuy(item).ToString();
            return subItem;
        }

        public static object getDemand(Item item)
        {
            return item.BuyCount;
        }

        public static ListViewItem.ListViewSubItem getDemandSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getDemand(item).ToString();
            return subItem;
        }

        public static object getMargin(Item item)
        {
            if (item.Margin == -1.0)
            {
                double margin = (item.MinSaleUnitPrice - 1) * 0.85 - (item.MaxOfferUnitPrice + 1);
                item.Margin = Math.Floor(margin);
            }
            return item.Margin;
        }

        public static ListViewItem.ListViewSubItem getMarginSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getMargin(item).ToString();
            return subItem;
        }

        public static object getPercentProfit(Item item)
        {
            if (item.PercentProfit == -1.0)
            {
                double margin = (double)getMargin(item);
                double profit = (margin / (item.MaxOfferUnitPrice + 1)) * 100.0;
                item.PercentProfit = Math.Floor(profit);
            }
            return item.PercentProfit;
        }

        public static ListViewItem.ListViewSubItem getPercentProfitSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = getPercentProfit(item).ToString();
            return subItem;
        }

        public static object getVendorSellPrice(Item item)
        {
            return item.VendorPrice;
        }

        public static ListViewItem.ListViewSubItem getVendorSellPriceSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            int vendorPrice = (int)getVendorSellPrice(item);
            subItem.ForeColor = (vendorPrice > (0.85 * item.MinSaleUnitPrice)) ? Color.Blue : (vendorPrice > (0.85 * item.MaxOfferUnitPrice)) ? Color.LightGreen : Color.Black;
            subItem.Text = vendorPrice.ToString();
            return subItem;
        }

        public static object getCraftingCost(Item item)
        {
            if (trader.GW2DBLoaded)
            {
                /// TODO: Use the gw2spidy API to get the recipe which contains the crafting cost, since we only need the gold cost value anyway 
                /// if the gw2spidy API fails with an exception then do this by calling MinCraftingCost as per original code
                RecipeCraftingCost recipeCraftCost = trader.MinCraftingCost(item.Recipes);
                return (recipeCraftCost != null) ? recipeCraftCost.GoldCost : 0;
            }
            return 0;
        }

        // Simpler function just for sorting.  We can't access the web while sorting because it is executing in UI thread.
        // So we use the stored MinCraftingCost property for each recipe.  This assumes that the MinCraftingCost of ALL recipes that we need 
        // are calculated before sort was called.
        public static object getCraftingCostComp(Item item)
        {
            if (trader.GW2DBLoaded)
            {
                RecipeCraftingCost recipeCraftCost = item.MinCraftingCost;
                return (recipeCraftCost != null) ? recipeCraftCost.GoldCost : 0;
            }
            return 0;
        }

        public static ListViewItem.ListViewSubItem getCraftingCostSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            if (colInfo.ListViewLV != null)
            {
                ThreadPool.QueueUserWorkItem((obj) =>
                {
                    lock (classLock)
                    {
                        int craftingCost = (int)getCraftingCost(item);
                        if (craftingCost > 0)
                        {
                            colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                            {
                                subItem.ForeColor = (craftingCost < (0.85 * item.MinSaleUnitPrice)) ? ((craftingCost < (0.85 * item.MaxOfferUnitPrice)) ? Color.Blue : Color.LightGreen) : Color.IndianRed;
                                subItem.Text = craftingCost.ToString();
                                subItem.Tag = item.Recipes[0];
                            }));
                        }
                        else
                        {
                            colInfo.ListViewLV.Invoke((MethodInvoker)(() =>
                            {
                                subItem.Text = "-";
                            }));
                        }
                    }
                });
            }
            return subItem;
        }

        public static ListViewItem.ListViewSubItem GetSellListingsSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = "View";
            subItem.Tag = new Listings(false, item);
            return subItem;
        }

        public static ListViewItem.ListViewSubItem GetBuyListingsSubItem(Item item, IColumnInfo colInfo)
        {
            ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();
            subItem.Text = "View";
            subItem.Tag = new Listings(true, item);
            return subItem;
        }
        #endregion
    }
}
