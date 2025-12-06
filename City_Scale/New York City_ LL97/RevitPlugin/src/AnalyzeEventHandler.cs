using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace CodeToDesign.LL97
{
    /// <summary>
    /// External event handler that runs the LL97 analysis.
    /// This allows us to call Revit API from a WPF button click.
    /// </summary>
    public class AnalyzeEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                Document doc = uiapp.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("LL97", "No active document.");
                    return;
                }

                RunAnalysis(uiapp, doc);
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("LL97 Error", ex.Message);
            }
        }

        public string GetName() => "LL97 Analyze Handler";

        private void RunAnalysis(UIApplication uiapp, Document doc)
        {
            ProjectInfo projInfo = doc.ProjectInformation;

            // ---------- 1. DETERMINE BUILDING AREA ----------
            double areaFt2 = 0.0;
            string areaSource = "";

            double roomArea = GetTotalRoomArea(doc);
            if (roomArea > 0)
            {
                areaFt2 = roomArea;
                areaSource = "Placed rooms";
            }
            else
            {
                double gba = GetGrossBuildingArea(doc);
                if (gba > 0)
                {
                    areaFt2 = gba;
                    areaSource = "Gross Building Area (floors × levels)";
                }
                else
                {
                    Autodesk.Revit.UI.TaskDialog.Show(
                        "LL97 Assistant",
                        "Could not determine building area.\n" +
                        "Either place Rooms OR create floors + levels.");
                    return;
                }
            }

            // ---------- 2. DEFAULT EUI + FUEL ----------
            // First check Project Info parameter, then use panel's calculated EUI
            double defaultEui = GetProjectDouble(doc, "LL97 Default EUI");
            if (defaultEui <= 0)
                defaultEui = Ll97Panel.CurrentEui; // Use EUI from settings panel

            // Get fuel type: first check Project Info, then use panel settings
            string defaultFuel = Ll97Panel.CurrentFuelType; // Default from settings panel
            Parameter projFuelParam = projInfo.LookupParameter("LL97 Default Fuel");
            if (projFuelParam != null && projFuelParam.HasValue)
            {
                string v = projFuelParam.AsString();
                if (!string.IsNullOrWhiteSpace(v))
                    defaultFuel = v;
            }
            
            // Handle "Mixed" fuel type - split 60% Gas, 40% Electric
            bool isMixedFuel = defaultFuel.Contains("Mixed");

            // ---------- 3. BUILD ROOM EMISSION CONTEXTS ----------
            List<RoomEmissionContext> roomContexts = new List<RoomEmissionContext>();

            FilteredElementCollector roomCol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (Element e in roomCol)
            {
                Room room = e as Room;
                if (room == null || room.Area <= 0) continue;

                double eui = defaultEui;
                string fuel = defaultFuel;

                Parameter roomEuiParam = room.LookupParameter("LL97 EUI");
                if (roomEuiParam != null && roomEuiParam.HasValue)
                    eui = roomEuiParam.AsDouble();

                Parameter roomFuelParam = room.LookupParameter("LL97 Fuel");
                if (roomFuelParam != null && roomFuelParam.HasValue)
                {
                    string fv = roomFuelParam.AsString();
                    if (!string.IsNullOrWhiteSpace(fv))
                        fuel = fv;
                }

                roomContexts.Add(new RoomEmissionContext
                {
                    AreaFt2 = room.Area,
                    EuiKbtuPerFt2Yr = eui,
                    FuelType = fuel
                });
            }

            // ---------- 4. COMPUTE EMISSIONS ----------
            Dictionary<string, double> byFuel;
            double emissionsTons;

            if (roomContexts.Count > 0)
            {
                emissionsTons = Ll97Engine.ComputeEmissionsFromRooms(roomContexts, out byFuel);
            }
            else
            {
                // Handle mixed fuel type by splitting into Gas (60%) and Electric (40%)
                if (isMixedFuel)
                {
                    var contexts = new List<RoomEmissionContext>
                    {
                        new RoomEmissionContext
                        {
                            AreaFt2 = areaFt2 * 0.6, // 60% Gas
                            EuiKbtuPerFt2Yr = defaultEui,
                            FuelType = "Gas"
                        },
                        new RoomEmissionContext
                        {
                            AreaFt2 = areaFt2 * 0.4, // 40% Electric
                            EuiKbtuPerFt2Yr = defaultEui,
                            FuelType = "Electric"
                        }
                    };
                    emissionsTons = Ll97Engine.ComputeEmissionsFromRooms(contexts, out byFuel);
                }
                else
                {
                    var rc = new RoomEmissionContext
                    {
                        AreaFt2 = areaFt2,
                        EuiKbtuPerFt2Yr = defaultEui,
                        FuelType = defaultFuel
                    };
                    emissionsTons = Ll97Engine.ComputeEmissionsFromRooms(new[] { rc }, out byFuel);
                }
            }

            // ---------- 5. WRITE BACK TO PROJECT INFO ----------
            using (Transaction t = new Transaction(doc, "Sync LL97 Data"))
            {
                t.Start();

                Parameter areaParam = projInfo.LookupParameter("LL97 Floor Area");
                if (areaParam != null && !areaParam.IsReadOnly)
                    areaParam.Set(areaFt2);

                Parameter emParam = projInfo.LookupParameter("LL97 Emissions");
                if (emParam != null && !emParam.IsReadOnly)
                    emParam.Set(emissionsTons);

                t.Commit();
            }

            // ---------- 6. CAP / FINE LOGIC ----------
            // LL97 caps by occupancy group (using Office/Group B as default)
            // 2024-2029: 0.00846 tCO₂e/ft²/yr
            // 2030+: 0.00453 tCO₂e/ft²/yr
            int currentYear = DateTime.Now.Year;
            double capPerSf = (currentYear <= 2029) ? 0.00846 : 0.00453;
            double cap = capPerSf * areaFt2;  // Total cap based on building area
            
            double difference = cap - emissionsTons;
            double fine = difference < 0 ? Math.Abs(difference) * 268.0 : 0.0;
            string status = difference >= 0 ? "PASS" : "FAIL";

            var res = new Ll97Result
            {
                Cap = cap,
                Emissions = emissionsTons,
                Difference = difference,
                Fine = fine,
                Status = status
            };

            // ---------- 7. UPDATE PANEL ----------
            Ll97DockablePane.PanelInstance?.LoadData(
                areaSource: areaSource,
                areaFt2: areaFt2,
                eui: defaultEui,
                res: res,
                byFuel: byFuel);
        }

        // Helper methods
        private double GetTotalRoomArea(Document doc)
        {
            double total = 0.0;
            var col = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (Element e in col)
            {
                Room room = e as Room;
                if (room != null && room.Area > 0)
                    total += room.Area;
            }
            return total;
        }

        private double GetProjectDouble(Document doc, string paramName)
        {
            Parameter param = doc.ProjectInformation.LookupParameter(paramName);
            if (param != null && param.HasValue)
                return param.AsDouble();
            return 0.0;
        }

        private double GetGrossBuildingArea(Document doc)
        {
            double footprint = GetFootprintAreaFromFloors(doc);
            int aboveGradeLevels = CountAboveGradeLevels(doc);
            return footprint * aboveGradeLevels;
        }

        private double GetFootprintAreaFromFloors(Document doc)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0) return 0.0;

            Level lowestLevel = levels.First();
            double total = 0.0;

            var floors = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType()
                .Cast<Floor>();

            foreach (Floor f in floors)
            {
                Parameter levelParam = f.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                if (levelParam != null)
                {
                    ElementId lvlId = levelParam.AsElementId();
                    if (lvlId == lowestLevel.Id)
                    {
                        Parameter areaParam = f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                        if (areaParam != null)
                            total += areaParam.AsDouble();
                    }
                }
            }
            return total;
        }

        private int CountAboveGradeLevels(Document doc)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(l => l.Elevation >= 0)
                .ToList();

            return Math.Max(levels.Count, 1);
        }
    }
}

