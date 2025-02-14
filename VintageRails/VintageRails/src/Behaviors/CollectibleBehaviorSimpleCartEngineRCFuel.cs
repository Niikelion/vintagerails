using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class CollectibleBehaviorSimpleCartEngineRCFuel : CollectibleBehavior, IAttachedInteractions {

    private double fuelPerClick;
    
    public CollectibleBehaviorSimpleCartEngineRCFuel(CollectibleObject collObj) : base(collObj) {
    }

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);

        fuelPerClick = properties["fuelPerClick"].AsDouble();
    }

    public void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition,
        EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave) {

        if (mode == EnumInteractMode.Interact) {
            if (onEntity.World.Side == EnumAppSide.Server) {
                var stack = itemslot.Itemstack;
                var collectible = stack.Collectible;
                var simpleEngine = collectible.GetCollectibleBehavior<CollectibleBehaviorSimpleCartEngine>(true);

                if (simpleEngine != null) {
                    CollectibleBehaviorSimpleCartEngine.AddFuel(itemslot, fuelPerClick);
                    handled = EnumHandling.PreventSubsequent;
                }
            }
        }
    }


    public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity) {
    }

    public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity) {
    }

    public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity) {
        return true;
    }

    public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity) {
        return true;
    }

    public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn) {
        
    }

    public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath) {
        
    }

    public void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid,
        byte[] data, ref EnumHandling handled, Action onRequireSave) {
        
    }
    
}