using VintageRails.Behaviors.Callbacks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public abstract class CollectibleBehaviorCartEngineBase : CollectibleBehavior, ITickAttachment, IAttachedListener {

    public const string EngineTreeAttribute = "vrails.engine";
    public const string MoveBackwardsAttribute = "movesBack";
    
    private double forwardForce;
    private double backwardForce;
    
    public CollectibleBehaviorCartEngineBase(CollectibleObject collObj) : base(collObj) {
        
    }

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);

        forwardForce = properties["forwardForce"].AsDouble();
        backwardForce = properties["backwardForce"].AsDouble();
    }

    public virtual void OnTick(ItemSlot self, Entity owner, double dt) {
        var rider = owner.GetBehavior<TrackRiderEntityBehavior>();
        if (rider is { WasOnTrack: true }) {
            var engineAttributes = self.Itemstack.Attributes.GetOrAddTreeAttribute(EngineTreeAttribute);

            var isWorking = IsWorking(self, rider, engineAttributes, dt);
            var movesBack = false;
            if (isWorking) {
                movesBack = ShouldMoveBackwards(self, rider, engineAttributes, dt);
                var force = GetCurrentForce(self, rider, engineAttributes, movesBack, dt);
                force *= movesBack ? -1 : 1;

                rider.Speed += rider.Facing * force * dt;

                AfterWork(self, rider, engineAttributes, dt);
            }

            var animSpeed = GetAnimationSpeed(self, rider, engineAttributes, isWorking, movesBack, dt);
            rider.EngineAnimationSpeed = animSpeed;
        }
    }

    public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity) {
        //Dunno
    }

    public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity) {
        //Cleanup
        itemslot.Itemstack.Attributes.RemoveAttribute(EngineTreeAttribute);
    }

    protected virtual double GetCurrentForce(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, bool movesBackwards, double dt) {
        return movesBackwards ? backwardForce : forwardForce;
    }

    protected virtual bool ShouldMoveBackwards(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        return engineAttributes.GetBool(MoveBackwardsAttribute, false);
    }

    protected abstract bool IsWorking(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt);

    protected abstract void AfterWork(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt);

    protected abstract float GetAnimationSpeed(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt);

}