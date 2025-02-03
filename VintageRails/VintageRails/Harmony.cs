using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageRails;

[HarmonyPatch(typeof(EntityBehaviorSeatable))]
[HarmonyPatch(nameof(EntityBehaviorSeatable.Initialize))]
public class SeatableInitFix {

    static void Prefix(EntityProperties properties, JsonObject attributes, EntityBehaviorSeatable __instance) {
        __instance.Seats = Array.Empty<IMountableSeat>();
    }
    
}