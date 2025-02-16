using System;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static System.Math;

namespace VintageRails.Behaviors.Collectibles;

public abstract class CollectibleBehaviorCartEngineBase : CollectibleBehavior, ITickAttachment, IAttachedInteractions {

    public const string EngineTreeAttribute = "vrails.engine";
    public const string MoveBackwardsAttribute = "movesBack";
    public const string TopSpeedAttribute = "topSpeed";
    public const string EnabledAttribute = "enabled";
    
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
        var rider = owner.GetBehavior<EntityBehaviorTrackRider>();
        if (rider is not null) {
            var engineAttributes = self.Itemstack.Attributes.GetOrAddTreeAttribute(EngineTreeAttribute);

            TickEngine(self, rider, engineAttributes, dt);

            bool isWorking = false;
            bool movesBack = false;
            
            if (rider.WasOnTrack) {
                isWorking = IsWorking(self, rider, engineAttributes, dt);
                movesBack = false;
                if (isWorking) {
                    movesBack = ShouldMoveBackwards(self, rider, engineAttributes, dt);
                    var force = GetCurrentForce(self, rider, engineAttributes, movesBack, dt);
                    force *= movesBack ? -1 : 1;
                    
                    rider.Speed += rider.Facing * force * dt;
                    
                    var topSpeed = GetTopSpeed(self, rider, engineAttributes, dt);
                    var internalFriction = (Abs(rider.Speed) - topSpeed) * dt * 5f; //friction coefficient = 1
                    rider.Speed -= Max(internalFriction, 0) * Sign(rider.Speed);
                    
                    AfterWork(self, rider, engineAttributes, dt);
                }
            }
            var animSpeed = GetAnimationSpeed(self, rider, engineAttributes, isWorking, movesBack, dt);
            rider.EngineAnimationSpeed = animSpeed;
        }
        //Does nothing because stupid
        self.MarkDirty();
    }

    protected virtual double GetCurrentForce(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, bool movesBackwards, double dt) {
        return movesBackwards ? backwardForce : forwardForce;
    }

    protected virtual bool ShouldMoveBackwards(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        return engineAttributes.GetBool(MoveBackwardsAttribute, false);
    }

    protected virtual void TickEngine(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) { }

    protected virtual bool IsWorking(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        return IsEnabled(engineAttributes);
    }

    protected abstract void AfterWork(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt);

    protected abstract float GetAnimationSpeed(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt);

    protected virtual float GetTopSpeed(ItemSlot slot, EntityBehaviorTrackRider rider, ITreeAttribute engineAttributes, double dt) {
        return engineAttributes.GetFloat(TopSpeedAttribute);
    }
    
    public static bool IsEnabled(ITreeAttribute engineAttributes) {
        return engineAttributes.GetBool(EnabledAttribute);
    }

    public static void ToggleEnabled(ITreeAttribute engineAttributes) {
        SetEnabled(engineAttributes, !IsEnabled(engineAttributes));
    }
    
    public static void SetEnabled(ITreeAttribute engineAttributes, bool enabled) {
        engineAttributes.SetBool(EnabledAttribute, enabled);
    }

    
    #region Attached Interactions
    public virtual void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity) {
        //Dunno
    }

    public virtual void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity) {
        //Cleanup
    }
    
    public virtual bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity) {
        return true;
    }

    public virtual bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity) {
        itemslot.Itemstack.Attributes.RemoveAttribute(EngineTreeAttribute);
        return true;
    }

    public virtual void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave) {
        if (onEntity.World.Side == EnumAppSide.Server && mode == EnumInteractMode.Interact) {
            var controls = byEntity.Controls;
            if (controls.ShiftKey || controls.CtrlKey) {
                return;
            }
            
            var attribs = itemslot.Itemstack.Attributes.GetOrAddTreeAttribute(EngineTreeAttribute);
            ToggleEnabled(attribs);
        }
    }

    public virtual void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn) {
    }

    public virtual void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath) {
    }

    public virtual void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid,
        byte[] data, ref EnumHandling handled, Action onRequireSave) {
    }
    #endregion
}