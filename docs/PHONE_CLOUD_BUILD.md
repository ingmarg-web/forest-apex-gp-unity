# Build Forest Apex GP from a phone with Unity Build Automation

The GitHub GameCI route needs a Unity license secret. For a phone-only workflow, use Unity Build Automation instead: Unity handles the Editor/build environment and you do not need a local Unity installation.

Repository:
https://github.com/ingmarg-web/forest-apex-gp-unity

Recommended configuration:

- Source control: Git
- Repository: https://github.com/ingmarg-web/forest-apex-gp-unity.git
- Branch: main
- Unity version: Auto-detect from ProjectSettings/ProjectVersion.txt (6000.0.36f1)
- Platform: Android
- Build App Bundle: OFF (we want an APK for direct testing)
- Scripting backend: project pre-export config sets IL2CPP
- Architecture: project pre-export config sets ARM64
- Pre-export method: ForestApex.Editor.ForestApexBuild.PrepareCloudBuild
- Scenes: allow project Build Settings after the pre-export method creates Assets/Scenes/ForestApex.unity

The pre-export method creates the playable scene and applies Android PlayerSettings before Unity exports the player.

After the build succeeds, download the Android artifact from Unity Build Automation and install the APK on the phone.
