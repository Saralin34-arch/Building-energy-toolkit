using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodeToDesign.LL97
{
    public partial class Ll97Panel : UserControl
    {
        // Store EUI settings
        public static double CurrentEui { get; set; } = 50.0;
        public static string CurrentBuildingType { get; set; } = "Office";
        public static string CurrentBuildingAge { get; set; } = "2000-2015";
        public static string CurrentOperatingHours { get; set; } = "Standard (40-50 hrs/wk)";
        public static string CurrentOccupancy { get; set; } = "Medium (1 person/150-200 ft²)";
        public static string CurrentHvac { get; set; } = "Standard Efficiency";
        public static string CurrentWindow { get; set; } = "Standard Double-pane";
        public static string CurrentInsulation { get; set; } = "Standard (R-13 to R-30)";
        public static string CurrentFuelType { get; set; } = "Gas";

        public Ll97Panel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle the Analyze button click
        /// </summary>
        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.AnalyzeEvent != null)
            {
                App.AnalyzeEvent.Raise();
            }
        }

        /// <summary>
        /// Handle the Clear button click - reset all displayed data
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearData();
        }

        /// <summary>
        /// Clear all displayed data
        /// </summary>
        public void ClearData()
        {
            // Building Information
            AreaSourceText.Text = "--";
            AreaUsedText.Text = "--";
            EuiText.Text = "--";

            // Emissions Result
            CapText.Text = "--";
            EmissionsText.Text = "--";
            DifferenceText.Text = "--";

            // Fuel breakdown
            GasText.Text = "";
            ElectricText.Text = "";

            // Potential Fine
            FineText.Text = "--";
            FineText.Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)); // Default gray

            // Project Status
            StatusText.Text = "--";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(45, 55, 72)); // Default gray
        }

        /// <summary>
        /// Handle the EUI Settings button click
        /// </summary>
        private void EuiSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new EuiSettingsWindow();
            
            // Load current settings
            settingsWindow.LoadSettings(
                CurrentBuildingType,
                CurrentBuildingAge,
                CurrentOperatingHours,
                CurrentOccupancy,
                CurrentHvac,
                CurrentWindow,
                CurrentInsulation,
                CurrentFuelType
            );

            // Show dialog
            bool? result = settingsWindow.ShowDialog();

            if (result == true && settingsWindow.Applied)
            {
                // Save settings
                CurrentEui = settingsWindow.CalculatedEui;
                CurrentBuildingType = settingsWindow.SelectedBuildingType;
                CurrentBuildingAge = settingsWindow.SelectedBuildingAge;
                CurrentOperatingHours = settingsWindow.SelectedOperatingHours;
                CurrentOccupancy = settingsWindow.SelectedOccupancy;
                CurrentHvac = settingsWindow.SelectedHvac;
                CurrentWindow = settingsWindow.SelectedWindow;
                CurrentInsulation = settingsWindow.SelectedInsulation;
                CurrentFuelType = settingsWindow.SelectedFuelType;

                // Update EUI display
                EuiText.Text = $"{CurrentEui:F1} kBtu/ft²·yr";
                
                // Show confirmation
                MessageBox.Show(
                    $"EUI updated to {CurrentEui:F1} kBtu/ft²/yr\n\n" +
                    $"Building Type: {CurrentBuildingType}\n" +
                    $"Building Age: {CurrentBuildingAge}\n" +
                    $"Fuel Type: {CurrentFuelType}\n\n" +
                    "Click 'ANALYZE START' to recalculate emissions.",
                    "EUI Settings Applied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        /// <summary>
        /// Load analysis results into the panel
        /// </summary>
        public void LoadData(
            string areaSource,
            double areaFt2,
            double eui,
            Ll97Result res,
            Dictionary<string, double> byFuel)
        {
            // Building Information
            AreaSourceText.Text = areaSource;
            AreaUsedText.Text = $"{areaFt2:N0} ft²";
            EuiText.Text = $"{eui:F1} kBtu/ft²·yr";

            // Emissions Result
            CapText.Text = $"{res.Cap:N0} tCO₂e/yr";
            EmissionsText.Text = $"{res.Emissions:N0} tCO₂e/yr";
            
            string diffSign = res.Difference >= 0 ? "under" : "over";
            DifferenceText.Text = $"{Math.Abs(res.Difference):N0} tCO₂e/yr ({diffSign})";

            // Fuel breakdown - Gas and Electric
            if (byFuel != null)
            {
                if (byFuel.ContainsKey("Gas"))
                    GasText.Text = $"Gas: {byFuel["Gas"]:N0} tCO₂e/yr";
                else
                    GasText.Text = "";

                if (byFuel.ContainsKey("Electric"))
                    ElectricText.Text = $"Electric: {byFuel["Electric"]:N0} tCO₂e/yr";
                else
                    ElectricText.Text = "";
                
                // If other fuels exist, show them in Gas/Electric lines
                foreach (var kvp in byFuel)
                {
                    if (kvp.Key != "Gas" && kvp.Key != "Electric")
                    {
                        if (string.IsNullOrEmpty(GasText.Text))
                            GasText.Text = $"{kvp.Key}: {kvp.Value:N0} tCO₂e/yr";
                        else if (string.IsNullOrEmpty(ElectricText.Text))
                            ElectricText.Text = $"{kvp.Key}: {kvp.Value:N0} tCO₂e/yr";
                    }
                }
            }

            // Potential Fine
            if (res.Fine > 0)
            {
                FineText.Text = $"${res.Fine:N0}/year";
                FineText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
            }
            else
            {
                FineText.Text = "$0 (Compliant)";
                FineText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Green
            }

            // Project Status
            StatusText.Text = res.Status;
            StatusText.Foreground = (res.Status == "PASS")
                ? new SolidColorBrush(Color.FromRgb(40, 167, 69))   // Green
                : new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        }
    }
}
