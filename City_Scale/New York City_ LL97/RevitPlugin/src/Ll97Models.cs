namespace CodeToDesign.LL97
{
    public class Ll97Caps
    {
        public string GroupId { get; set; }
        public string Description { get; set; }
        public double Cap2024_2029 { get; set; }
        public double Cap2030_2034 { get; set; }
    }

    public class BuildingContext
    {
        public string GroupId { get; set; }
        public double FloorArea { get; set; }
        public double AnnualEmissions { get; set; }
        public int TargetYear { get; set; }
    }

    public class Ll97Result
    {
        public double Cap { get; set; }
        public double Emissions { get; set; }
        public double Difference { get; set; }
        public double Fine { get; set; }
        public string Status { get; set; }
    }

    public class RoomEmissionContext
    {
        public double AreaFt2 { get; set; }             // Revit internal (ft²)
        public double EuiKbtuPerFt2Yr { get; set; }     // kBtu/ft²·yr
        public string FuelType { get; set; }            // "Gas", "Electric", etc.
    }
}
