using System.Windows;
public class LandPlotModel
{
    public int Id { get; set; }
    public string OwnerName { get; set; }
    public int OwnerId { get; set; }       
    public string Pryznachennya { get; set; }
    public string SoilType { get; set; }
    public string GeoFeature { get; set; }  
    public double GroundWater { get; set; } 
    public bool HasRiver { get; set; }      
    public bool IsFlat { get; set; }        
    public bool IsFertile { get; set; }     
    public bool NearForest { get; set; }    
    public bool NearRoad { get; set; }      
    public string MarketValueFormatted { get; set; }
    public string Location { get; set; }
    public List<Point> Coordinates { get; set; }
}