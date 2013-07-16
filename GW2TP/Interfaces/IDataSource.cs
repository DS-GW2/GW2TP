using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2TP
{
    public delegate void DataReadyFunc(List<Item> list, int offset, int count, int total, bool needToSort);
    public interface IDataSource
    {
        void Flush();

        void fetchData(int offset, int count, DataReadyFunc OnDataReady);
    }
}
