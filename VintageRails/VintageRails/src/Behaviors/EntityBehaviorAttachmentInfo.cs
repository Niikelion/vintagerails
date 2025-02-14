using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class EntityBehaviorAttachmentInfo : EntityBehavior {

    [NotNull] private EntityBehaviorAttachable? Attachable { get; set; }

    public EntityBehaviorAttachmentInfo(Entity entity) : base(entity) {
        
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        Attachable = entity.GetBehavior<EntityBehaviorAttachable>();
    }

    public override string PropertyName() {
        return "vrails_attachment_help";
    }

    public override WorldInteraction[]? GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled) {
        handled = EnumHandling.PassThrough;
        var box = es.SelectionBoxIndex;
        var slot = Attachable.GetSlotFromSelectionBoxIndex(box);
        if (slot == null) {
            return null;
        }
        var stack = slot.Itemstack;

        if (stack != null) {
            var collectible = stack.Collectible;
            var infoHandlers = collectible.GetCollectibleInterfaces<IInfoAttachment>();
            var infos = new List<WorldInteraction>();
            foreach (var infoHandler in infoHandlers) {
                 infos.AddRange(infoHandler.GetInteractionHelps(slot, entity));
            }
            handled = EnumHandling.Handled;
            return infos.ToArray();
        }

        return null;
    }

    public override void GetInfoText(StringBuilder infotext) {
        foreach (var slot in Attachable.Inventory) {
            var stack = slot.Itemstack;
            if (stack == null) {
                continue;
            }
            
            var collectible = stack.Collectible;
            var infoHandlers = collectible.GetCollectibleInterfaces<IInfoAttachment>();
            foreach (var infoHandler in infoHandlers) {
                infoHandler.GetInfo(slot, entity, infotext);
            }
        }
    }
}