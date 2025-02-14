using System.Linq;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Collectibles;

public class CollectibleBehaviorControlledCartEngine : CollectibleBehaviorCartEngineBase {
    
    public CollectibleBehaviorControlledCartEngine(CollectibleObject collObj) : base(collObj) {
        
    }

    protected override bool IsWorking(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        var controls = GetSeatsControls(slot, rider);
        return controls != null && (controls.Backward || controls.Forward); //No call to base since we don't need this tobe affected by disabling
    }

    protected override bool ShouldMoveBackwards(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        var controls = GetSeatsControls(slot, rider);
        return controls is { Backward: true, Forward: false };
    }

    protected override void AfterWork(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        
    }

    protected override float GetAnimationSpeed(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt) {
        return (float)rider.Speed;
    }

    protected EntityControls? GetSeatsControls(ItemSlot slot, EntityBehaviorTrackRider rider) {
        var seatable = rider.entity.GetBehavior<EntityBehaviorSeatable>();
        var slotId = slot.Inventory.GetSlotId(slot);
        var seat = seatable.Seats.FirstOrDefault(seat => seat.SeatId == "attachableseat-" + slotId);
        if (seat != null) {
            var controls = seat.Controls;
            if (controls != null) {
                return controls;
            }
        }
        return null;
    }
}