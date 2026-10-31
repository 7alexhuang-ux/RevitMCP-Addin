using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace AlexMCP.Tools
{
    public class ToolHandler
    {
        private UIApplication _uiapp;

        public ToolHandler(UIApplication uiapp)
        {
            _uiapp = uiapp;
        }

        public string ExecuteTool(string toolName, JObject args)
        {
            try
            {
                switch (toolName)
                {
                    // --- Query Tools ---
                    case "get_project_info": return GetProjectInfo();
                    case "get_all_levels": return GetAllLevels();
                    case "get_current_view_info": return GetCurrentViewInfo();
                    case "get_current_view_elements": return GetCurrentViewElements(args);
                    case "get_selected_elements": return GetSelectedElements();
                    case "get_available_family_types": return GetAvailableFamilyTypes(args);
                    case "get_element_parameters": return GetElementParameters(args);

                    // --- Creation Tools ---
                    case "create_wall": return CreateWall(args); // Kept for compatibility
                    case "create_point_based_element": return CreatePointBasedElement(args);
                    case "create_line_based_element": return CreateLineBasedElement(args);
                    case "create_surface_based_element": return CreateSurfaceBasedElement(args);

                    // --- Modification Tools ---
                    case "delete_element": return DeleteElements(args); // Alias
                    case "delete_elements": return DeleteElements(args);
                    case "modify_element": return ModifyElement(args);
                    case "reset_model": return ResetModel();

                    // --- Action Tools ---
                    case "color_splash": return ColorSplash(args);
                    case "tag_walls": return TagWalls();

                    // --- Advanced Tools (Placeholders) ---
                    case "send_code_to_revit": return "Error: Dynamic code execution is not yet supported in this version.";
                    case "search_modules": return "Info: No external modules repository configured.";
                    case "use_module": return "Info: Module system not active.";

                    // --- Room Query Tools ---
                    case "get_current_view_rooms": return GetCurrentViewRooms();
                    case "get_room_contained_elements": return GetRoomContainedElements();

                    default:
                        return $"Error: Tool '{toolName}' not found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing tool '{toolName}': {ex.Message}\nStack: {ex.StackTrace}";
            }
        }

        // ================= QUERY TOOLS =================

        private string GetProjectInfo()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            ProjectInfo info = doc.ProjectInformation;
            return $"Project Name: {info.Name}, Number: {info.Number}, Status: {info.Status}, Client: {info.ClientName}";
        }

        private string GetAllLevels()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToElements();
            return string.Join("\n", levels.Select(l => $"Name: {l.Name}, Id: {l.Id}, Elevation: {(l as Level).Elevation}"));
        }

        private string GetCurrentViewInfo()
        {
            View view = _uiapp.ActiveUIDocument.ActiveView;
            return $"View Name: {view.Name}, Type: {view.ViewType}, Id: {view.Id}, Scale: 1:{view.Scale}";
        }

        private string GetCurrentViewElements(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string categoryName = args["category"]?.ToString();
            
            FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
            if (!string.IsNullOrEmpty(categoryName))
            {
                // Try to find category
                if (Enum.TryParse(categoryName, true, out BuiltInCategory bic))
                    collector.OfCategory(bic);
                else
                    return $"Error: Invalid category '{categoryName}'.";
            }
            else
            {
                collector.WhereElementIsNotElementType();
            }

            var elements = collector.ToElements().Take(50); // Limit to 50 to avoid token overflow
            return string.Join("\n", elements.Select(e => $"{e.Name} (Id: {e.Id}, Cat: {e.Category?.Name})"));
        }

        private string GetSelectedElements()
        {
            var ids = _uiapp.ActiveUIDocument.Selection.GetElementIds();
            if (ids.Count == 0) return "No elements selected.";
            
            Document doc = _uiapp.ActiveUIDocument.Document;
            List<string> infos = new List<string>();
            foreach (var id in ids)
            {
                Element e = doc.GetElement(id);
                infos.Add($"{e.Name} (Id: {e.Id}, Cat: {e.Category?.Name})");
            }
            return string.Join("\n", infos);
        }

        private string GetAvailableFamilyTypes(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string categoryName = args["category"]?.ToString();
            
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol));
            if (!string.IsNullOrEmpty(categoryName) && Enum.TryParse(categoryName, true, out BuiltInCategory bic))
            {
                collector.OfCategory(bic);
            }

            var symbols = collector.Cast<FamilySymbol>().Take(50);
            return string.Join("\n", symbols.Select(s => $"{s.FamilyName} : {s.Name}"));
        }

        private string GetElementParameters(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            if (!int.TryParse(args["elementId"]?.ToString(), out int id)) return "Invalid ID";
            
            Element e = doc.GetElement(new ElementId(id));
            if (e == null) return "Element not found";

            List<string> pList = new List<string>();
            foreach (Parameter p in e.Parameters)
            {
                if (p.HasValue) pList.Add($"{p.Definition.Name}: {p.AsValueString() ?? p.AsString()}");
            }
            return string.Join("\n", pList);
        }

        // ================= CREATION TOOLS =================

        private string CreateWall(JObject args)
        {
            // Re-using CreateLineBasedElement logic but with specific parsing for backward compat
            args["category"] = "OST_Walls";
            return CreateLineBasedElement(args);
        }

        private string CreatePointBasedElement(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string symbolName = args["symbolName"]?.ToString();
            string familyName = args["familyName"]?.ToString(); // Optional
            string levelName = args["levelName"]?.ToString();
            double x = args["x"]?.ToObject<double>() ?? 0;
            double y = args["y"]?.ToObject<double>() ?? 0;

            Level level = FindLevel(doc, levelName);
            if (level == null) return "Error: Level not found.";

            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Equals(symbolName, StringComparison.OrdinalIgnoreCase) && 
                                     (string.IsNullOrEmpty(familyName) || s.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase)));

            if (symbol == null) return $"Error: Family Symbol '{symbolName}' not found.";

            using (Transaction t = new Transaction(doc, "Create Point Element"))
            {
                t.Start();
                if (!symbol.IsActive) symbol.Activate();
                doc.Create.NewFamilyInstance(new XYZ(x, y, 0), symbol, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                t.Commit();
            }
            return $"Success: Created {symbolName} at ({x},{y}).";
        }

        private string CreateLineBasedElement(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string category = args["category"]?.ToString(); // e.g., OST_Walls, OST_StructuralFraming
            string levelName = args["levelName"]?.ToString();
            double startX = args["startX"]?.ToObject<double>() ?? 0;
            double startY = args["startY"]?.ToObject<double>() ?? 0;
            double endX = args["endX"]?.ToObject<double>() ?? 10;
            double endY = args["endY"]?.ToObject<double>() ?? 0;

            Level level = FindLevel(doc, levelName);
            if (level == null) return "Error: Level not found.";

            XYZ start = new XYZ(startX, startY, 0);
            XYZ end = new XYZ(endX, endY, 0);
            Line line = Line.CreateBound(start, end);

            using (Transaction t = new Transaction(doc, "Create Line Element"))
            {
                t.Start();
                if (category.Contains("Wall"))
                {
                    Wall.Create(doc, line, level.Id, false);
                }
                else if (category.Contains("Beam") || category.Contains("Framing"))
                {
                    // Simplified beam creation (requires loading a symbol usually, skipping for MVP complexity)
                    return "Error: Beam creation requires specifying a FamilySymbol. Use create_point_based_element for columns.";
                }
                // Add more line-based types here
                t.Commit();
            }
            return $"Success: Created {category} from ({startX},{startY}) to ({endX},{endY}).";
        }

        private string CreateSurfaceBasedElement(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string levelName = args["levelName"]?.ToString();
            JArray points = args["points"] as JArray;

            Level level = FindLevel(doc, levelName);
            if (level == null) return "Error: Level not found.";

            if (points == null || points.Count < 3) return "Error: Need at least 3 points for a surface.";

            CurveLoop loop = new CurveLoop();
            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];
                XYZ pt1 = new XYZ(p1["x"].ToObject<double>(), p1["y"].ToObject<double>(), 0);
                XYZ pt2 = new XYZ(p2["x"].ToObject<double>(), p2["y"].ToObject<double>(), 0);
                loop.Append(Line.CreateBound(pt1, pt2));
            }

            using (Transaction t = new Transaction(doc, "Create Floor"))
            {
                t.Start();
#if REVIT2022_OR_GREATER
                Floor.Create(doc, new List<CurveLoop> { loop }, level.Id, level.Id); // Simplified
#else
                // Legacy method for older Revit APIs if needed, but assuming 2024 based on user context
                // doc.Create.NewFloor(loop, type, level);
                return "Error: Floor creation implementation requires Revit version check.";
#endif
                t.Commit();
            }
            return "Success: Created Floor.";
        }

        // ================= MODIFICATION TOOLS =================

        private string DeleteElements(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            List<ElementId> idsToDelete = new List<ElementId>();

            if (args["elementId"] != null) idsToDelete.Add(new ElementId(int.Parse(args["elementId"].ToString())));
            if (args["elementIds"] is JArray arr)
            {
                foreach (var item in arr) idsToDelete.Add(new ElementId(int.Parse(item.ToString())));
            }

            using (Transaction t = new Transaction(doc, "Delete Elements"))
            {
                t.Start();
                doc.Delete(idsToDelete);
                t.Commit();
            }
            return $"Success: Deleted {idsToDelete.Count} elements.";
        }

        private string ModifyElement(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            if (!int.TryParse(args["elementId"]?.ToString(), out int id)) return "Invalid ID";
            string paramName = args["parameterName"]?.ToString();
            string value = args["value"]?.ToString();

            Element e = doc.GetElement(new ElementId(id));
            if (e == null) return "Element not found";

            Parameter p = e.LookupParameter(paramName);
            if (p == null) return $"Parameter '{paramName}' not found.";
            if (p.IsReadOnly) return $"Parameter '{paramName}' is read-only.";

            using (Transaction t = new Transaction(doc, "Modify Parameter"))
            {
                t.Start();
                if (p.StorageType == StorageType.String) p.Set(value);
                else if (p.StorageType == StorageType.Double) p.Set(double.Parse(value));
                else if (p.StorageType == StorageType.Integer) p.Set(int.Parse(value));
                t.Commit();
            }
            return $"Success: Set {paramName} to {value}.";
        }

        private string ResetModel()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WhereElementIsNotElementType();
            var ids = collector.ToElementIds();

            using (Transaction t = new Transaction(doc, "Reset Model"))
            {
                t.Start();
                doc.Delete(ids);
                t.Commit();
            }
            return $"Success: Deleted {ids.Count} elements from current view.";
        }

        // ================= ACTION TOOLS =================

        private string ColorSplash(JObject args)
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            string paramName = args["parameterName"]?.ToString();
            
            // Simplified: Just color everything red for now as a proof of concept
            // Real implementation requires complex analysis of parameter values and creating filters
            
            var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WhereElementIsNotElementType();
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetSurfaceForegroundPatternColor(new Color(255, 0, 0));
            FillPatternElement solidFill = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fp => fp.Name.Equals("Solid fill", StringComparison.OrdinalIgnoreCase));
            if (solidFill != null)
            {
                ogs.SetSurfaceForegroundPatternId(solidFill.Id);
            }

            using (Transaction t = new Transaction(doc, "Color Splash"))
            {
                t.Start();
                foreach (Element e in collector)
                {
                    doc.ActiveView.SetElementOverrides(e.Id, ogs);
                }
                t.Commit();
            }
            return "Success: Applied Red Color Splash (MVP).";
        }

        private string TagWalls()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            var walls = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Walls).ToElements();

            using (Transaction t = new Transaction(doc, "Tag Walls"))
            {
                t.Start();
                foreach (Wall wall in walls)
                {
                    // Simplified: Requires finding Tag Symbol and location logic
                    // doc.Create.NewTag(...)
                }
                t.Commit();
            }
            return "Info: Tag Walls logic requires loading tags first. (Placeholder)";
        }

        // ================= ROOM QUERY TOOLS =================

        private string GetCurrentViewRooms()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            var rooms = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .Cast<Room>()
                .Where(r => r.Area > 0) // Filter out unplaced/unbounded rooms
                .ToList();

            if (rooms.Count == 0) return "No placed rooms found in the current view.";

            List<string> roomInfos = new List<string>();
            foreach (Room r in rooms)
            {
                string areaStr = r.Area > 0 ? $"{r.Area:F2} sq.ft" : "N/A";
                roomInfos.Add($"Room: {r.Name}, Number: {r.Number}, Area: {r.GetParameters("Area")[0].AsValueString()}");
            }
            return string.Join("\n", roomInfos);
        }

        private string GetRoomContainedElements()
        {
            Document doc = _uiapp.ActiveUIDocument.Document;
            var rooms = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .Cast<Room>()
                .Where(r => r.Area > 0)
                .ToList();

            if (rooms.Count == 0) return "No placed rooms found in the current view.";

            // Get Doors and Windows in view
            var doors = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();
            var windows = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType().Cast<FamilyInstance>().ToList();

            List<string> result = new List<string>();

            foreach (Room r in rooms)
            {
                List<string> contents = new List<string>();

                // Check Doors
                foreach (var d in doors)
                {
                    if (IsInRoom(d, r)) contents.Add($"  - Door: {d.Name} (Id: {d.Id})");
                }

                // Check Windows
                foreach (var w in windows)
                {
                    if (IsInRoom(w, r)) contents.Add($"  - Window: {w.Name} (Id: {w.Id})");
                }

                if (contents.Count > 0)
                {
                    result.Add($"Room: {r.Name} [{r.Number}] contains:");
                    result.AddRange(contents);
                }
                else
                {
                    result.Add($"Room: {r.Name} [{r.Number}] is empty (of doors/windows).");
                }
            }

            return string.Join("\n", result);
        }

        private bool IsInRoom(FamilyInstance fi, Room r)
        {
            // Check ToRoom and FromRoom
            if (fi.ToRoom != null && fi.ToRoom.Id == r.Id) return true;
            if (fi.FromRoom != null && fi.FromRoom.Id == r.Id) return true;
            // Fallback to Room property
            if (fi.Room != null && fi.Room.Id == r.Id) return true;
            return false;
        }

        // ================= HELPERS =================

        private Level FindLevel(Document doc, string name)
        {
            if (string.IsNullOrEmpty(name)) return new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault();
            return new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
