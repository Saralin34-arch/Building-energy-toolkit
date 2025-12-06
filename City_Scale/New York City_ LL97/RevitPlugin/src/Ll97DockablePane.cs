using Autodesk.Revit.UI;
using System;

namespace CodeToDesign.LL97
{
    internal class Ll97DockablePane : IDockablePaneProvider
    {
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(new Guid("6F417B68-0F9C-4C7A-8A2D-4D36B6C8A9C3"));

        private static Ll97Panel _panelInstance;
        
        public static Ll97Panel PanelInstance
        {
            get
            {
                // Lazy load - only create when first accessed
                if (_panelInstance == null)
                    _panelInstance = new Ll97Panel();
                return _panelInstance;
            }
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = PanelInstance;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right
            };
        }
    }
}
