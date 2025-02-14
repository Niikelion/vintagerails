using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VintageRails.Behaviors.Callbacks;

public interface ITickAttachment {

    void OnTick(ItemSlot self, Entity owner, double dt);

}