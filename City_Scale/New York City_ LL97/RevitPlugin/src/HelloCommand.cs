using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;   // for Room
using Autodesk.Revit.UI;

namespace CodeToDesign.LL97
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class HelloCommand : IExternalCommand
    {
        // --------------------- ROOM-BASED AREA --------------------- //

        // Sum all placed room areas in the model (ft² in Revit internal units)
        private double GetTotalRoomArea(Document doc)
        {
            double total = 0.0;

            FilteredElementCollector col = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (Element e in col)
            {
                Room room = e as Room;
                if (room == null) continue;

                if (room.Area > 0)
                {
                    total += room.Area;   // Revit internal = ft²
                }
            }

            return total;
        }

        // --------------------- PROJECT INFO HELPER ----------------- //

        // Generic helper to read a double from Project Information
        private double GetProjectDouble(Document doc, string paramName)
        {
            ProjectInfo projInfo = doc.ProjectInformation;
            Parameter param = projInfo.LookupParameter(paramName);

            if (param != null && param.HasValue)
                return param.AsDouble();   // works for Number / Area project params

            return 0.0;
        }

        // --------------------- LEVEL-BASED GROSS AREA -------------- //

        // 1) Find building footprint from FLOORS on the lowest level
        private double GetFootprintAreaFromFloors(Document doc)
        {
            // Find lowest level
            FilteredElementCollector levelCol = new FilteredElementCollector(doc)
                .OfClass(typeof(Level));

            Level lowestLevel = levelCol
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .FirstOrDefault();

            if (lowestLevel == null)
                return 0.0;

            // Sum areas of floors on that level
            double total = 0.0;

            FilteredElementCollector floorCol = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType();

            foreach (Floor f in floorCol)
            {
                // Only floors hosted on the lowest level
                if (f.LevelId != lowestLevel.Id) continue;

                Parameter areaParam = f.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null && areaParam.HasValue)
                {
                    total += areaParam.AsDouble();   // ft²
                }
            }

            return total;
        }

        // 2) Count above-grade levels (very simple heuristic)
        private int GetAboveGradeLevelCount(Document doc)
        {
            FilteredElementCollector levelCol = new FilteredElementCollector(doc)
                .OfClass(typeof(Level));

            List<Level> levels = levelCol.Cast<Level>().ToList();
            if (levels.Count == 0)
                return 0;

            // Use the lowest level elevation as "ground"
            double minElev = levels.Min(l => l.Elevation);
            int count = 0;

            foreach (Level lvl in levels)
            {
                // count levels that are at or above the lowest level
                if (lvl.Elevation >= minElev - 0.0001)   // small tolerance
                    count++;
            }

            return count;
        }

        // 3) Gross Building Area = footprint × number of above-grade levels
        private double GetGrossBuildingArea(Document doc)
        {
            double footprintFt2 = GetFootprintAreaFromFloors(doc);
            int levelCount = GetAboveGradeLevelCount(doc);

            if (footprintFt2 <= 0 || levelCount <= 0)
                return 0.0;

            return footprintFt2 * levelCount;
        }

        // --------------------- MAIN COMMAND ------------------------ //

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            ProjectInfo projInfo = doc.ProjectInformation;

            // ---------- 1. AREA: Rooms first, then Gross Building Area ----------

            double areaFt2 = 0.0;
            string areaSource = "";

            // Try ROOM-based area first
            double roomArea = GetTotalRoomArea(doc);

            if (roomArea > 0)
            {
                areaFt2 = roomArea;
                areaSource = "Rooms (sum of all placed room areas)";
            }
            else
            {
                // Fallback: LEVEL-based Gross Building Area
                double gba = GetGrossBuildingArea(doc);

                if (gba > 0)
                {
                    areaFt2 = gba;
                    areaSource = "Gross Building Area (floors × above-grade levels)";
                }
                else
                {
                    Autodesk.Revit.UI.TaskDialog.Show(
                        "LL97 Assistant",
                        "Could not determine building area.\n" +
                        "Either place Rooms OR create floors + levels\n" +
                        "so the tool can approximate Gross Building Area.");
                    return Result.Failed;
                }
            }

            // ---------- 2. DEFAULT EUI + FUEL FROM PROJECT INFO ----------

            double defaultEui = GetProjectDouble(doc, "LL97 Default EUI");  // kBtu/ft²·yr
            if (defaultEui <= 0)
                defaultEui = 50.0;  // simple fallback

            string defaultFuel = "Gas";
            Parameter projFuelParam = projInfo.LookupParameter("LL97 Default Fuel");
            if (projFuelParam != null && projFuelParam.HasValue)
            {
                string v = projFuelParam.AsString();
                if (!string.IsNullOrWhiteSpace(v))
                    defaultFuel = v;
            }

            // ---------- 3. BUILD ROOM EMISSION CONTEXTS ----------

            List<RoomEmissionContext> roomContexts = new List<RoomEmissionContext>();

            FilteredElementCollector roomCol = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (Element e in roomCol)
            {
                Room room = e as Room;
                if (room == null) continue;
                if (room.Area <= 0) continue;

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

                RoomEmissionContext rc = new RoomEmissionContext
                {
                    AreaFt2 = room.Area,
                    EuiKbtuPerFt2Yr = eui,
                    FuelType = fuel
                };

                roomContexts.Add(rc);
            }

            // ---------- 4. COMPUTE EMISSIONS USING ENGINE ----------

            Dictionary<string, double> byFuel;
            double emissionsTons;

            if (roomContexts.Count > 0)
            {
                // Room-based emissions
                emissionsTons = Ll97Engine.ComputeEmissionsFromRooms(roomContexts, out byFuel);
            }
            else
            {
                // No rooms: approximate from total area + default EUI
                RoomEmissionContext rc = new RoomEmissionContext
                {
                    AreaFt2 = areaFt2,
                    EuiKbtuPerFt2Yr = defaultEui,
                    FuelType = defaultFuel
                };

                emissionsTons = Ll97Engine.ComputeEmissionsFromRooms(
                    new RoomEmissionContext[] { rc },
                    out byFuel);
            }

            // ---------- 5. WRITE AREA + EMISSIONS BACK TO PROJECT INFO ----------

            using (Transaction t = new Transaction(doc, "Sync LL97 Data"))
            {
                t.Start();

                Parameter areaParam = projInfo.LookupParameter("LL97 Floor Area");
                if (areaParam != null && !areaParam.IsReadOnly)
                {
                    areaParam.Set(areaFt2);    // internal units = ft²
                }

                Parameter emParam = projInfo.LookupParameter("LL97 Emissions");
                if (emParam != null && !emParam.IsReadOnly)
                {
                    emParam.Set(emissionsTons);   // tCO2e/yr
                }

                t.Commit();
            }

            // ---------- 6. SIMPLE CAP / FINE LOGIC (PLACEHOLDER) ----------

            // For now we just use a simple placeholder logic.
            // You can replace this block later with real LL97Engine cap/fine methods.

            double cap = 939.0;                       // example cap tCO2e/yr
            double emissions = emissionsTons;         // total emissions
            double difference = cap - emissions;      // positive = below cap
            double fine = difference < 0
                ? Math.Abs(difference) * 268.0        // example $/tCO2e
                : 0.0;
            string status = difference >= 0 ? "PASS" : "FAIL";

            var res = new Ll97Result
            {
                Cap = cap,
                Emissions = emissions,
                Difference = difference,
                Fine = fine,
                Status = status
            };

            // ---------- 7. PUSH DATA INTO DOCKABLE PANEL & SHOW IT ----------

            Ll97DockablePane.PanelInstance?.LoadData(
                areaSource: areaSource,
                areaFt2: areaFt2,
                eui: defaultEui,
                res: res,
                byFuel: byFuel);

            DockablePane pane = uiapp.GetDockablePane(Ll97DockablePane.PaneId);
            if (!pane.IsShown())
            {
                pane.Show();
            }

            return Result.Succeeded;
        }
    }
}
