# Hacky-LDtk-Unity-importer
***
Imports levels created with LDtk (<https://github.com/deepnight/ldtk>) into Unity.

Not meant for serious production, I just wrote this as a quick way to import levels while doing game jams.
***

## Features
* Automatically imports levels and their tilemaps into Unity
* Currently does **not** support entities or enums
* Does **not** support external levels
* Does **not** care about the world layout, each level will be a seperate prefab
* Will probably not be updated until next ludum dare, but feel free to open any issues / pull requests
* Probably messy and undocumented code
* Add a bunch of **not**s here I forgot to add

## Usage
1. Drop Assets/LDtkImporter into the Assets folder of your Unity project
2. Open the importer window in the menubar at Assets/Import/LDtk Importer
3. Click Browse and choose your .ldtk project file (File can be anywhere on your system, it does not need to be in the Unity project folder)
4. Set pixels per unit
5. Set import directory in your assets folder (do not place any other files in this folder as they might get overwritten/deleted)
6. Press import and the levels and their tilemaps will be imported. _Hopefully_ ;-)
7. One prefab for each level should now exist in the import directory
## Recommendations for LDtk
* Use a seperate layers for collisions, ladders, background, etc.. so you can easyly add Tilemap Collision 2D (+ use as trigger for the ladders)
***
### What is the setting maximum tilemaps per layer?
In one layer LDtk allows to have multiple tiles stacked at one location (e.g if transparent tiles are used). This limits the maximum depth per layer. 4 should be a good value for most setups.
***

*Happy jamming!*