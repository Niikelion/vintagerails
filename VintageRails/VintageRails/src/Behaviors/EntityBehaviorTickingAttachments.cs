using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Behaviors.Callbacks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class EntityBehaviorTickingAttachments : EntityBehavior, IOrderedPhysicsTickBehavior {
    
    public IEnumerable<Type> Before { get; } = new[] { typeof(TrackRiderEntityBehavior) };

    [NotNull] private EntityBehaviorContainer? Container { get; set; }

    public EntityBehaviorTickingAttachments(Entity entity) : base(entity) {
        
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);
        Container = entity.GetBehavior<EntityBehaviorAttachable>();
    }

    public override string PropertyName() {
        return "vrails_ticking_attachments";
    }
    
    public void OnTick(double dt) {
        var inv = Container.Inventory;
        for (int i = 0; i < inv.Count; i++) {
            var slot = inv[i];
            var stack = slot.Itemstack;
            if (stack == null) {
                return;
            }

            var collectible = stack.Collectible;

            var ticker = collectible.GetCollectibleInterface<ITickAttachment>();
            if (ticker != null) {
                ticker.OnTick(slot, entity, dt);
            }
        }
    }
}