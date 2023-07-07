# KPal - Advanced Pixel Art Palette Generator
## About
KPal is a tool to create color palettes usually for pixel art creation. The generation is highly customizable and different color ramps can be linked.
## Features
* HSV model based color ramp creation
* Customizable curves for hue and saturation for each ramp
* Linking multiple color ramps to reduce color count and increase palette integrity
* Multiple palette visualization views for color harmony analysis
* Export to multiple formats
  * png (1px, 8px, 32px)
  * Aseprite
  * Gimp gpl
  * Paint.NET txt
  * Adobe ase
  * Corel xml
  * JASC/PSP pal
  * (Open/Libre/Star)Office soc
* Fine adjustments for each color
* Selectable saturation shift behavior for darker values
## Build
KPal is a Visual Studio/C#/WPF project. Open, build and run the solution in Visual Studio.
## Installation / Releases
You can download a release for Windows from the [releases section](https://github.com/krush62/KPal/releases).
No installation is needed. KPal.exe is the application itself, colors.csv contains the list of color names (This file is not mandatory but highly recommended).
This application needs .NET (Microsoft Windows Deskop Runtime) to work.
## Usage Tips
* To link colors: drag a color from one palette to a color of another palette
* Save as a different file: a "Save as" functionality is available when using the right mouse button for the save button
* Reset slider values: double clicking the numeric value representation will reset the corresponding slider
* To change the visualizer type on the bottom, hover the visualizer area and select a different visualizer from the drop-down list.
## Screenshots
![SCREENSHOT](screenshots/screenshot.jpg?raw=true)
## License
This project is licensed under GPLv3, for details see the [license file](LICENSE).