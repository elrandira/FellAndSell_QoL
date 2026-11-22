# Modding with MelonLoader and Harmony

## Project structure

```
FellAndSellQoL/
├── FellAndSellQoL.sln
├── FellAndSellQoL/                 # Mod project
│   ├── FellAndSellQoL.csproj
│   ├── QoLMelon.cs                 # Entry point (MelonMod subclass)
│   ├── Settings.cs                 # MelonPreferences definitions
│   ├── Patches/                    # Harmony patch classes
│   │   ├── PlayerPatches.cs
│   │   └── UIPatches.cs
│   ├── Features/                   # Feature logic, independent of patches
│   │   ├── MoveSpeedFeature.cs
│   │   └── AutoSellFeature.cs
│   └── Util/                       # Shared helpers
│       └── GameRefs.cs             # Cached references to game objects/managers
├── Libs/                           # Copied DLLs from the game (not committed)
│   ├── MelonLoader.dll
│   ├── 0Harmony.dll
│   ├── UnityEngine.dll
│   ├── UnityEngine.CoreModule.dll
│   └── Assembly-CSharp.dll         # or the IL2CPP interop assemblies
├── .gitignore
└── README.md
```

### Responsibilities

- QoLMelon.cs: Only the MelonMod subclass. It sets up preferences, calls HarmonyInstance.PatchAll(), and forwards scene events to features. Keep it thin.
- Settings.cs: Creates the MelonPreferences category and entries in one place, so every feature reads config from a single source.
- Patches/: Only [HarmonyPatch] classes. Each patch should be small and delegate to a feature method. This keeps game-specific method names isolated here, so if the game updates you only fix this folder.
- Features/: The actual mod behavior as plain C# with no Harmony attributes. This makes the logic easier to read and test.
- Util/: Shared helpers such as cached manager lookups and logging wrappers.
- Libs/: References to the game's DLLs. These are game-specific, so don't commit them to the repository.

### Examples

- QolMelon.cs

```Csharp
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(FellAndSellQoL.QoLMelon), "Fell & Sell QoL", "1.0.0", "yourname")]

namespace FellAndSellQoL
{
    public class QoLMelon : MelonMod
    {
        public static QoLMelon Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Settings.Initialize();
            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("Loaded");
        }
    }
}
```

- Settings.cs

```Csharp
using MelonLoader;

namespace FellAndSellQoL
{
    public static class Settings
    {
        public static MelonPreferences_Entry<bool> AutoSell;
        public static MelonPreferences_Entry<float> MoveMultiplier;

        public static void Initialize()
        {
            var cat = MelonPreferences.CreateCategory("FellAndSellQoL");
            AutoSell = cat.CreateEntry("AutoSell", true);
            MoveMultiplier = cat.CreateEntry("MoveMultiplier", 1.5f);
        }
    }
}
```

- Patches/PlayerPatches.cs

```Csharp
using HarmonyLib;
using FellAndSellQoL.Features;

namespace FellAndSellQoL.Patches
{
    // Replace the target type and method with the actual game names.
    [HarmonyPatch(typeof(PlayerController), "GetMoveSpeed")]
    public static class PlayerMoveSpeedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            __result = MoveSpeedFeature.Apply(__result);
        }
    }
}
```

- Features/MoveSpeedFeature.cs

```Csharp
namespace FellAndSellQoL.Features
{
    public static class MoveSpeedFeature
    {
        public static float Apply(float baseSpeed)
        {
            return baseSpeed * Settings.MoveMultiplier.Value;
        }
    }
}
```

- Project File

Set the references to the game's DLLs with Private=false so they aren't copied into the output. The output DLL goes into the game's Mods/ folder.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>FellAndSellQoL</AssemblyName>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="MelonLoader">
      <HintPath>..\Libs\MelonLoader.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>..\Libs\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>..\Libs\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>..\Libs\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <Target Name="CopyToMods" AfterTargets="Build">
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(GameDir)\Mods" Condition="'$(GameDir)' != ''" />
  </Target>
</Project>
```

Set GameDir to the game's install folder, for example in a Directory.Build.props file or on the command line with -p:GameDir="C:\Games\FellAndSell".

Notes

- Target framework: MelonLoader's Mono games typically use .NET Framework 4.x or .NET Standard 2.1, and IL2CPP games use .NET 6. Match the target to what your game's MelonLoader build expects.
- IL2CPP vs Mono: If Fell & Sell is IL2CPP, reference the generated interop assemblies (for example Il2Cppmscorlib.dll and Il2CppAssembly-CSharp.dll) in Libs/ instead of Assembly-CSharp.dll, and use the Il2Cpp type names.
- Game type names: PlayerController and GetMoveSpeed are placeholders. Use a decompiler such as dnSpy or ILSpy on the game's assemblies to find the real names.


## Shipping a MelonLoader Mod

Shipping a MelonLoader mod means building the DLL, packaging it with a README, and giving players clear install steps. The output is a single `.dll` file.

## 1. Build a Release

From the project folder:

```bash
dotnet build -c Release
```

Or in Visual Studio, set the configuration dropdown to Release and choose Build > Build Solution.
The compiled file is written to:
          
```
FellAndSellQoL/bin/Release/net6.0/FellAndSellQoL.dll
```

(The framework folder depends on your TargetFramework, for example net472 or netstandard2.1.)
If you set up the CopyToMods target from earlier, the DLL is also copied to the game's Mods/ folder automatically. Test it there first.

## 2. Check the Output

Before shipping, verify:

- The file is named FellAndSellQoL.dll and is only a few KB to a few hundred KB in size.
- It contains only your code. Dependencies like MelonLoader.dll, 0Harmony.dll, and UnityEngine*.dll should not be in the output (that's what Private=false ensures). Shipping them can cause version conflicts.
- Test it in the game: start the game, confirm the "Loaded" message appears in the MelonLoader console, and check that your settings appear in UserData/MelonPreferences.cfg.

## 3. Package the Release

Create a zip archive with this structure:

```
FellAndSellQoL-v1.0.0.zip
├── Mods/
│   └── FellAndSellQoL.dll
├── README.md
└── LICENSE            (optional)
```

Preferred: put the .dll in Mods/ so players can extract the zip directly into the game folder.
Include a README.md covering:

- Requirements: the MelonLoader version (for example, "MelonLoader 0.6.x") and the game version the mod was built and tested against.
- Installation: install MelonLoader first, then copy FellAndSellQoL.dll into the game's Mods/ folder.
- Configuration: the settings and where they are stored (UserData/MelonPreferences.cfg).
- Uninstall: delete the DLL.
- Known issues and the version or license information.

## 4. Publish

Common places to share a MelonLoader mod:

- GitHub Releases: upload the zip as a release asset and tag the version. Push the source code too if you want it open.
- Nexus Mods (the game's mod page, if one exists): upload the zip and follow its rules.
- Thunderstore: supports MelonLoader mods for some games. Check whether Fell & Sell has a community page there.

Use semantic versioning (1.0.0, 1.0.1, and so on) and keep the MelonInfo version in QoLMelon.cs in sync with the release name. Mention the game version each release supports, since a game update can break Harmony patches.


# References

- https://www.youtube.com/watch?v=czkk3UwSMm8, "How to create a mod for schedule I", Scheduled Modder 05.04.2025
- https://steamcommunity.com/sharedfiles/filedetails/?id=2854936721, "How to create your own mod with MelonLoadder, Average Enjoyer, 27.08.2022
