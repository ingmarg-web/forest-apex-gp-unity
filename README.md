# Forest Apex GP

Forest Apex GP is a mobile-first arcade/simcade racer being migrated to **Unity 6 + C# + Universal Render Pipeline (URP)**.

The previous Forest Apex GP v3.6.0 design remains the golden reference for driving feel and product direction: analogue steering, separate throttle/brake, three cars, three distinct circuits, garage/event flow, AI opponents, speed-sensitive camera and Android performance.

## Current Unity foundation

- Unity 6 / URP
- Android landscape setup
- garage / car selection
- event / circuit selection
- three cars with different tuning
- Pine Ridge technical mountain/forest circuit
- Sunset Gulch high-speed desert circuit
- Neon Downtown night circuit with a driveable elevated bridge
- procedural track/environment generation
- Rigidbody arcade/simcade vehicle dynamics
- analogue touch steering
- independent GAS and BRAKE touch controls
- AI opponents with lane offsets
- lap / progress / position tracking
- countdown, HUD and result flow
- speed-sensitive chase camera / FOV
- GitHub Actions Android APK workflow

## Open in Unity

Recommended editor: **Unity 6000.0.36f1**.

Run **Forest Apex > Generate Playable Scene**, then open **Assets/Scenes/ForestApex.unity**.

## Android

Run **Forest Apex > Build Android APK**. Output: **build/Android/ForestApexGP.apk**.

The GitHub Actions workflow can build the APK after the standard GameCI Unity license secrets are configured.

## Direction

This is the architectural Unity foundation, not the final visual-art pass. Gameplay, AI, race flow and track identity are kept separate from production assets so later passes can replace procedural geometry with proper meshes, textures, shaders, audio and VFX without another rewrite.
