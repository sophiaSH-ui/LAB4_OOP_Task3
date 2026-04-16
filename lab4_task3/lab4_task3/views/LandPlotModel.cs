using System.Collections.Generic;
using System.Windows;

namespace lab4_task3
{
    public class LandPlotModel
    {
        public string OwnerName { get; set; }
        public string Pryznachennya { get; set; }
        public string SoilType { get; set; }
        public string MarketValueFormatted { get; set; }
        public string Location { get; set; }
        public List<Point> Coordinates { get; set; }
    }
}