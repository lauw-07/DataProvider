using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLDatabase
{
    public class PriceData
    {
        public int PriceID { get; set; }
        public int InstrumentID { get; set; }
        public DateTime PxDate { get; set; }
        public float OpenPx { get; set; }
        public float ClosePx { get; set; }
        public float HighPx { get; set; }
        public float LowPx { get; set; }
        public int Volume { get; set; }
        
        public PriceData(int priceID, int instrumentID, DateTime pxDate, float openPx, float closePx, float highPx, float lowPx, int volume) {
            PriceID = priceID;
            InstrumentID = instrumentID;
            PxDate = pxDate;
            OpenPx = openPx;
            ClosePx = closePx;
            HighPx = highPx;
            LowPx = lowPx;
            Volume = volume;
        }
        
    }
}
