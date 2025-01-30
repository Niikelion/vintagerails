using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors
{
    public class BlockBehaviorOverridePick : BlockBehavior
    {
        private string pickedItem;

        public BlockBehaviorOverridePick(Block block) : base(block) {}

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            
            pickedItem = properties["pickedItem"].AsString();
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventSubsequent;
            return new(world.GetBlock(new AssetLocation(pickedItem)));
        }
    }
}
