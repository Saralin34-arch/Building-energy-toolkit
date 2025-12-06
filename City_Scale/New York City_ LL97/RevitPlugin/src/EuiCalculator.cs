using System;
using System.Collections.Generic;

namespace CodeToDesign.LL97
{
    /// <summary>
    /// Calculates EUI based on multiple building factors
    /// </summary>
    public static class EuiCalculator
    {
        // Base EUI values by building type (kBtu/ft²/yr)
        private static readonly Dictionary<string, double> BaseEuiByType = new Dictionary<string, double>
        {
            { "Office", 75 },
            { "Residential (Multifamily)", 60 },
            { "Retail", 65 },
            { "Hotel", 110 },
            { "Hospital/Healthcare", 225 },
            { "School/Education", 75 },
            { "Warehouse", 30 },
            { "Restaurant", 300 },
            { "Data Center", 400 },
            { "Mixed Use", 85 }
        };

        // Age adjustment multipliers
        private static readonly Dictionary<string, double> AgeMultiplier = new Dictionary<string, double>
        {
            { "Pre-1980", 1.35 },       // Older buildings use ~35% more
            { "1980-2000", 1.15 },      // ~15% more
            { "2000-2015", 1.0 },       // Baseline
            { "Post-2015", 0.85 }       // Newer buildings ~15% more efficient
        };

        // Operating hours adjustment
        private static readonly Dictionary<string, double> HoursMultiplier = new Dictionary<string, double>
        {
            { "Standard (40-50 hrs/wk)", 1.0 },
            { "Extended (60-80 hrs/wk)", 1.25 },
            { "24/7 Operation", 1.6 }
        };

        // Occupancy density adjustment
        private static readonly Dictionary<string, double> OccupancyMultiplier = new Dictionary<string, double>
        {
            { "Low (<1 person/200 ft²)", 0.9 },
            { "Medium (1 person/150-200 ft²)", 1.0 },
            { "High (>1 person/100 ft²)", 1.15 }
        };

        // HVAC efficiency adjustment
        private static readonly Dictionary<string, double> HvacMultiplier = new Dictionary<string, double>
        {
            { "High Efficiency (New)", 0.8 },
            { "Standard Efficiency", 1.0 },
            { "Old/Inefficient", 1.3 },
            { "Unknown", 1.0 }
        };

        // Window quality adjustment
        private static readonly Dictionary<string, double> WindowMultiplier = new Dictionary<string, double>
        {
            { "Triple-pane/High Performance", 0.9 },
            { "Double-pane Low-E", 0.95 },
            { "Standard Double-pane", 1.0 },
            { "Single-pane", 1.15 },
            { "Unknown", 1.0 }
        };

        // Insulation quality adjustment
        private static readonly Dictionary<string, double> InsulationMultiplier = new Dictionary<string, double>
        {
            { "High (R-30+)", 0.85 },
            { "Standard (R-13 to R-30)", 1.0 },
            { "Low (<R-13)", 1.2 },
            { "Unknown", 1.0 }
        };

        /// <summary>
        /// Get list of building types for dropdown
        /// </summary>
        public static List<string> GetBuildingTypes()
        {
            return new List<string>(BaseEuiByType.Keys);
        }

        /// <summary>
        /// Get list of building ages for dropdown
        /// </summary>
        public static List<string> GetBuildingAges()
        {
            return new List<string>(AgeMultiplier.Keys);
        }

        /// <summary>
        /// Get list of operating hours options for dropdown
        /// </summary>
        public static List<string> GetOperatingHours()
        {
            return new List<string>(HoursMultiplier.Keys);
        }

        /// <summary>
        /// Get list of occupancy levels for dropdown
        /// </summary>
        public static List<string> GetOccupancyLevels()
        {
            return new List<string>(OccupancyMultiplier.Keys);
        }

        /// <summary>
        /// Get list of HVAC efficiency options for dropdown
        /// </summary>
        public static List<string> GetHvacOptions()
        {
            return new List<string>(HvacMultiplier.Keys);
        }

        /// <summary>
        /// Get list of window quality options for dropdown
        /// </summary>
        public static List<string> GetWindowOptions()
        {
            return new List<string>(WindowMultiplier.Keys);
        }

        /// <summary>
        /// Get list of insulation quality options for dropdown
        /// </summary>
        public static List<string> GetInsulationOptions()
        {
            return new List<string>(InsulationMultiplier.Keys);
        }

        /// <summary>
        /// Calculate EUI based on all factors
        /// </summary>
        public static EuiResult CalculateEui(
            string buildingType,
            string buildingAge,
            string operatingHours,
            string occupancy,
            string hvacEfficiency,
            string windowQuality,
            string insulationQuality)
        {
            // Get base EUI for building type
            double baseEui = BaseEuiByType.ContainsKey(buildingType) 
                ? BaseEuiByType[buildingType] 
                : 75.0; // Default to office

            // Apply multipliers
            double ageMultiplier = AgeMultiplier.ContainsKey(buildingAge) 
                ? AgeMultiplier[buildingAge] : 1.0;
            
            double hoursMultiplier = HoursMultiplier.ContainsKey(operatingHours) 
                ? HoursMultiplier[operatingHours] : 1.0;
            
            double occMultiplier = OccupancyMultiplier.ContainsKey(occupancy) 
                ? OccupancyMultiplier[occupancy] : 1.0;
            
            double hvacMult = HvacMultiplier.ContainsKey(hvacEfficiency) 
                ? HvacMultiplier[hvacEfficiency] : 1.0;
            
            double windowMult = WindowMultiplier.ContainsKey(windowQuality) 
                ? WindowMultiplier[windowQuality] : 1.0;
            
            double insMult = InsulationMultiplier.ContainsKey(insulationQuality) 
                ? InsulationMultiplier[insulationQuality] : 1.0;

            // Calculate final EUI
            double calculatedEui = baseEui * ageMultiplier * hoursMultiplier * occMultiplier 
                                   * hvacMult * windowMult * insMult;

            // Round to 1 decimal place
            calculatedEui = Math.Round(calculatedEui, 1);

            return new EuiResult
            {
                BaseEui = baseEui,
                CalculatedEui = calculatedEui,
                AgeMultiplier = ageMultiplier,
                HoursMultiplier = hoursMultiplier,
                OccupancyMultiplier = occMultiplier,
                HvacMultiplier = hvacMult,
                WindowMultiplier = windowMult,
                InsulationMultiplier = insMult
            };
        }
    }

    /// <summary>
    /// Result of EUI calculation with breakdown
    /// </summary>
    public class EuiResult
    {
        public double BaseEui { get; set; }
        public double CalculatedEui { get; set; }
        public double AgeMultiplier { get; set; }
        public double HoursMultiplier { get; set; }
        public double OccupancyMultiplier { get; set; }
        public double HvacMultiplier { get; set; }
        public double WindowMultiplier { get; set; }
        public double InsulationMultiplier { get; set; }
    }
}

