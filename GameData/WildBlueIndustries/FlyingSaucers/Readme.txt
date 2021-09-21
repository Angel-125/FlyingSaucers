Kerbal Flying Saucers: Build Flying Saucers in KSP!

Have you ever wanted to pretend that your kerbals found a crashed flying saucer and then reverse-engineered it? With Kerbal Flying Saucers (KFS), now you can! KFS is a mod that lets you research and build your own custom flying saucers that work within established gameplay mechanics while providing an exotic twist. It’s as easy as mixing and matching components to create a vehicle suited to your needs. And with its custom tech tree branch you can simulate your reverse-engineering efforts via the stock science system.
Kerbal Flying Saucers supports Pathfinder, MOLE, DeepFreeze, OSE Workshop, KIS/KAS, CTT, Snacks, TAC-LS, and more.

---Real-World References---

Popular Mechanics, November 2000: “America’s Nuclear Flying Saucer” (https://books.google.com/books?id=MxXlKb9wIe0C&lpg=PP1&lr&rview=1&pg=PA66#v=onepage&q&f=false
)
VZ-9 Avrocar: (http://www.nationalmuseum.af.mil/Visit/MuseumExhibits/FactSheets/Display/tabid/509/Article/195801/avro-canada-vz-9av-avrocar.aspx)
Wired: “Declassified at Last: Air Force’s Supersonic Flying Saucer Schematics” (https://www.wired.com/2012/10/the-airforce/)
Northrop NS-97: (https://s-media-cache-ak0.pinimg.com/originals/53/f7/48/53f7489bf06f582eeb1f68a3a42fb2aa.jpg)

---INSTALLATION---

Copy the contents of the mod's GameData directory into your GameData folder.

--RELEASE NOTES---

0.6

New Gravitic Engines

- GND-00 "Beta" Gravitic Engine: This is a 1.25m (Size 1) self-contained gravitic engine that has a built-in fusion reactor and gravitic generator. With Blueshift installed, the generator can switch between Propellium and Fusion Pellets.
- GND-01 "Quantum" Gravitic Engine: This is a 2.5m (Size 2) self-contained gravitic engine that has a built-in fusion reactor and gravitic generator. With Blueshift installed, the generator can switch between Propellium and Fusion Pellets.

New Storage Tanks

The following tanks can store a wide variety of different resources including Propellium and Graviolium.
NOTE: These parts are only available if you don't have Blueshift installed.

- Mk2 Omni Tank
- Mk3-S3 Omni Tank
- S-1 Omni Tank
- S-2 Omni Tank
- S-3 Omni Tank
- S-1 Omni Endcap Tank
- S-2 Omni Endcap Tank

Changes

- Added new WBIGraviticEngineGenerator part module. It combines a resource generator with the gravitic engine. It is capable of switching between two or more resource modes defined by a RESOURCE_MODE config node.
- Added support for EVA Repairs, if installed. You can find the mini-mod here.

0.5

This release is a major milestone: the A-51 Flapjack is feature complete! It's been a long road, and one with lots of bumps. The A-51 was originally just a proof of concept to ensure that the plugin worked correctly and that I could make a saucer-shaped aircraft that could fly. But, it morphed into the craft that it is today. It is a complete solution from its early days as a jet-powered VTOL prototype to a rocket-powered lenticular reentry vehicle- based on a real-world Air Force proposal- to a flying saucer powered by gravity waves. Now that it is complete, I can focus on the self-contained gravitic engines (coming next release) and the mothership (as time and sanity permits).

New Parts

- A-51 Conference Room: This is an alternate version of the A-51 Crew Cabin. It has an interior configured for meetings.
- Mk2 Engine Plate: This is similar to the Making History engine mounts, but it is made for the Mk2 form factor.
- IXS Cockpit: This is a custom Size 2 cockpit that can be fitted with 3 A-51 saucer sections. The design is inspired by the IXS Enterprise cockpit created by Mark Rademaker.
- A-51 Landing Strut: This is a landing strut designed for the A-51. it is node-attached to the keel (with another node for saucer sections).

Changes

- Deprecated the existing A-51 Engine Mount and replaced it with a newer version, called the A-51-Mk2 Adapter, that adapts the A-51 saucer to the Mk2 part set.
- Deprecated the existing A-51 Linear Aerospike and replaced it with a newer version that fits the Mk2 form factor.
- Updated the Flapjack parts and textures to be closer to Restock.
- Added missing stock cargo inventory to the A-51 Crew Cabin.
- Added a stock/Near Future Props IVA to the A-51 Cockpit that will automatically be enabled when MOARdV's Avionics Systems isn't installed.
- Removed the A-51 Mk1 Adapter, A-51 Size 1.875m Adapter, A-51 Size 1.5 Shroud, A-51 to Mk2 Shroud, A-51 Size 2 Adapter, and A-51 Size 2 Shroud. These are no longer needed as a result of adding the A-51-Mk2 Adapter.
- Hid the Advanced Alien Engineering tech tree node when Community Tech Tree is installed; parts slated for AEE will be found in CTT's Unified Field Theory node instead.
- Fixed issue where drills were not harvesting graviolium on planetary surfaces that had the resource.
- Added new stock Easter Egg-inspired flying saucer Space Anomaly that appears if you have Blueshift installed.
NOTE: If you don't have Blueshift installed, then the UFO becomes available in the VAB/SPH.

Known Issues
- The DepthMask for the Landing Strut's gear well isn't working properly and appears to be an issue with the landing strut's interaction with DepthMask.
- The Landing Strut lacks a functioning suspension system due to the way the game works. It has high impact tolerance to compensate.

0.4.12
- Recompiled for KSP 1.12.2.

0.4.10

Changes

- Gravitic engines no longer set the throttle when hover mode is active, but resources are still consumed.
- Lowered the unit cost of Graviolium.
- Added stock inventory support to storage sections.
- Graviolium is no longer produced in the Refinery (which was available at the Space Center).
- Graviolium is no longer removed when you launch a vessel.
- With Blueshift installed, parts with Graviolium storage will be able to add the resource in the editor when the resource tweak button is enabled.
- Fixed issue where gravitic engines without a rotating gravity ring would disable Crazy Mode.
- Fixed issue where Graviolium never appears in asteroids.

0.4.9

Gravitic Engines (A-51 & S-4)
- All activated gravitic engines will synchronize their flight direction and controls. For instance, if one engine is set to forward thrust, then all activated engines will be set to forward thrust.
- Effect plumes on multiple activated gravitic engines now all synchronize and travel in the same direction.
- Fixed issue where hover mode with multiple engines would not be able to cancel out vertical acceleration.
- Fixed issue where hover mode didn't consume resources.

0.4.8
IMPORTANT NOTE: Remove the existing S-4 Gravitic Engine part from your mothership before installing this update!

Gravitic Engines (A-51 & S-4)
- Crazy Mode now has an enabled/disabled toggle and action group item.
- Crazy Mode now responds to input from axis action groups (F/B, L/R, U/D). If you enable Crazy Cruise Mode then you won't have to hold down the translation buttons to continue moving.
- Crazy Mode now honors the engine thrust percent setting.
- Reduced Crazy Mode max speed and rebalanced Gravity Waves cost. On a single gravitic generator, craft can go about 75% max speed without depleting the GravityWaves.
- You can now halt Crazy Mode by tapping on the brakes.
- Added new Toggle Hover Mode action and Enable/Disable Hover Mode PAW button.
- When Hover Mode is active and Crazy Mode is inactive, you can tap the up/down translation axis keys to increase or decrease vertical velocity. Tap the brakes to kill vertical velocity.
- Removed previous Crazy Mode action group items.

Parts
- At last the mothership texture is shiny and metallic! This was the intent from the start but I had to wait for the technology to catch up. Once more parts are done I'll make a more traditional non-shiny texture option.
- Removed plume effects and RCS from the S-4 Engineering Core.
- Removed the old S-4 Gravitic Engine part.
- Added new version of the S-4 Gravitic Engine that is based on the engineering core. This revamped part is similar to the Flapjack's gravitic engine.
- Refactored the normal maps to support mip-mapping (makes bumpiness look better from a distance).

0.4.7
- Compatibility update

0.4.6
- Updated to KSP 1.8
- Completed texturing on the S-4 Outer Storage Section, S-4 Inner Storage Section, and S-4 Half Outer Storage Section.
- Removed probe core functionality from the A-51 Flapjack and S-4 Excalibur cockpits.
- Added static discharge ability to the S-4 Inner Storage Section.
- Added additional attachment nodes to the S-4 Engineering Core. These nodes can be toggled.
- Completed exterior of the S-4 "Excalibur" cockpit.
NOTE: Cockpit IVA is a WORK IN PROGRESS!!!

0.4.5.4
- WBT update

0.4.5.3
- Added portsideCam, starbordCam, forwardCam, and bottomCam to the Flapjack and Excalibur cockpit parts.

0.4.5.2
- Removed part requirements on several experiments.

0.4.5
- Updated engine effects on Flapjack and Excalibur.
- Performance tuned Excalibur's generators and engine.
- Fixed Crazy Mode on Excalibur engine and updated max velocity.
- Fixed part orientation on Excalibur engine.

0.4.4
- Updated for KSP 1.7
- Decals courtesy of JadeOfMaar

0.4.3
- Updated FX, icons, plumes, bulkhead profiles, and Flapjack breaking forces- thanks JadeOfMaar!

0.4.2
- Bug fixes

0.4.1
- Recompiled for KSP 1.6

0.4
- Updated to KSP 1.5.X
- Fixed issues with the Kray Kray experiment not working properly.
- The Kray Kray now properly unlocks whole tech tree nodes upon a successful breakthrough research result.

0.3.12
Last release for KSP 1.4.5!

Bug Fixes & Enhancements
- Fixed issue where switching Play Modes would cause some files to not be renamed and cause all kinds of fun for players...
NOTE: For the changes to take effect, you'll need to switch your Play Mode to some mode other than the current one, then switch it to the desired mode.

0.3.11
- Added Large Tail Fin: This is a larger version of the Flapjack's tail fin for use on the Excalibur.

0.3.10
- Added missing parts.
- Added new WBIGraviticLift module. You can control it via the Flight Operations Managar, or via throttle. Example config:

	MODULE
	{
		name = WBIGraviticLift
		ConverterName = Gravitic Lift
		StartActionName = Start Gravitic Lift
		StopActionName = Stop Gravitic Lift
		ToggleActionName = Toggle Gravitic Lift
		FillAmount = 1.0
		AutoShutdown = false
		GeneratesHeat = false
		UseSpecialistBonus = false
		startEffect = effectStart
		stopEffect = effectStop
		runningEffect = effectRunning

		//Maximum acceleration in meters per second per second.
		maxAcceleration = 20

		//EfficiencyBonus is auto-calculated. The more massive the craft, the higher the "bonus," which translates into
		//consuming and producing input/output resources faster.
		//So instead of EfficiencyBonus, we use specific impulse to determine how well we use the resources.
		//Math: acceleration * total vessel mass = lift force.
		//EfficiencyBonus = lift force / (9.81 * specificImpulse).
		//Resources are consumed & produced at Ratio * EfficiencyBonus.
		specificImpulse = 1
		 
		//...or liquid fuel, or whatever...
		INPUT_RESOURCE
		{
			ResourceName = Graviolium
			Ratio = 0.0016875
			FlowMode = STAGE_PRIORITY_FLOW
		}
		OUTPUT_RESOURCE
		{
			ResourceName = StaticCharge
			Ratio = 0.05
			DumpExcess = false
		}
	}

0.3.9

Excalibur (WORK IN PROGRESS!)
- Added Large Body Flap. It works the same way as the Flapjack's body flap.
- Added S-4 "Excalibur" Cockpit prototype.
- Finished modeling and unwrap of the storage sections and engineering core- texturing to follow.
- Removed Double Outer Section.
- Updated wording on node switching for the Inner Section, and added visual cues to help identify configuration for the outer sections.
- Added particle effects to the engineering core.

Bug Fixes & Enhancements
- Fixed Crazy Mode controls vanishing when a vessel without a gravitic engine is loaded into scene.
- Fixed NRE produced by converters when BARIS isn't installed.
- Fixed issue preventing multiple saucers within physics range of each other from hovering at the same time.
- Fixed issue where craft wouldn't hover on airless bodies.
- Graviolium won't be stripped from your vessels upon launching from the VAB/SPH if you're in Sandbox or Science mode.
- Crazy Mode can now be used when the craft is considered to be sub-orbital as well as when it is flying.

0.3.8
- Fixes Play Mode failing to rename certain files. NOTE: You might need to reset your current play mode. Simply open the WBT app from the Space Center, choose another mode, press OK, and again open the app, selecting your original play mode. Then be sure to restart KSP.

0.3.7
- Fix for Stardust not collecting Graviolium.

0.3.5
- Minor update to the Hydroscoop to use the new WBIResourceHarvester.

0.3.4
- WBT update.
- Fixed click-through issues with windows displayed in the editor.

0.3.3
- Added experiment lab to MPL and experiment container to the Science Box.
- Bug fixes for experiments.

0.3.2
- WildBlueTools update.

0.3.1
- Updated part symmetry axis. Thanks for the investigation, Vardicd & shdwlrd! :)
- You can now set the gravitic engine's forward, reverse, and VTOL thrust modes through action groups.
- You can now toggle Crazy Mode on and off through action groups. NOTE: this only works in the forward direction.
- Added new sounds to the Excalibur Engineering Core's generators.
- The Excalibur Inner Section now has node toggles to make it easier to switch between adding two standard-sized outer sections, or one standard outer section and two half-sections.
- Made a better sound loop for the gravitic engine.
- More infrastructure work done on KerbalActuators for kOS support.
- Updated a couple of part tool tips for clarity.
- Re-added IVA props that I missed during latest MAS integration.
- Fixed mesh gaps as best as possible for Excalibur's template parts and adjusted the shape to better fit 2.5m tall payloads.
- Fixed top node placement on the fusion reactor.
- Fix for multiple part tool tips appearing when you attach new parts. Hopefully this will solve it once and for all!

0.3.0
- Flapjack IVA upgraded courtesy of MOARdv :) NOTE: You'll need MOARdv Avionics Systems 0.20.1 or later.
- Added new S-4 Excalibur parts category.
- Added test parts for the S-4 Excalibur mothership. THESE ARE SUBJECT TO CHANGE WITHOUT NOTICE.

0.2.11
- Un-broke the template managers.
- Finally straightened out the Half Keel to have the proper model and colliders.

0.2.10
- Fixed missing collider on the Half Keel.
- The Gravitic Engine won't kill the throttle if you turn off Hover Mode and have forward thrust set during flight. This should make it easier to transition from VTOL to forward flight.

0.2.9
- Added A-51 Half Keel for those times when only half a saucer will suffice.
- Recompiled for KSP 1.4.4

0.2.8
- Fixed issues where the Flapjack docking port and Gravitic Engine's RCS thrusters wouldn't work with the cargo bay attached. Thanks for the solution, MOARdv! :)

