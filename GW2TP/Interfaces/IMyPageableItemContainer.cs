using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GW2TP
{
    public interface IMyPageableItemContainer
    {
        int MaxiumRows { get; }
        int StartRowIndex { get; }
        void SetPageProperties(int startRowIndex, int maximumRows, bool databind);
        event EventHandler<MyPageEventArgs> TotalRowCountAvailable;
    }
}
