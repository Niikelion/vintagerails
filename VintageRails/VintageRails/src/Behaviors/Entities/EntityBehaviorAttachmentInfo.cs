using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Entities;

public class EntityBehaviorAttachmentInfo : EntityBehavior, ICustomInteractionHelpPositioning {

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


    public Vec3d? GetInteractionHelpPosition() {
        ICoreClientAPI api = (ICoreClientAPI)entity.Api;
        if (api.World.Player.CurrentEntitySelection == null)
            return null;
        int selectionBoxIndex = api.World.Player.CurrentEntitySelection.SelectionBoxIndex - 1;
        if (selectionBoxIndex >= 0) 
            return entity.GetBehavior<EntityBehaviorSelectionBoxes>().GetCenterPosOfBox(selectionBoxIndex)?.Add(0.0, 0.5, 0.0);
        else {
            return entity.Pos.XYZ.AddCopy(0, 1, 0);
        }
    }

    public bool TransparentCenter => false;
}