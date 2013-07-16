using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using GW2Miner.Engine;
using GW2Miner.Domain;
using ListViewEmbeddedControls;
using CustomToolTipDemo;

namespace GW2TP
{
    public class SearchResult : IMyPageableItemContainer, IColumnInfo
    {
        public delegate void AlertFunction(string title, string msg);

        private List<Item> itemList = new List<Item>();
        private ListViewEx listView;
        private IDataSource dataSource;
        //private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();
        private VirtualModeListViewColumnSorter lvwColumnSorter = new VirtualModeListViewColumnSorter();
        private TradeWorker trader = new TradeWorker();
        private ImageList imageListSmall = new ImageList();
        private List<int> imageListTracker = new List<int>();
        private ImageCache Cache = new ImageCache("Cache");
        private AutoResetEvent ImageSavedEvent = new AutoResetEvent(false);
        private int imageCount = 0;
        private int maximumRows = 0, offset = 1, totalRowCount = 0;

        private ListViewItem mLastItem = new ListViewItem();
        private CustomizedToolTip tooltip = new CustomizedToolTip();

        private ColumnInfo[] columns;

        private ListViewItem[] myCache; //array to cache items for the virtual list 
        private int firstItem; //stores the index of the first item in the cache
        private event EventHandler<ItemSortEventArgs> itemSortEventHandler;
        private bool needReverseList = false;

        private event EventHandler<MyPageEventArgs> totalRowCountAvailable;
        protected event EventHandler pagePropertiesChanged;

        private readonly AlertFunction AlertFn;

        private SearchResult()
        {
        }

        public SearchResult(ListViewEx listView, ColumnInfo[] columns, IDataSource dataSource, AlertFunction AlertFn)
        {
            this.listView = listView;            
            this.columns = columns;
            this.dataSource = dataSource;
            this.AlertFn = AlertFn;

            Init(columns);
        }

        public void Update(List<Item> list, int offset, int count, int total, bool needSortSelf)
        {
            listView.Invoke((MethodInvoker)(() =>
            {
                if (needSortSelf && lvwColumnSorter.Order != SortOrder.None)
                {
                    list = Sort(list);
                }
                //int listSize = Math.Min(list.Count - offset + 1, pageSize);
                //int listSize = Math.Min(list.Count, count);
                int listSize;

                if (list.Count > count)
                    listSize = Math.Min(list.Count - offset + 1, count);
                else
                    listSize = Math.Min(total - offset + 1, count);

                if (listSize != this.maximumRows || offset != this.offset)
                {
                    this.maximumRows = listSize;
                    this.offset = offset;
                    OnPagePropertiesChanged(new EventArgs());
                }

                this.totalRowCount = total;
                OnTotalRowCountAvailable(new MyPageEventArgs(this.offset, this.maximumRows, this.totalRowCount));

                if (list.Count > listSize)
                {
                    //int size = Math.Min(listSize, list.Count - offset + 1);

                    // the list contains all the records
                    this.itemList = list.GetRange(offset - 1, listSize);
                }
                else
                {
                    this.itemList = list;
                }

                this.imageListTracker.Clear();
                this.imageListSmall.Images.Clear();
                this.listView.Items.Clear();

                this.listView.SetSortIcon(lvwColumnSorter.SortColumn, lvwColumnSorter.Order);

                this.imageListSmall.ImageSize = new Size(32, 32);
                imageCount = 0;
                listView.SmallImageList = imageListSmall;
                firstItem = 0;
                myCache = null;
                this.listView.VirtualListSize = 0;
                this.listView.Update();
                this.listView.VirtualListSize = listSize;
                this.listView.Update();

                this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }));
        }

        public event EventHandler<ItemSortEventArgs> ItemSortEventHandler
        {
            add
            {
                lock (typeof(SearchResult))
                {
                    itemSortEventHandler += value;
                }
            }

            remove
            {
                lock (typeof(SearchResult))
                {
                    itemSortEventHandler -= value;
                }
            }
        }

        public void ResetSortOrder()
        {
            //this.listView.SetSortIcon(lvwColumnSorter.SortColumn, SortOrder.None);
            //needSortSelf = false;
            lvwColumnSorter.Order = SortOrder.None;
            //lvwColumnSorter.SortColumn = 0;
        }

        public List<Item> Sort(List<Item> list)
        {
            if (lvwColumnSorter.Order != SortOrder.None)
            {
                if (this.needReverseList)  // provides some optimization but ASSUMES cached list as input!
                {
                    this.needReverseList = false;
                    list.Reverse();
                }
                else
                {
                    list.Sort(lvwColumnSorter);
                }
            }
                //this.needSortSelf = false;

            return list;
        }

        public ColumnInfo[] Columns
        {
            get
            {
                return columns;
            }
            set
            {
                columns = value;
            }
        }

        protected virtual void OnPagePropertiesChanged(EventArgs e)
        {
            if (this.pagePropertiesChanged != null)
            {
                this.pagePropertiesChanged(this, e);
            }
        }

