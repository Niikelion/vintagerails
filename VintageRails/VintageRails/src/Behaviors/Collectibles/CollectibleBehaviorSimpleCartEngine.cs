using System;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace VintageRails.Behaviors.Collectibles;

public class CollectibleBehaviorSimpleCartEngine : CollectibleBehaviorCartEngineBase {
    
    public const string FuelTimeAttribute = "fuelTime";
    
    public CollectibleBehaviorSimpleCartEngine(CollectibleObject collObj) : base(collObj) {
    }

    protected override bool IsWorking(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        return base.IsWorking(slot, rider, engineAttributes, dt) && engineAttributes.GetDouble(FuelTimeAttribute) > 0;
    }

    protected override void AfterWork(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        var current = engineAttributes.GetDouble(FuelTimeAttribute);
        current = Math.Max(current - dt, 0.0);
        engineAttributes.SetDouble(FuelTimeAttribute, current);
        
        slot.MarkDirty();
    }

    protected override float GetAnimationSpeed(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt) {
        return isWorking ? 1 : 0;
    }

    public static void AddFuel(ItemSlot slot, double amount) {
        var tree = slot.Itemstack.Attributes.GetOrAddTreeAttribute(EngineTreeAttribute);
        var fuel = tree.GetDouble(FuelTimeAttribute);
        tree.SetDouble(FuelTimeAttribute, fuel + amount);
        slot.MarkDirty();
    }
}