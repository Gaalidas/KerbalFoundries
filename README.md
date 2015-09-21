KerbalFoundries - Main Distribution
====================================

File Structure:

Key: *   = Required for full functionality.
	 **  = Has cargo bay but does not shield anything isnide it.  Work in progress.
	 *** = Does not actually work when inverted.

"KerbalFoundries" -> Root directory of the mod, must be placed in GameData.
 \ "Assets"	-> Contains all of the textures and models.

 \ "Extras"	-> Contains compatibility ModuleManager patches and other supplementary configs.
  \ - "KF_DustFX_MM.cfg"			-> Compatibility patches between CollisionFX and the KF-specific DustFX particle systems.*
  \ - "KF_Scaletype.cfg" 			-> Contains all the ScaleType definitions for TweakScake.*
  \ - "KFCommunityTechTree.cfg"		-> Contains a few requested tech modifications for users who use CTT.
  \ - "FE_Mod_KerbalFoundries.cfg" 	-> Users of Filter Extensions will have a KF category with all of our parts in it.

 \ "Parts"	-> Contains the part configs.
  \ "LoFiAPUrevA.cfg"		-> An Auxilary Power Unit that generates ElectricCharge using LF/O as needed.
  \ "Repulsor.cfg"			-> The most basic repulsor unit with no gimbal and attaches like a wheel would.
  \ "RepulsorGimbal.cfg"	-> The new revolution in repulsion technology, works no matter what side is up.
  \ "RepulsorSurface.cfg"	-> A surface-attachable repulsion module.  No gimbals, but very thin.
  \ "RoverBody.cfg"			-> A basic rover body that has some built-in RCS systems for control when repulsing.**
  \ "Screw.cfg"				-> Based on a design used for vehicles that traverse fields of thick mud.  Works like a track.
  \ "Skid.cfg"				-> A basic landing skid, but with steering and suspension.
  \ "TrackLong.cfg"			-> A long, tank-like, track unit.
  \ "TrackMedium.cfg"		-> Our first track to make public release, roughly half the size of the long unit.
  \ "TrackRBIInverting.cfg"	-> For those who remember the old RBI mod, this track will need no introduction.***
  \ "TrackSimple.cfg"		-> A very simple, small-ish track unit.
  \ "TrackSmall.cfg"		-> Very small tracks.
  \ "TracksMole.cfg"		-> Another RBI conversion, these tracks are massive.
  \ "TrackSurface.cfg"		-> A small surface-attachable track that uses wheel-style steering.
  \ "TrackTiny.cfg"			-> An even smaller track than the "Small" ones.
  \ "WheelLarge.cfg"		-> Large wheels, large tires, can carry a heavy load or make a small car flip without even moving.
  \ "WheelMedium.cfg"		-> Our most popular wheel based on the classic KF individual-suspension wheel.
  \ "WheelSmall.cfg"		-> A small wheel that led to the development of our orientation markers due to the way it attaches.
  \ "WheelTiny.cfg"			-> A set of two wheels which use track steering.  Not widely used, but suitable for small rovers.

 \ Plugins
  \ "KF_plugin.dll"			-> Our main plugin.

 \ Settings
  \ "KFGlobals.txt"			-> This has all of our global definitions and settings.
  \ "DustColors.cfg"		-> This contains all of the unchanging color definitions for the dust colors based on planet/biome.
   \ "DustColors_*.cfg"		-> These can be ModuleManager patches for other mods that add or change planets.
								One such example is provided: "DustColors_Arkas.cfg"

 \ Sounds
  \ "APU.ogg"				-> Engine sound effect for the APU module.
  \ "GroundSkid.ogg"		-> A sound to be palyed when skidding over the terrain.
  \ "repulsor2.ogg"			-> A cool new repulsor running effect.
  \ "TyreSqueel.ogg"		-> An unused, but to be used someday sound effect for tire squeeling.
  \ "wheel2.ogg"			-> A wheel running effect.
