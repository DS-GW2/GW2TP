using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GW2TP
{
    public class SimpleDataPager
    {
        private const int OFFSETINDEX = 0;
        private const int LASTITEMINDEX = 2;
        private const int TOTALINDEX = 4;
        private const int STATUSINDEX = 5;
        private const int REFRESHINDEX = 6;
        private const int FIRSTPAGEINDEX = 7;
        private const int PREVIOUSPAGEINDEX = 8;
        private const int NEXTPAGEINDEX = 9;
        private const int LASTPAGEINDEX = 10;

        private IMyPageableItemContainer pagedControl;
        private StatusStrip statusStrip;
        private int pageSize = 10, totalPages = 1, currentPage = 1, offset = 1, totalRowCount = 10, maximumRows = 10;
        private event EventHandler pageRefreshing;

        public SimpleDataPager(int pageSize, IMyPageableItemContainer pagedControl, StatusStrip statusStrip)
        {
            this.pageSize = pageSize;
            this.pagedControl = pagedControl;
            this.statusStrip = statusStrip;

            Init();
            //UpdateStatusStrip();
        }

        public IMyPageableItemContainer PagedControl
        {
            get
            {
                return this.pagedControl;
            }
            set
            {
                this.pagedControl = value;
            }
        }

        public void OnTotalRowCountAvailable(object sender, MyPageEventArgs e)
        {
            this.offset = e.StartRowIndex;
            this.maximumRows = e.MaxiumRows;
            this.totalRowCount = e.TotalRowCount;
            this.totalPages = this.totalRowCount / this.pageSize;
            if (this.totalPages * this.pageSize != this.totalRowCount)
            {
                this.totalPages = this.totalPages + 1;
            }
            this.currentPage = this.offset / this.pageSize;
            if (this.currentPage * this.pageSize != this.offset)
            {
                this.currentPage = this.currentPage + 1;
            }
            //if (this.totalRowCount > 0) UpdateStatusStrip();
            //else this.statusStrip.Visible = false;
            UpdateStatusStrip();
            this.statusStrip.Items[STATUSINDEX].Text = string.Empty;
        }

        public int StartRowIndex
        {
            get
            {
                return this.offset;
            }
        }

        public int MaximumRows
        {
            get
            {
                return this.maximumRows;
            }
        }

        public int TotalRowCount
        {
            get
            {
                return this.totalRowCount;
            }
        }

        public int PageSize
        {
            get
            {
                return this.pageSize;
            }
            set
            {
                this.pageSize = value;
                this.totalPages = this.totalRowCount / this.pageSize;
                if (this.totalPages * this.pageSize != this.totalRowCount)
                {
                    this.totalPages = this.totalPages + 1;
                }
                this.currentPage = this.offset / this.pageSize;
                if (this.currentPage * this.pageSize != this.offset)
                {
                    this.currentPage = this.currentPage + 1;
                }
                //if (this.totalRowCount > 0) UpdateStatusStrip();
                //else this.statusStrip.Visible = false;
                UpdateStatusStrip();
            }
        }

        public int CurrentPage
        {
            get
            {
                return this.currentPage;
            }
        }

        public int TotalPages
        {
            get
            {
                return this.totalPages;
            }
        }

        public virtual void SetPageProperties(int startRowIndex, int maximumRows, bool databind)
        {
            this.statusStrip.Items[STATUSINDEX].Text = "Working...";
            if (maximumRows == 0) maximumRows = this.pageSize;
            this.pagedControl.SetPageProperties(startRowIndex, maximumRows, databind);
        }

        public void RefreshPage(bool full = true)
        {
            if (full) OnPageRefreshing(new EventArgs());
            //SetPageProperties(this.offset, NumberOfItemsToFetch(this.offset), true);
            SetPageProperties(this.offset, this.pageSize, true);
        }

        public void FirstPage()
        {
            //this.offset = 1;
            //this.currentPage = 1;
            //UpdateStatusStrip();
            //SetPageProperties(1, NumberOfItemsToFetch(1), true);
            SetPageProperties(1, this.pageSize, true);
        }

        public void LastPage()
        {
            //this.offset = (this.pageSize * (this.totalPages - 1)) + 1;
            //this.currentPage = this.totalPages;
            //UpdateStatusStrip();
            int lastPageOffset = (this.pageSize * (this.totalPages - 1)) + 1;
            //SetPageProperties(lastPageOffset, NumberOfItemsToFetch(lastPageOffset), true);
            SetPageProperties(lastPageOffset, this.pageSize, true);
        }

        public void NextPage()
        {
            if (this.currentPage < this.totalPages)
            {
                //this.currentPage = this.currentPage + 1;
                //this.offset = this.offset + this.pageSize;
                //UpdateStatusStrip();
                int nextPageOffset = this.offset + this.pageSize;
                //SetPageProperties(nextPageOffset, NumberOfItemsToFetch(nextPageOffset), true);
                SetPageProperties(nextPageOffset, this.pageSize, true);
            }
        }

        public void PreviousPage()
        {
            if (this.currentPage > 1)
            {
                //this.currentPage = this.currentPage - 1;
                //this.offset = this.offset - this.pageSize;
                //UpdateStatusStrip();
                int previousPageOffset = this.offset - this.pageSize;
                //SetPageProperties(previousPageOffset, NumberOfItemsToFetch(previousPageOffset), true);
                SetPageProperties(previousPageOffset, this.pageSize, true);
            }
        }

        public event EventHandler PageRefreshing
        {
            add
            {
                this.pageRefreshing += value;
            }
            remove
            {
                this.pageRefreshing -= value;
            }
        }

        public void OnPageRefreshing(EventArgs e)
        {
            if (this.pageRefreshing != null)
            {
                this.pageRefreshing(this, e);
            }
        }

        public void Reset()
        {
            this.totalPages = this.currentPage = this.offset = 1;
            this.totalRowCount = this.maximumRows = this.pageSize;

            //this.statusStrip.Visible = false;
        }

        private void Init()
        {
            Reset();

            this.pagedControl.TotalRowCountAvailable += OnTotalRowCountAvailable;
            this.statusStrip.Items[REFRESHINDEX].Click += SimpleDataPagerRefresh_Click;
            this.statusStrip.Items[FIRSTPAGEINDEX].Click += SimpleDataPagerFirst_Click;
            this.statusStrip.Items[PREVIOUSPAGEINDEX].Click += SimpleDataPagerPrevious_Click;
            this.statusStrip.Items[NEXTPAGEINDEX].Click += SimpleDataPagerNext_Click;
            this.statusStrip.Items[LASTPAGEINDEX].Click += SimpleDataPagerLast_Click;
        }

        void SimpleDataPagerRefresh_Click(object sender, EventArgs e)
        {
            RefreshPage();
        }

        void SimpleDataPagerFirst_Click(object sender, EventArgs e)
        {
            FirstPage();
        }

        void SimpleDataPagerPrevious_Click(object sender, EventArgs e)
        {
            PreviousPage();
        }

        void SimpleDataPagerNext_Click(object sender, EventArgs e)
        {
            NextPage();
        }

        void SimpleDataPagerLast_Click(object sender, EventArgs e)
        {
            LastPage();
        }

        private int LastPageItemOffset(int offset)
        {
            int lastPageItem = offset + this.pageSize - 1;
            if (lastPageItem > this.totalRowCount) lastPageItem = this.totalRowCount;
            return lastPageItem;
        }

        private int NumberOfItemsToFetch(int offset)
        {
            return (LastPageItemOffset(offset) - offset + 1);
        }

        private void UpdateStatusStrip()
        {
            this.statusStrip.Visible = true;

            this.statusStrip.Items[OFFSETINDEX].Text = this.offset.ToString();
            this.statusStrip.Items[LASTITEMINDEX].Text = (this.offset + this.maximumRows - 1).ToString();
            this.statusStrip.Items[TOTALINDEX].Text = this.totalRowCount.ToString();

            this.statusStrip.Items[REFRESHINDEX].Enabled = true;
            this.statusStrip.Items[FIRSTPAGEINDEX].Enabled = true;
            this.statusStrip.Items[PREVIOUSPAGEINDEX].Enabled = true;
            this.statusStrip.Items[LASTPAGEINDEX].Enabled = true;
            this.statusStrip.Items[NEXTPAGEINDEX].Enabled = true;

            if (this.currentPage <= 1)
            {
                this.statusStrip.Items[FIRSTPAGEINDEX].Enabled = false;
                this.statusStrip.Items[PREVIOUSPAGEINDEX].Enabled = false;
            }

            if (this.currentPage >= this.totalPages)
            {
                this.statusStrip.Items[LASTPAGEINDEX].Enabled = false;
                this.statusStrip.Items[NEXTPAGEINDEX].Enabled = false;
            }

            if (this.totalPages == 0)
            {
                this.statusStrip.Items[OFFSETINDEX].Visible = this.statusStrip.Items[OFFSETINDEX + 1].Visible = this.statusStrip.Items[LASTITEMINDEX].Visible = 
                    this.statusStrip.Items[LASTITEMINDEX + 1].Visible = this.statusStrip.Items[TOTALINDEX].Visible = false;
            }
            else
            {
                this.statusStrip.Items[OFFSETINDEX].Visible = this.statusStrip.Items[OFFSETINDEX + 1].Visible = this.statusStrip.Items[LASTITEMINDEX].Visible = 
                    this.statusStrip.Items[LASTITEMINDEX + 1].Visible = this.statusStrip.Items[TOTALINDEX].Visible = true;
            }
        }
    }
}
