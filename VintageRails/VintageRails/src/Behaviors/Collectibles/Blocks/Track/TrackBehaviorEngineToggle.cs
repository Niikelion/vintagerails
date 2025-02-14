using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Collectibles.Blocks.Track;

public class TrackBehaviorEngineToggle : ITrackBehavior {
    
    private readonly EngineToggleType _toggleType;
    
    public static ITrackBehavior Create(JsonObject properties) {
        return new TrackBehaviorEngineToggle(properties);
    }
    
    private TrackBehaviorEngineToggle(JsonObject properties) {
        _toggleType = properties["toggleType"].AsObject(EngineToggleType.Toggle);
    }

    public void OnCartEntered(EntityBehaviorTrackRider rider, BlockPos currentTrackPos, BlockPos previousTrackPos) {
        var container = rider.entity.GetBehavior<EntityBehaviorContainer>();

        if (container == null) {
            return;
        }

        var slot = container.Inventory[0];
        var stack = slot.Itemstack;
        if (stack == null) {
            return;
        }

        var engine = stack.Collectible.GetBehavior<CollectibleBehaviorCartEngineBase>();
        if (engine == null) {
            return;
        }

        var attributes = stack.Attributes.GetOrAddTreeAttribute(CollectibleBehaviorCartEngineBase.EngineTreeAttribute);

        switch(_toggleType) {
            case EngineToggleType.Enable:
                CollectibleBehaviorCartEngineBase.SetEnabled(attributes, true);
                break;
            case EngineToggleType.Disable:
                CollectibleBehaviorCartEngineBase.SetEnabled(attributes, false);
                break;
            case EngineToggleType.Toggle:
                CollectibleBehaviorCartEngineBase.ToggleEnabled(attributes);
                break;
        }
    }

    public enum EngineToggleType {
        Enable,
        Disable,
        Toggle
    }
}