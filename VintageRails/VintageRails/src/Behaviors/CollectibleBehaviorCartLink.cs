using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageRails.Behaviors;

public class CollectibleBehaviorCartLink : CollectibleBehavior {

    public const string CartEntityIdAttribute = "vrails.cartId";
    
    public CollectibleBehaviorCartLink(CollectibleObject collObj) : base(collObj) {
        
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection? entitySel,
        bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        handling = EnumHandling.PreventSubsequent;
        handHandling = EnumHandHandling.Handled;

        if (byEntity.Api is ICoreServerAPI sapi && entitySel != null) {
            var ent = entitySel.Entity;
            var cart = ent.GetBehavior<TrackRiderEntityBehavior>();

            if (cart == null) {
                return;
            }
            
            if (!byEntity.Controls.ShiftKey) {
                var id = ent.EntityId;
                slot.Itemstack.Attributes.SetLong(CartEntityIdAttribute, id);
            }
            else {
                var id = slot.Itemstack.Attributes.GetLong(CartEntityIdAttribute);
                var ent2 = sapi.World.GetEntityById(id);

                var cart2 = ent2.GetBehavior<TrackRiderEntityBehavior>();
                
                sapi.Server.LogDebug($"from: {id} | to: {ent.EntityId} => distance: {RailUtil.GetDistanceOnTrack(cart, cart2, CartDirection.Forward, 5)}");
            }
        }
    }
}