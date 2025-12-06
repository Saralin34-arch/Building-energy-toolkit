using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CodeToDesign.LL97
{
    public partial class EuiSettingsWindow : Window
    {
        public double CalculatedEui { get; private set; }
        public bool Applied { get; private set; }

        // Store current selections
        public string SelectedBuildingType { get; private set; }
        public string SelectedBuildingAge { get; private set; }
        public string SelectedOperatingHours { get; private set; }
        public string SelectedOccupancy { get; private set; }
        public string SelectedHvac { get; private set; }
        public string SelectedWindow { get; private set; }
        public string SelectedInsulation { get; private set; }
        public string SelectedFuelType { get; private set; }

        // Available fuel types
        private static readonly List<string> FuelTypes = new List<string>
        {
            "Gas",
            "Electric", 
            "Steam",
            "Mixed (Gas + Electric)"
        };

        public EuiSettingsWindow()
        {
            InitializeComponent();
            LoadDropdowns();
            SetDefaults();
            
            // Wire up selection changed events
            BuildingTypeCombo.SelectionChanged += OnSelectionChanged;
            BuildingAgeCombo.SelectionChanged += OnSelectionChanged;
            OperatingHoursCombo.SelectionChanged += OnSelectionChanged;
            OccupancyCombo.SelectionChanged += OnSelectionChanged;
            HvacCombo.SelectionChanged += OnSelectionChanged;
            WindowCombo.SelectionChanged += OnSelectionChanged;
            InsulationCombo.SelectionChanged += OnSelectionChanged;
            
            UpdateCalculation();
        }

        private void LoadDropdowns()
        {
            BuildingTypeCombo.ItemsSource = EuiCalculator.GetBuildingTypes();
            BuildingAgeCombo.ItemsSource = EuiCalculator.GetBuildingAges();
            OperatingHoursCombo.ItemsSource = EuiCalculator.GetOperatingHours();
            OccupancyCombo.ItemsSource = EuiCalculator.GetOccupancyLevels();
            HvacCombo.ItemsSource = EuiCalculator.GetHvacOptions();
            WindowCombo.ItemsSource = EuiCalculator.GetWindowOptions();
            InsulationCombo.ItemsSource = EuiCalculator.GetInsulationOptions();
            FuelTypeCombo.ItemsSource = FuelTypes;
        }

        private void SetDefaults()
        {
            BuildingTypeCombo.SelectedItem = "Office";
            BuildingAgeCombo.SelectedItem = "2000-2015";
            OperatingHoursCombo.SelectedItem = "Standard (40-50 hrs/wk)";
            OccupancyCombo.SelectedItem = "Medium (1 person/150-200 ft²)";
            HvacCombo.SelectedItem = "Standard Efficiency";
            WindowCombo.SelectedItem = "Standard Double-pane";
            InsulationCombo.SelectedItem = "Standard (R-13 to R-30)";
            FuelTypeCombo.SelectedItem = "Gas";
        }

        /// <summary>
        /// Set values from existing settings
        /// </summary>
        public void LoadSettings(string buildingType, string age, string hours, 
                                  string occupancy, string hvac, string window, 
                                  string insulation, string fuelType = "Gas")
        {
            if (!string.IsNullOrEmpty(buildingType)) BuildingTypeCombo.SelectedItem = buildingType;
            if (!string.IsNullOrEmpty(age)) BuildingAgeCombo.SelectedItem = age;
            if (!string.IsNullOrEmpty(hours)) OperatingHoursCombo.SelectedItem = hours;
            if (!string.IsNullOrEmpty(occupancy)) OccupancyCombo.SelectedItem = occupancy;
            if (!string.IsNullOrEmpty(hvac)) HvacCombo.SelectedItem = hvac;
            if (!string.IsNullOrEmpty(window)) WindowCombo.SelectedItem = window;
            if (!string.IsNullOrEmpty(insulation)) InsulationCombo.SelectedItem = insulation;
            if (!string.IsNullOrEmpty(fuelType)) FuelTypeCombo.SelectedItem = fuelType;
            
            UpdateCalculation();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCalculation();
        }

        private void UpdateCalculation()
        {
            try
            {
                string buildingType = BuildingTypeCombo.SelectedItem?.ToString() ?? "Office";
                string age = BuildingAgeCombo.SelectedItem?.ToString() ?? "2000-2015";
                string hours = OperatingHoursCombo.SelectedItem?.ToString() ?? "Standard (40-50 hrs/wk)";
                string occupancy = OccupancyCombo.SelectedItem?.ToString() ?? "Medium (1 person/150-200 ft²)";
                string hvac = HvacCombo.SelectedItem?.ToString() ?? "Standard Efficiency";
                string window = WindowCombo.SelectedItem?.ToString() ?? "Standard Double-pane";
                string insulation = InsulationCombo.SelectedItem?.ToString() ?? "Standard (R-13 to R-30)";

                EuiResult result = EuiCalculator.CalculateEui(
                    buildingType, age, hours, occupancy, hvac, window, insulation);

                CalculatedEui = result.CalculatedEui;
                CalculatedEuiText.Text = result.CalculatedEui.ToString("F1");
                BaseEuiText.Text = $"Base EUI: {result.BaseEui} | " +
                                   $"Age: ×{result.AgeMultiplier:F2} | " +
                                   $"Hours: ×{result.HoursMultiplier:F2}";
            }
            catch
            {
                CalculatedEuiText.Text = "--";
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            // Store selections
            SelectedBuildingType = BuildingTypeCombo.SelectedItem?.ToString();
            SelectedBuildingAge = BuildingAgeCombo.SelectedItem?.ToString();
            SelectedOperatingHours = OperatingHoursCombo.SelectedItem?.ToString();
            SelectedOccupancy = OccupancyCombo.SelectedItem?.ToString();
            SelectedHvac = HvacCombo.SelectedItem?.ToString();
            SelectedWindow = WindowCombo.SelectedItem?.ToString();
            SelectedInsulation = InsulationCombo.SelectedItem?.ToString();
            SelectedFuelType = FuelTypeCombo.SelectedItem?.ToString() ?? "Gas";

            Applied = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Applied = false;
            DialogResult = false;
            Close();
        }
    }
}
