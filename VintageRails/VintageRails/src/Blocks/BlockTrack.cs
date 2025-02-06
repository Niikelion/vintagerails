using System.Linq;
using VintageRails.Behaviors;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Blocks
{
    internal class BlockTrack : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemStack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
                return false;

            var block = GetAppropriateBlock(world, byPlayer, blockSel);

            block?.DoPlaceBlock(world, byPlayer, blockSel, itemStack);
            return block != null;
        }

        private Block? GetAppropriateBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.Face.IsHorizontal)
            {
                var block = GetRailBlock(world, "raised_", blockSel.Face.Opposite, blockSel.Face);
                if (block != null) return block;
            }
            
            var blockFacing = SuggestedHVOrientation(byPlayer, blockSel)[0];
            foreach (var toFacing in BlockFacing.HORIZONTALS)
            {
                if (toFacing == blockFacing)
                    continue;

                var block = TryAttachPlaceToHorizontal(world, blockSel.Position, toFacing, blockFacing);

                if (block != null) return block;
            }

            return GetRailBlock(world, "flat_", blockFacing, blockFacing.Opposite);
        }

        private Block? TryAttachPlaceToHorizontal(IWorldAccessor world, BlockPos position, BlockFacing toFacing, BlockFacing targetFacing)
        {
            var blockPos = position.AddCopy(toFacing);
            var block = world.BlockAccessor.GetBlock(blockPos);
            var opposite = toFacing.Opposite;
            
            var track = block.GetBehavior<BlockBehaviorCartTrack>();

            if (track != null)
            {
                if (!track.EndsDirections.Contains(opposite))
                    return null;
            }
            else
            {
                if (block is not BlockTrack)
                    return null;
                
                var facingsFromType = GetFacingsFromType(block.Variant["type"]);

                if (!facingsFromType.Contains(opposite)) return null;
            }
            
            var railBlock = GetRailBlock(world, "curved_", toFacing, targetFacing);
            return railBlock;
        }
        
        private Block? GetRailBlock(IWorldAccessor world, string prefix, BlockFacing dir0, BlockFacing dir1)
        {
            var block = world.GetBlock(CodeWithParts(prefix + dir0.Code[0] + dir1.Code[0]));
            return block ?? world.GetBlock(CodeWithParts(prefix + dir1.Code[0] + dir0.Code[0]));
        }
        
        private static BlockFacing[] GetFacingsFromType(string type)
        {
            string text = type.Split('_')[1];
            return new[]
            {
                BlockFacing.FromFirstLetter(text[0]),
                BlockFacing.FromFirstLetter(text[1])
            };
        }
    }
}