        protected virtual void OnTotalRowCountAvailable(MyPageEventArgs e)
        {
            if (this.totalRowCountAvailable != null)
            {
                this.totalRowCountAvailable(this, e);
            }
        }

        private void FixImageList()
        {
            while (imageCount > 0)
            {
                ImageSavedEvent.Reset();
                ImageSavedEvent.WaitOne(50);
            }
            Cache.IsRunning = false;
            for (int i = this.imageListSmall.Images.Count; i < this.imageListTracker.Count; i++)
            {
                string imageName = String.Format("{0}.png", this.imageListTracker[i]);
                this.imageListSmall.Images.Add(Bitmap.FromFile(Cache.GetPath(imageName)));
            }
        }

        private ListViewItem UpdateRow(Item transItem)
        {
            ListViewItem item = new ListViewItem();

            Cache.IsRunning = true;
            item.ImageIndex = FindImage(transItem);
            item.UseItemStyleForSubItems = false;
            item.Tag = transItem;

            foreach (ColumnInfo column in columns)
            {
                if (column.ColumnSubItemFunction != null) item.SubItems.Add(column.ColumnSubItemFunction(transItem, this));
            }

            return item;
        }

        //Manages the cache.  ListView calls this when it might need a  
        //cache refresh. 
        private void listView_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            //We've gotten a request to refresh the cache. 
            //First check if it's really neccesary. 
            if (myCache != null && e.StartIndex >= firstItem && e.EndIndex <= firstItem + myCache.Length)
            {
                //If the newly requested cache is a subset of the old cache,  
                //no need to rebuild everything, so do nothing. 
                return;
            }

            //Now we need to rebuild the cache.
            firstItem = e.StartIndex;
            int length = e.EndIndex - e.StartIndex + 1; //indexes are inclusive
            myCache = new ListViewItem[length];

            //Fill the cache with the appropriate ListViewItems. 
            for (int i = 0; i < length; i++)
            {
                myCache[i] = UpdateRow(this.itemList[i + firstItem]);
            }
            FixImageList();
        }

        //The basic VirtualMode function.  Dynamically returns a ListViewItem 
        //with the required properties; in this case, the square of the index.
        private void listView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            //Caching is not required but improves performance on large sets. 
            //To leave out caching, don't connect the CacheVirtualItems event  
            //and make sure myCache is null. 

