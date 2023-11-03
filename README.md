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
* You can copy the hex value of a color by clicking the label showing the hex value

## Screenshots
![SCREENSHOT](screenshots/screenshot.jpg?raw=true)

## Visualizers

### ShadingCube
The different shades of each ramp is shown on three cubes per ramp.

![VISUALIZER_SHADING_CUBE](screenshots/visualizer_shading_cube.jpg?raw=true)

### PaletteMerge
The ramps are displayed with any connection to other ramps.
Note: The connections are only displayed if there is only one connection per ramp.

![VISUALIZER_PALETTE_MERGE](screenshots/visualizer_palette_merge.jpg?raw=true)

### HueVal
The colors are displayed in three different diagrams
* Cylinder
  * Hue is the angle on the circle
  * Saturation is the distance to the circle center
  * Brightness is the height inside the cylinder
* Cube
  * Hue is the axis pointing to the right
  * Saturation is the axis pointing to the left
  * Brightness is the axis pointing upwards
* X/Y-Diagram
  * Hue is the x-axis (left to right)
  * Saturation is visualized by the size of the color circle
  * Brightness is the y-axis (bottom to top)

![VISUALIZER_HUE_VAL](screenshots/visualizer_hue_val.jpg?raw=true)

### SatVal
The ramps are displayed in a splitted bar diagram.
The top part of the diagram represents the brightness of the color.
The lower part represents the saturation of the color.

![VISUALIZER_SAT_VAL](screenshots/visualizer_sat_val.jpg?raw=true)

### LabView
This visualizer consists of two elements:
* CIELAB color space coverage 
  * Each color circle size represents its brightness.
* CIELAB color distance
  * The numerical distance for neighboring colors is shown

![VISUALIZER_LAB_VIEW](screenshots/visualizer_lab_view.jpg?raw=true)

### Voronoi
The three diagrams show the closest color (RGB space) to each point for the following axes:
* Hue-Saturation
* Hue-Brightness
* Saturation - Brightness

![VISUALIZER_VORONOI](screenshots/visualizer_voronoi.jpg?raw=true)


### Temperature
Each color is displayed on a Kelvin-based temperature scale.
 
 ![VISUALIZER_TEMPERATURE](screenshots/visualizer_temperature.jpg?raw=true)

## License
This project is licensed under GPLv3, for details see the [license file](LICENSE).

This project uses the following libraries:

* [Extended WPF Toolkit™](https://github.com/xceedsoftware/wpftoolkit) by [Xceed](https://xceed.com) provided under the [Xceed Community License agreement](https://github.com/xceedsoftware/wpftoolkit/blob/master/license.md)

* [csnumerics](https://github.com/cureos/csnumerics) by [Cureus AB](https://github.com/cureos) provided under the [LGPL-3.0 license](https://github.com/cureos/csnumerics/blob/main/COPYING.txt)