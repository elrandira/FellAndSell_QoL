# Unity Modding with BepInEx and Harmony

## Overview

- BepInEx: A plugin framework and patcher for Unity games (Mono and IL2CPP).
- MelonLoader: An alternative mod loader for Unity games (Mono and IL2CPP) that loads mods called "Melons" and provides its own config system (MelonPreferences) and logging.
- Harmony: A .NET library for runtime method patching. Both BepInEx and MelonLoader use it.

BepInEx and MelonLoader both load your mod into the game and handle configuration and logging, while Harmony changes the game's behavior at runtime. A mod is written for one loader, so choose either BepInEx or MelonLoader for a given project.

---

## BepInEx

### Responsibilities

- **Loading plugins**: Injects into the game process at startup and loads DLLs from `BepInEx/plugins`.
- **Plugin structure**: Plugins derive from `BaseUnityPlugin`, are marked with `[BepInPlugin]` (GUID, name, version), and typically initialize in `Awake()`.
- **Configuration**: `Config.Bind` for user-editable settings.
- **Logging**: `Logger` for output.
- **Chainloader and patchers**: Resolves dependencies between plugins and offers a patcher system to modify assemblies before the game loads them.
- **IL2CPP support**: Uses Il2CppInterop and related tooling so managed code can be written against generated interop assemblies.

## Harmony

### Patch Types

- **Prefix**: Runs before the original method. Returning `false` skips the original.
- **Postfix**: Runs after the original method.
- **Finalizer**: Handles exceptions thrown by the original method.
- **Transpiler**: Rewrites the method's IL instructions.

### Usage

- **Attribute-based patching**: `[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.Update))]`
- **Manual patching**: `new Harmony(id).PatchAll()` applies all annotated patches; `Harmony.Patch(...)` patches specific methods.
- **Accessing private members**: `Traverse` and `AccessTools` reach private fields and methods.
- **Patch priority**: `[HarmonyBefore]` and `[HarmonyAfter]` manage ordering between mods.

## MelonLoader

MelonLoader is an alternative to BepInEx for loading mods into Unity games. It is a mod loader for Unity games that supports both Mono and IL2CPP builds. Like BepInEx, it injects into the game process at startup and loads your mod assemblies. Its mods are called Melons, and it has its own plugin base classes rather than BepInEx's.

---

## Mono vs. IL2CPP

- Method names, types, and assemblies can differ between the two.
- IL2CPP requires interop types instead of the original managed types.

---

## Common Pitfalls

- **Game updates** can rename or restructure methods, breaking patches.
- Use `AccessTools` and `nameof` to catch mismatches early.
- Inspect the game's assembly with **dnSpy** or **ILSpy** to confirm names.
- Watch for conflicts with other mods and set patch priority where needed.

---

## Code Examples with BepInEx

### Plugin Skeleton

```csharp
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.yourname.fellandsell.qol", "Fell & Sell QoL", "1.0.0")]
public class QoLPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("com.yourname.fellandsell.qol").PatchAll();
        Logger.LogInfo("Fell & Sell QoL loaded");
    }
}
```

### Postfix: Modify a Return Value

```csharp
[HarmonyPatch(typeof(GameNamespace.PlayerStats), "get_MoveSpeed")]
class MoveSpeedPatch
{
    static void Postfix(ref float __result)
    {
        __result *= 1.5f; // faster movement
    }
}
```

### Prefix: Skip the Original Method

```csharp
[HarmonyPatch(typeof(GameNamespace.ShopUI), "ShowConfirmDialog")]
class SkipConfirmPatch
{
    static bool Prefix()
    {
        return false; // skip original; handle the action yourself
    }
}
```

### Accessing Private Fields with Traverse

```csharp
[HarmonyPatch(typeof(GameNamespace.Inventory), "AddItem")]
class InventoryCapPatch
{
    static void Prefix(GameNamespace.Inventory __instance)
    {
        var cap = Traverse.Create(__instance).Field("maxSlots");
        cap.SetValue(cap.GetValue<int>() + 10);
    }
}
```

Replace GameNamespace and the class/method names with the real ones from Fell & Sell's assembly.

## Code Examples with MelonLoader

### Plugin Skeleton

```csharp
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(QoLMod.QoLMelon), "Fell & Sell QoL", "1.0.0", "yourname")]
[assembly: MelonGame(null, null)] // set your game's developer and name if needed

namespace QoLMod
{
    public class QoLMelon : MelonMod
    {
        private MelonPreferences_Entry<bool> enableFeature;

        public override void OnInitializeMelon()
        {
            var category = MelonPreferences.CreateCategory("FellAndSellQoL");
            enableFeature = category.CreateEntry("EnableFeature", true);

            HarmonyInstance.PatchAll();
            LoggerInstance.Msg("Fell & Sell QoL loaded");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene loaded: {sceneName}");
        }
    }
}
```

### Config example

```csharp
var category = MelonPreferences.CreateCategory("FellAndSellQoL");
var moveBoost = category.CreateEntry("MoveSpeedMultiplier", 1.5f);

// Later, in a patch:
float multiplier = moveBoost.Value;
```

Settings are saved to UserData/MelonPreferences.cfg, which users can edit directly.

### Installation

1. Download the MelonLoader installer from the official site or GitHub.
1. Run it and point it at the game's executable.
1. Choose the correct Unity version and build type (Mono or IL2CPP).
1. Launch the game once so MelonLoader generates its folders and, for IL2CPP, the interop assemblies.
1. Place your compiled .dll in the Mods/ folder.

## Typical QoL Features

| Feature | Approach | 
| --- | --- |
| Bigger inventory or carry capacity | Postfix on capacity getters, or prefix on add methods | 
| Skip intro/confirmation dialogs | Prefix returning false | 
| Auto-save or hotkeys | Postfix on Update() checking Input.GetKeyDown | 
| Config toggles | Config.Bind in Awake(), checked inside each patch | 

## Finding Real Targets in Fell & Sell

1. Locate Managed/Assembly-CSharp.dll (Mono) or the IL2CPP dumps.
1. Open it in [dnSpy](https://github.com/dnspy/dnspy) or [ILSpy](https://github.com/icsharpcode/ilspy) and search for keywords such as "inventory", "price", "shop", "speed".
1. Note the class and method names and substitute them into the examples above.