            //check to see if the requested item is currently in the cache 
            if (myCache != null && e.ItemIndex >= firstItem && e.ItemIndex < firstItem + myCache.Length)
            {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                e.Item = myCache[e.ItemIndex - firstItem];
            }
            else if (e.ItemIndex < this.itemList.Count)
            {
                //A cache miss, so create a new ListViewItem and pass it back. 
                e.Item = UpdateRow(this.itemList[e.ItemIndex]);
                FixImageList();
            }
            else
            {   // should not happen
                e.Item = new ListViewItem();
                //e.Item.SubItems.Add(String.Empty);
            }
        }

        private void listView_Click(object sender, EventArgs e)
        {
            Point mousePosition = this.listView.PointToClient(Control.MousePosition);
            ListViewHitTestInfo info = this.listView.HitTest(mousePosition);
            if ((info.Item != null) && (info.SubItem != null) && (info.Item.Tag != null) && (info.SubItem.Tag != null))
            {
                Item item = (Item)info.Item.Tag;
                if (info.SubItem.Tag is Listings)
                {
                    //bool toBuy = (bool)info.SubItem.Tag;
                    //if (toBuy) MessageBox.Show("Buy Listing " + item.Name);
                    //else MessageBox.Show("Sell Listing " + item.Name);
                    this.listView.Invoke((MethodInvoker)delegate
                    {
                        Listings listings = (Listings)info.SubItem.Tag;
                        listings.Visible = false;
                        // Show will stop the app from exiting
                        listings.ShowDialog(this.listView);                        
                    });
                }
                else if (info.SubItem.Tag is int)
                {
                    ThreadPool.QueueUserWorkItem((obj) => 
                    {
                        trader.RenewBuyOrder(item, (int)info.SubItem.Tag);
                        Thread.Sleep(1000);
                        Refresh();
                        //MessageBox.Show(item.Name + " Outbidded!");
                    });
                }
                else if (info.SubItem.Tag is List<ItemBuySellListingItem>)
                {
                    ThreadPool.QueueUserWorkItem((obj) => 
                    {
                        trader.BuyAllRidiculousSellOrders((List<ItemBuySellListingItem>)info.SubItem.Tag, item);
                        Thread.Sleep(1000);
                        Refresh();
                        //MessageBox.Show("All ridiculous under sell orders for " + item.Name + " Bought!");
                    });
                }
                else if (info.SubItem.Tag is long)
                {
                    ThreadPool.QueueUserWorkItem((obj) =>
                    {
                        if (item.IAmSelling) trader.cancelSellOrder(item.Id, (long)info.SubItem.Tag);
                        else trader.cancelBuyOrder(item.Id, (long)info.SubItem.Tag);
                        Thread.Sleep(1000);
                        Refresh();
                    });
                }
                else if (info.SubItem.Tag is gw2dbRecipe)
                {
                    string url = String.Format("http://www.gw2spidy.com/recipe/{0}", ((gw2dbRecipe)(info.SubItem.Tag)).Data_Id);
                    Process.Start(url);
                }
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info =
                this.listView.HitTest(e.Location);
            string url = String.Format("http://www.gw2spidy.com/item/{0}", ((Item)(info.Item.Tag)).Id);
            Process.Start(url);
        }

        private void listView_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = this.listView.HitTest(e.X, e.Y);

            if (mLastItem != info.Item || !this.tooltip.Active)
            {
                if ((info.Item != null) && (info.Item.Tag != null) && (info.Item.ImageIndex < this.imageListSmall.Images.Count))
                {
                    Item transItem = (Item)(info.Item.Tag);
                    this.listView.Tag = this.imageListSmall.Images[info.Item.ImageIndex];
                    this.tooltip.ForeColor = transItem.GetItemColor();
                    this.tooltip.Show(transItem.ToolTipString, this.listView, info.Item.Position.X, info.Item.Position.Y);
                }
                else
                {
                    this.tooltip.RemoveAll();
                }
            }

            mLastItem = info.Item;
        }

        private void listView_MouseLeave(object sender, EventArgs e)
        {
            this.tooltip.RemoveAll();
        }

        private void Refresh()
        {
            this.dataSource.Flush();
            SetPageProperties(this.offset, this.maximumRows, true);
        }

        private void Init(ColumnInfo[] columns)
        {
            lvwColumnSorter.ColumnInfo = columns;

            foreach (ColumnInfo column in columns)
            {
                this.listView.Columns.Add(column.ColumnName);
            }

            this.listView.VirtualMode = true;
            this.listView.VirtualListSize = 0;
            this.listView.RetrieveVirtualItem += listView_RetrieveVirtualItem;
            this.listView.CacheVirtualItems += listView_CacheVirtualItems;

            this.listView.View = View.Details;
            this.listView.ShowItemToolTips = false;
            this.listView.ColumnClick += this.ListView_ColumnClick;
            //this.listView.ListViewItemSorter = lvwColumnSorter;
            this.listView.MouseDoubleClick += listView_MouseDoubleClick;
            this.listView.Click += listView_Click;
            this.tooltip.AutoSize = true;
            this.tooltip.Size = new Size(200, 40);
            this.tooltip.BackColor = Color.FromKnownColor(KnownColor.InactiveCaptionText);
            this.listView.MouseMove += listView_MouseMove;
            this.listView.MouseLeave += listView_MouseLeave;
            Cache.CacheStored += new EventHandler<CacheStoredEventArgs>(CacheStoredEvent);

            this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        // Sort ListView
        private void ListView_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
        {
            this.needReverseList = false;
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                this.needReverseList = true;
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }
            
            // Perform the sort with these new sort options.
            //this.listView.Sort();

            //this.needSortSelf = (columns[e.Column].OrderBy == string.Empty);

            if (columns[e.Column].OrderBy != string.Empty) this.needReverseList = false; // caller would take care of it

            if (itemSortEventHandler != null)
            {
                itemSortEventHandler(this, new ItemSortEventArgs(columns[e.Column].OrderBy, lvwColumnSorter.Order == SortOrder.Descending, e.Column));
            }
            else
            {   // should not happen
                Update(this.itemList, 1, this.itemList.Count, this.maximumRows, true);
            }
        }

        private int FindImage(Item item)
        {
            int retIndex = FindImageIndex(item.Id);
            if (retIndex < 0)
            {
                retIndex = imageListTracker.Count;
                imageListTracker.Add(item.Id);
                Cache.StoreAndRequest(item.ImageUri, item.Id + ".png");
                imageCount++;
            }
            return retIndex;
        }

        private void CacheStoredEvent(object sender, CacheStoredEventArgs e)
        {
            imageCount--;
            if (imageCount == 0) ImageSavedEvent.Set();
        }

        private int FindImageIndex(int id)
        {
            for (int i = 0; i < imageListTracker.Count; i++)
            {
                if (imageListTracker[i] == id)
                {
                    return i;
                }
            }
            return -1;
        }

        #region IMyPageableItemContainer Interface
        public int MaxiumRows
        {
            get
            {
                return this.maximumRows;
            }
        }

        public int StartRowIndex
        {
            get
            {
                return this.offset;
            }
        }

        public void SetPageProperties (int startRowIndex, int maximumRows, bool databind)
        {
            this.dataSource.fetchData(startRowIndex, maximumRows, Update);
        }

        public event EventHandler<MyPageEventArgs> TotalRowCountAvailable
        {
            add
            {
                this.totalRowCountAvailable += value;
            }
            remove
            {
                this.totalRowCountAvailable -= value;
            }
        }
        #endregion

        #region IColumnInfo Interface
        public ListViewEx ListViewLV
        {
            get
            {
                return this.listView;
            }
        }

        public void Alert(string title, string msg)
        {
            if (this.AlertFn != null)
            {
                this.AlertFn(title, msg);
            }
        }
        #endregion
    }
}

