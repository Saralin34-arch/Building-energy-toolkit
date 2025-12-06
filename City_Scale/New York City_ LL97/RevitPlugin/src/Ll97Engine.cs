using System.Collections.Generic;
using System.Linq;
using System;

namespace CodeToDesign.LL97
{
    public static class Ll97Engine
    {
        // Existing caps table
        private static readonly List<Ll97Caps> Caps = new List<Ll97Caps>
        {
            new Ll97Caps
            {
                GroupId = "B",
                Description = "Office / Mercantile",
                Cap2024_2029 = 0.00846,
                Cap2030_2034 = 0.00453
            }
            // you can add more groups here later
        };

        public static Ll97Result Evaluate(BuildingContext ctx)
        {
            Ll97Caps caps = Caps.First(c => c.GroupId == ctx.GroupId);

            double capPerSf = (ctx.TargetYear <= 2029)
                ? caps.Cap2024_2029
                : caps.Cap2030_2034;

            double capTotal = capPerSf * ctx.FloorArea;      // tCO2e/yr
            double diff = ctx.AnnualEmissions - capTotal;
            double fine = (diff > 0) ? diff * 268.0 : 0.0;

            string status =
                (diff <= 0) ? "PASS" :
                (diff <= capTotal * 0.10) ? "AT RISK" :
                "FAIL";

            return new Ll97Result
            {
                Cap = capTotal,
                Emissions = ctx.AnnualEmissions,
                Difference = diff,
                Fine = fine,
                Status = status
            };
        }

        // ---------- NEW: Emissions from rooms ----------

        // Approximate emission factors (kg CO2 per kBtu)
        // Natural gas ~0.053 kg/kBtu, US-average electricity ~0.108 kg/kBtu.
        private static double GetEmissionFactorKgPerKbtu(string fuelType)
        {
            if (String.IsNullOrWhiteSpace(fuelType))
                return 0.053; // default to gas

            string key = fuelType.Trim().ToLowerInvariant();

            switch (key)
            {
                case "gas":
                case "natural gas":
                    return 0.053;

                case "electric":
                case "electricity":
                    return 0.108;

                case "steam":
                    return 0.070;   // rough placeholder

                default:
                    return 0.053;   // fallback
            }
        }

        public static double ComputeEmissionsFromRooms(
            IEnumerable<RoomEmissionContext> rooms,
            out Dictionary<string, double> byFuelType)
        {
            byFuelType = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            double totalTons = 0.0;

            foreach (RoomEmissionContext r in rooms)
            {
                double factorKgPerKbtu = GetEmissionFactorKgPerKbtu(r.FuelType);

                // kg CO2 = Area * EUI * factor
                double kg = r.AreaFt2 * r.EuiKbtuPerFt2Yr * factorKgPerKbtu;
                double tons = kg / 1000.0;  // tCO2e

                string key = String.IsNullOrWhiteSpace(r.FuelType)
                    ? "Unknown"
                    : r.FuelType.Trim();

                if (!byFuelType.ContainsKey(key))
                    byFuelType[key] = 0.0;

                byFuelType[key] += tons;
                totalTons += tons;
            }

            return totalTons;
        }
    }
}
