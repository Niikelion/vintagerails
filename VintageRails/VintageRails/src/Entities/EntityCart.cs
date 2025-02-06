using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VintageRails.Entities
{
    internal class EntityCart : Entity
    {
        private double baseSpeed = 0.05;
        private string dropItem = "vintagerails:minecart";

        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            base.Initialize(properties, api, inChunkIndex3d);

            if (properties.Attributes == null) return;
            
            baseSpeed = properties.Attributes["baseSpeed"].AsDouble(baseSpeed);
            dropItem = properties.Attributes["dropItem"].AsString(dropItem);
        }
    }
}
