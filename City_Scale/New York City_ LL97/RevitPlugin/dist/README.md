Revit LL97 Plugin — Installation Guide

This folder contains the compiled plugin files required to run the LL97 plugin inside Autodesk Revit.
You do NOT need Visual Studio or the source code — only the files in this folder.

📦 Contents of this Folder
dist/
│
├── CodeToDesign.LL97.dll       → Main plugin logic
├── CodeToDesign.LL97.addin     → Revit manifest file (required)
└── README.md

🛠️ How to Install the Plugin (Windows)
1. Download the plugin

Download the latest ZIP from the GitHub Releases page.

Unzip it — you will find:

CodeToDesign.LL97.dll
CodeToDesign.LL97.addin

2. Copy files to the Revit Addins folder

Place both files into your Revit addins directory:

%AppData%\Autodesk\Revit\Addins\2025\


or for Revit 2024:

%AppData%\Autodesk\Revit\Addins\2024\


Full path example:

C:\Users\YOURNAME\AppData\Roaming\Autodesk\Revit\Addins\2025\

3. Launch Revit

Open Revit → you should see a new tab:

LL97 Toolkit


Or a new panel containing:

LL97 Calculator

Energy Settings

EUI to Carbon

Emissions Breakdown

Compliance Status

🔍 How the Plugin Works

Reads building area, energy inputs, and year

Computes carbon emissions based on fuel type

Compares against Local Law 97 caps

Displays:

Cap

Emissions

Difference

Fine

Status (PASS / FAIL)

🧹 Uninstall

Delete files from:

%AppData%\Autodesk\Revit\Addins\2025\

❓ FAQ

Q: I installed the plugin but don’t see anything in Revit.
Make sure you placed the .addins and .dll in the correct version folder (2024 vs 2025).

Q: Can I use this plugin in Revit LT?
No — Revit LT does not support add-ins.

📩 Contact

If you encounter issues, create an Issue on GitHub.