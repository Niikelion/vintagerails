using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Utils;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Entities;

public class EntityBehaviorTickingAttachments : EntityBehavior, IOrderedPhysicsTickBehavior {
    
    public IEnumerable<Type> Before { get; } = new[] { typeof(EntityBehaviorTrackRider) };

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

            var tickers = collectible.GetCollectibleInterfaces<ITickAttachment>();
            foreach (var ticker in tickers) {
                ticker.OnTick(slot, entity, dt);
            }
        }
    }
}