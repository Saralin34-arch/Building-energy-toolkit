Revit LL97 Plugin — Source Code

This folder contains the full source code for the LL97 Revit Plugin.
The plugin computes:

Building Energy Use Intensity (EUI)

Carbon emissions by fuel type

LL97 emissions caps

Annual fines & compliance status

UI panel elements for BIM-based carbon analysis

This source code is intended for developers who want to modify or extend the plugin.

🔧 Project Structure
src/
│
├── *.cs                → Main C# logic (LL97 engine, models, handlers)
├── *.xaml              → WPF UI layouts for Revit dockable panes
├── *.xaml.cs           → Code-behind for UI
├── CodeToDesign.LL97.csproj
└── README.md


You will also see folders like:

Properties/ — Assembly info and metadata

Resources/ (optional) — Icons, images, SVG assets

⚙️ Build Requirements

You need the following tools to build the plugin from source:

Windows 10/11

Revit 2024 or 2025 SDK installed

Visual Studio 2022

Workload: .NET desktop development

Optional workload: Desktop development with C++ (required by some Revit SDK tools)

.NET Framework 4.8 or .NET 8 depending on your project template

🧩 How to Build the Plugin

Open CodeToDesign.LL97.csproj or the .sln project in Visual Studio

Set build mode to:

Debug | Any CPU


or

Release | Any CPU


Build the solution:

Build → Build Solution (Ctrl + Shift + B)


The output file appears under:

bin/Debug/net8.0-windows/CodeToDesign.LL97.dll


or Release:

bin/Release/net8.0-windows/

🧱 Main Components
Core Logic

LL97Engine.cs — Core engine for cap/fine/emission logic

LL97Models.cs — Data structures (fuel types, building categories, GHG factors)

EuiCalculator.cs — Computes annual energy usage

Revit UI

LL97Panel.xaml — Main dockable panel

LL97Panel.xaml.cs — Panel logic and triggers

EuiSettingsWindow.xaml — Settings popup window

EuiSettingsWindow.xaml.cs — Settings logic

Revit API Integration

HelloCommand.cs — Test command

AnalyzeEventHandler.cs — Event hooks for model changes

App.cs — Revit plugin entry point

📦 Output Files (go into dist/)

When you build, Visual Studio outputs:

CodeToDesign.LL97.dll → The plugin

Additional .json and .pdb (optional for debugging)

These files should be moved to the dist folder for release.

🤝 Contributing

You are welcome to fork this repo and submit pull requests for:

New UI features

Additional carbon analysis frameworks

Country/state-level carbon factors

UI accessibility improvements

📄 License

Add your license here (MIT recommended).