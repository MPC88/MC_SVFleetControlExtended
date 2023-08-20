# MC_SVFleetControlExtended
  
Backup your save before using any mods.  
  
Uninstall any mods and attempt to replicate issues before reporting any suspected base game bugs on official channels.  
  
Install  
=======  
1. Install BepInEx - https://docs.bepinex.dev/articles/user_guide/installation/index.html Stable version 5.4.21 x86.  
2. Run the game at least once to initialise BepInEx and quit.  
3. Download latest mod release.  
4. Place MC_SVFleetControlExtended.dll in .\Star Valor\BepInEx\plugins\  

Note: This mod includes functionality provided by the following, which should be removed if already in use:
- MC_SVDockUndockAllHotkeys  
- MC_SVFleetEnergyBarrierControl  
- MC_SVFleetRoleHotkeys  
- MC_SVFleetStationCargoDrop
  
All configured settings will be lost (keybinds, energy barrier thresholds).  

Function  
========  
Expanded fleet control.  This mod is work in progress.  Majority appears stable, however odd behaviour may still be encountered, particularly with escorts and forced engagement ranges.  
  
On the fleet behaviour configuration panel:  
- Set escort (low level of testing): Fleet member will treat assign escort as they would normally treat the player.  They will follow the escortee in formation.  They will prioritise repairing/defending their assigned escortee.  If the escortee is docked, they will wait at the station/docked location.
- Dedicated defender: Fleet member will prioritise attacking drone and missiles.  NOTE they don't care if they have point defence weapons, that's up to you to ensure.  If there are no drones/missiles they will engage hostile ships attacking their assign escortee.  Otherwise, they will not engage at all (i.e. they will not instigate any combat).  I might change this so they ignore missiles, they are pretty bad at those, but they are reasonable at attacking drones.  Hit and run behaviour not recommended.  
- Force engagement range (low level of testing): DPS fleet member will maintain this distance from their target.  Useful for ships with long range weapons, but also some point defence as this stops them moving into range for their point defence weapons.  This works best with strafe and fire behaviour, hit and run sees fleet members overshooting their desired range (unless you set it to 0 of course, then it's spot on...).  
- Use energy barrier when HP below: Allows you to set when a fleet member will use their energy barrier (if they have one equipped).  
- Cloak with player: Fleet member will use their cloaking device (if they have one) when the player does.  They will deactivate with the player too.  NOTE they will not hold fire, you should command this separately if you don't want them to engage just like normal.  
  
Hot keys (configurable, keys shown below are the defaults):
- Left Alt+A: Undock all (from carrier or station).  
- Left Alt+D: Dock all (to carrier or station).  
- Left Alt+S: Unload cargo from all.  Only works when docked.  Also unloads passengers and any ammunition they do not require for equipped weapons.  
- Left Alt+X: Hold position toggle.  Forces the fleet to remain in current position or return to formation.
- J: Set all fleet members to DPS role.  
- K: Set all fleet members to Healer role.  
- L: Set all fleet members to Miner role.  Note this conflicts with default spotlight binding.  
  
Configuration  
=============  
After first run, mc.starvalor.fleetcontrolextended.cfg will be created in .\Star Valor\BepInEx\config\ folder.  This file allows you set configure the keybinds.  There are separate entries for:  
- Dock all key  
- Undock all key  
- Unload all key  
- Modifier key for the above (set to None if not desired)

- Hold position toggle key
- Modifier key for the above (set to None if not desired)

- Set roles to DPS  
- Set roles to healer  
- Set roles to miner
- Modifier key for the above (set to None by default)  
