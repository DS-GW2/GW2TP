using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2TP
{
    public partial class Listings : Form
    {
        bool isBuy;
        Item item;

        TradeWorker trader = new TradeWorker();

        public Listings(bool isBuy, Item item)
        {
            this.isBuy = isBuy;
            this.item = item;

            InitializeComponent();

            if (!isBuy)
            {
                this.Text = "Sell Listings";
                this.ListingsListView.Columns[1].Text = "Available";
                this.ListingsListView.Columns[2].Text = "Sellers";
            }

            this.Text = String.Concat(this.Text, String.Format(" for {0}", item.Name));

            //this.ListingsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.ListingsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void HideButton_Click(object sender, EventArgs e)
        {
            this.Hide();  // calling Close() instead would dispose the form, so we use Hide() now
            this.DialogResult = DialogResult.OK;
        }

        private int GetMySellingPrice(Item refItem, DateTime refDateTime)
        {
            List<Item> sellList = ColumnInfo.itemSellList ?? trader.get_my_sells(true).Result;
            foreach (Item item in sellList)
            {
                if (item.Created > refDateTime) continue;
                if (item.Id == refItem.Id)
                {
                    return item.UnitPrice;
                }
            }

            return -1;
        }

        private int GetMyBuyingPrice(Item refItem, DateTime refDateTime)
        {
            List<Item> buyList = trader.get_my_buys(true).Result;
            foreach (Item item in buyList)
            {
                if (item.Created > refDateTime) continue;
                if (item.Id == refItem.Id)
                {
                    return item.UnitPrice;
                }
            }

            return -1;
        }

        private void Listings_Shown(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                List<ItemBuySellListingItem> result;

                if (isBuy) result = trader.get_buy_listings(item.Id).Result;
                else result = trader.get_sell_listings(item.Id).Result;

                int boughtPrice = 0, buyingSellingPrice = 0, breakEvenPrice = 0;

                if (!this.isBuy)
                {
                    //worth = ((item.Worth < 0) ? (int)ColumnInfo.getWorth(item) : item.Worth);
                    if (item.myItemOnSale != null)
                    {
                        buyingSellingPrice = item.myItemOnSale.UnitPrice;
                    }
                    else
                    {
                        buyingSellingPrice = GetMySellingPrice(item, (item.Created == DateTime.MinValue ? DateTime.MaxValue : item.Created));
                    }

                    // get the most recently bought of such an item or its recorded bought price
                    boughtPrice = ((item.BoughtPrice < 0) ? (int)ColumnInfo.getBoughtPrice(item) : item.BoughtPrice);
                    if (boughtPrice >= 0)
                    {
                        breakEvenPrice = (int)Math.Ceiling(boughtPrice / 0.85);

                        this.DescriptionLabel.Invoke(new MethodInvoker(() => this.DescriptionLabel.Text = String.Format("Found bought price = {0}.  BreakEven price = {1}", boughtPrice, breakEvenPrice)));
                    }
                }
                else
                {
                    buyingSellingPrice = GetMyBuyingPrice(item, (item.Created == DateTime.MinValue ? DateTime.MaxValue : item.Created));
                    breakEvenPrice = (int)Math.Floor(0.85 * (item.MinSaleUnitPrice - 1));
                    this.DescriptionLabel.Invoke(new MethodInvoker(() => this.DescriptionLabel.Text = String.Format("Current BreakEven price = {0}", breakEvenPrice)));
                }

                this.ListingsListView.Invoke(new MethodInvoker(delegate()
                    {
                        this.ListingsListView.Items.Clear();

                        foreach (ItemBuySellListingItem buySellItem in result)
                        {
                            this.ListingsListView.Items.Add(CreateRow(buySellItem, buyingSellingPrice, breakEvenPrice));
                        }

                        this.ListingsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                        this.ListingsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    }));
            });
        }

        private ListViewItem CreateRow(ItemBuySellListingItem buySellItem, int sellingPrice, int breakEvenPrice)
        {
            ListViewItem listViewItem = new ListViewItem();

            if ((!isBuy && buySellItem.PricePerUnit < breakEvenPrice) || (isBuy && buySellItem.PricePerUnit > breakEvenPrice)) listViewItem.ForeColor = Color.IndianRed;
            if (sellingPrice >= 0 && buySellItem.PricePerUnit == sellingPrice)
            {
                listViewItem.Text = String.Format(">>>>> {0}", buySellItem.PricePerUnit);
            }
            else
            {
                listViewItem.Text = buySellItem.PricePerUnit.ToString();
            }

            listViewItem.SubItems.Add(buySellItem.NumberAvailable.ToString());
            listViewItem.SubItems.Add(buySellItem.NumberOfListings.ToString());

            return listViewItem;
        }

        private void Listings_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            this.Parent = null;
            e.Cancel = true;
        }
    }
}
