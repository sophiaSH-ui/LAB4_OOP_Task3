using System.Windows;
public class LandPlotModel
{
    public int Id { get; set; }
    public string OwnerName { get; set; }
    public int OwnerId { get; set; }       
    public string Pryznachennya { get; set; }
    public string SoilType { get; set; }
    public double GroundWater { get; set; }   
    public string MarketValueFormatted { get; set; }
    public string Location { get; set; }
    public List<Point> Coordinates { get; set; }
}