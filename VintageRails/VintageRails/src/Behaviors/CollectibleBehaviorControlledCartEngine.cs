using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class CollectibleBehaviorControlledCartEngine : CollectibleBehaviorCartEngineBase {
    
    public CollectibleBehaviorControlledCartEngine(CollectibleObject collObj) : base(collObj) {
        
    }

    protected override bool IsWorking(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        var seatable = rider.entity.GetBehavior<EntityBehaviorSeatable>();
        var slotId = slot.Inventory.GetSlotId(slot);
        var seat = seatable.Seats.FirstOrDefault(seat => seat.SeatId == "attachableseat-" + slotId);
        if (seat != null) {
            var controls = seat.Controls;
            if (controls != null) {
                return controls.Backward || controls.Forward;
            }
        }
        return false;
    }

    protected override void AfterWork(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        
    }

    protected override float GetAnimationSpeed(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt) {
        return (float)rider.Speed;
    }
    
}