0.2.7
- Fixed issue where the throttle wouldn't be updated during Hover Mode and forward flight.

0.2.6
- Fixed issue where the gravitic engine wouldn't produce acceleration when MechJeb tried to execute a maneuver node.
- Added infrastructure to KerbalActuators to support MOARdV Avionics Systems.
- Removed debug code left over from last release.
- Improved Crazy Mode performance when there are no other vessels in physics range.

0.2.5
- In the flight scene, you now have the ability to recovery resources that are normally stored in the Refinery.
- Some resources, like Graviolium, are now considered restricted in OmniStorage tanks. You can allocate space for a restricted resource in the VAB/SPH, but it won't be added to the container until launch. This avoids the problems associated with adding resources in the editor that can give a craft a negative part cost. No such restriction applies to vessels reconfigured after launch.
- Lowered the Flapjack cargo bay floor slightly to make it easier to sit kerbals inside using the external command seat.
- Crazy Mode movement is now affected by thrust limit in addition to throttle setting.
- Fixed issue with Crazy Mode failing to function when multiple vessels are loaded in physics range.

0.2.4
- The gravitic engine now consumes GravityWaves while in Hover Mode.
- The gravitic engine now has a built-in Terrain Awareness and Warning System (TAWS) for Crazy Mode. TAWS will halt the craft if using Crazy Mode will result in a crash.
- GravityWaves produced in the gravitic generator will fade away if the generator is turned off. They weren't intended to stick around indefinitely...
- Fixed missing tech tree node icons.
- Fixed issue where resources were consumed while reconfiguring a disassembled part.

