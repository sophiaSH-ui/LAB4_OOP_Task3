using System.Collections.Generic;
using System.Windows;

namespace lab4_task3
{
    public static class TestDataStore
    {
        public static List<LandPlotModel> GetTestPlots()
        {
            return new List<LandPlotModel>
            {
                new LandPlotModel
                {
                    OwnerName = "Іваненко І.І.",
                    Pryznachennya = "Під забудову",
                    SoilType = "Чорнозем",
                    MarketValueFormatted = "550 000 грн",
                    Location = "м. Черкаси",
                    Coordinates = new List<Point>
                    {
                        new Point(100, 100),
                        new Point(350, 80),
                        new Point(450, 250),
                        new Point(200, 350),
                        new Point(50, 200)
                    }
                },
                new LandPlotModel
                {
                    OwnerName = "Петренко П.П.",
                    Pryznachennya = "СГ призначення",
                    SoilType = "Суглинок",
                    MarketValueFormatted = "320 000 грн",
                    Location = "Черкаська обл., с. Хутори",
                    Coordinates = new List<Point>
                    {
                        new Point(50, 50),
                        new Point(250, 50),
                        new Point(250, 150),
                        new Point(50, 150)
                    }
                }
            };
        }
    }
}