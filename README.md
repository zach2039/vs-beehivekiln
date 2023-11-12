Beehive Kiln
=================

Overview
--------
A mod that adds beehive kiln to Vintage Story; the beehive kiln is mutli-block kiln for mass pottery firing after pit kilns.


Quick-start Guide
--------

1. Kiln must be constructed, filled with raw pottery, and sealed. Consult the mod images for proper setup. Viewing the block info of the flue will show construction errors.

2. A firepit must be lit underneath with fuel that burns at a hot enough temperature. Viewing the block info of the flue will show current kiln temperature.

3. After the kiln heats, 6 hours must pass and the kiln must not be opened or heat will be lost and process will reset. 

4. After firing completes, break open the kiln to get your fired items.


Config Settings in `VintageStoryData/ModConfig/BeehiveKiln.json`
--------

 - FiringTimeHours: How long a firing process takes to complete once at minimum temp, in hours; defaults to `6.0`.

 - MinimumFiringTemperatureCelsius: The minimum temperature the kiln must reach to start and/or continue firing, in celsius; defaults to `500`.
  
 - TemperatureGainPerHourCelsius: How much kiln temperature can increase per hour, in celsius; defaults to `250`.


Future Plans
--------

 - Other fuel sources, like lit charcoal blocks and similar.


Notable mods that add similar
--------

 - [Useful Stuff](https://mods.vintagestory.at/show/mod/25)
	- Adds a different implementation of a kiln, among other things; be sure to check it out.