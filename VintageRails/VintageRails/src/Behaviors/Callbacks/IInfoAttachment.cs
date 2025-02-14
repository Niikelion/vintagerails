using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VintageRails.Behaviors.Callbacks;

public interface IInfoAttachment {

    WorldInteraction[] GetInteractionHelps(ItemSlot thisSlot, Entity thisEntity);
    
    void GetInfo(ItemSlot thisSlot, Entity thisEntity, StringBuilder infoText);

}