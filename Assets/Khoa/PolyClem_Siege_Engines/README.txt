POLYCLEM SIEGE ENGINES
Animated ballista and onager, separate bolt and stone. Blender sources, FBX and GLB exports.

CONTENTS
Ballista: 2,032 triangles. Onager: 4,128 triangles. Bolt: 144 triangles. Stone: 80 triangles.
Fire / Reload clips: Ballista 1 s / 5 s; Onager 1 s / 3 s.
One assembled Siege_Engines_Demo scene: a 12-second loop with both engines and their ammunition displayed beside them. This Blender scene is included only in the complete and Blender downloads.

USE
Blender sources (when included): open Ballista.blend or Onager.blend, select the rig and choose AN_Ballista_Fire / AN_Ballista_Reload or AN_Onager_Fire / AN_Onager_Reload in the Action Editor. Set frames 1–25 for Fire, 1–121 for Ballista Reload, or 1–73 for Onager Reload, at 24 fps. The Gameplay_Cycle action shows the full cycle; Siege_Engines_Demo.blend opens the assembled loop at 96 fps.
Exchange files (when included): import the whole hierarchy. Each Fire or Reload file contains the named clip, sampled at 96 poses/second without changing its duration. GLB clip labels are SCENE_Ballista_Fire, SCENE_Ballista_Reload, SCENE_Onager_Fire and SCENE_Onager_Reload.
Units: metres. Preserve root/scale transforms. UVs and solid-colour materials are included; no texture files are required.

PROJECTILES
Attach the bolt to SOCKET_Ballista_Projectile_Anchor, or the stone to SOCKET_Onager_Projectile_Anchor. Both rigs also have a Projectile_Anchor bone. At 0.125 seconds in Ballista Fire or 0.375 seconds in Onager Fire, detach/spawn the projectile and control its flight in your application. Event timing is documented here; create the event in your engine. The bolt points along Blender local +Y, with its origin at the tail; the stone origin is at its centre. Verify axis conversion on import.

COMPATIBILITY AND LICENSE
Saved and checked in Blender 5.2. Unity/Unreal integration is not tested; gameplay logic and engine collision setup are not included. See LICENSE.txt for direct-download terms and the ArtStation license scope.
Parts of this pack were developed with AI assistance.
