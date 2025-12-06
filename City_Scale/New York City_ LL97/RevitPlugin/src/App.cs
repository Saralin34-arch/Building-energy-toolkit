using System;
using Autodesk.Revit.UI;

namespace CodeToDesign.LL97
{
    public class App : IExternalApplication
    {
        // Single source of truth for the dockable pane ID
        public static readonly DockablePaneId Ll97PaneId =
            new DockablePaneId(new Guid("6F417B68-0F9C-4C7A-8A2D-4D36B6C8A9C3"));

        // External event for running analysis from WPF button
        public static ExternalEvent AnalyzeEvent { get; private set; }
        public static AnalyzeEventHandler AnalyzeHandler { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            // Register the dockable pane
            var provider = new Ll97DockablePane();
            application.RegisterDockablePane(
                Ll97PaneId,
                "LL97 Assistant",
                provider);

            // Create the external event for analysis
            AnalyzeHandler = new AnalyzeEventHandler();
            AnalyzeEvent = ExternalEvent.Create(AnalyzeHandler);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
