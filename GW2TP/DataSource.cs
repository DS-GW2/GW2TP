using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2TP
{
    public abstract class DataSource : IDataSource
    {
        protected SearchResult searchResult = null;
        protected SimpleDataPager dataPager = null;
        protected List<Item> cachedResult;

        protected int totalItems = -1;

        protected string orderBy = string.Empty;
        protected bool sortDescending = false, getAllPages = false;
        protected int sortColumn;

        protected virtual void Init()
        {
            this.searchResult.ItemSortEventHandler += searchResult_ItemSortEventHandler;

            this.dataPager.PageRefreshing += dataPager_PageRefreshing;
        }

        protected virtual void searchResult_ItemSortEventHandler(object sender, ItemSortEventArgs e)
        {
            this.orderBy = e.OrderBy;
            this.sortDescending = e.SortDescending;
            this.getAllPages = (e.OrderBy == string.Empty);
            this.sortColumn = e.Column;
            if (this.cachedResult != null && this.totalItems == this.cachedResult.Count && this.getAllPages)
            {
                // we already have all the items
                this.cachedResult = this.searchResult.Sort(this.cachedResult);
                this.dataPager.RefreshPage(false);
                //this.searchResult.Update(this.cachedResult, this.offset, TRANSACTION_PAGE_SIZE, this.totalItems, false);
            }
            else
            {
                this.dataPager.RefreshPage();
            }
        }

        protected virtual void dataPager_PageRefreshing(object sender, EventArgs e)
        {
            this.Flush();
        }

        public virtual void Flush()
        {
            this.cachedResult = null; // Force refresh!
        }

        public abstract void fetchData(int offset, int count, DataReadyFunc OnDataReady);
    }
}