0.2.3
- Gravitic engine acceleration now relies on accelerationCurve, which is tied to the throttle setting and thrust limiter. The accelerationCurve will likely need performance tuning...
- Fixed symmetry issues with Flapjack saucer sections.
- Fixed issue where acceleration could be applied even when the gravitic engine wasn't running.
- Fixed NREs generated when a saucer crashes.
- Fixed FX warnings generated when a saucer crashes.
- Fixed OmniStorage not remembering its storage configuration. NOTE: You might need to redo your omni storage containers.
- Fixed OmniStorge issue with KIS volume configuration.

0.2.2
- NRE fixes for turning on/off converters when BARIS isn't installed.
- Fixed missing IntakeLqd on the Hydroscoop.
- Removed RPM dependency - Thanks to MOARdv for updating MAS to support the ASET Avionics HUD!
  NOTE: You'll need MAS 0.16.0.

0.2.1
- Improved the gravitic engine's ability to propel asymmetric craft designs. Aero forces still apply.
- Gravitic engine now bases "forward" on the vessel control point. As an example, forward could be respective to a cockpit or a docking port. VTOL lift is unaffected.
- Excluded experiments from KEI.
- Potentially excluded experiments from [x]Science! Needs testing...
- Fixed issue with tool tip window appearing multiple times.
- Added additional tool tips.

0.2.0 Initial Alpha Pre-Release

---ACKNOWLEDGEMENTS---
Icons made by Freepik from www.flaticon.com

---LICENSE---
Art Assets, including .mu, .mbm, and .dds files are copyright 2017 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2017 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.