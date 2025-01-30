using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Blocks
{
    internal class BlockTrack : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }

            var blockFacing = SuggestedHVOrientation(byPlayer, blockSel)[0];
            Block block = null;
            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
            {
                var toFacing = BlockFacing.HORIZONTALS[i];
                if (TryAttachPlaceToHoriontal(world, byPlayer, blockSel.Position, toFacing, blockFacing))
                {
                    return true;
                }
            }

            block ??= GetStraightOrRaisedVariant(world, blockFacing, blockSel.Position);

            if (blockFacing.IsAxisWE)
                block ??= world.GetBlock(CodeWithParts("flat_we"));

            block ??= world.GetBlock(CodeWithParts("flat_ns"));

            block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            return true;
        }

        private Block GetStraightOrRaisedVariant(IWorldAccessor world, BlockFacing blockFacing, BlockPos position)
        {
            if (blockFacing.IsVertical)
                return world.GetBlock(CodeWithParts("flat_ns"));

            var nextUpBlock = position.AddCopy(blockFacing).AddCopy(BlockFacing.UP);

            if (world.BlockAccessor.GetBlock(nextUpBlock) is BlockTrack)
                return GetRailBlock(world, "raised_", blockFacing, blockFacing.Opposite);

            if (blockFacing.IsAxisWE)
                return world.GetBlock(CodeWithParts("flat_we"));

            return world.GetBlock(CodeWithParts("flat_ns"));
        }

        private bool TryAttachPlaceToHoriontal(IWorldAccessor world, IPlayer byPlayer, BlockPos position, BlockFacing toFacing, BlockFacing targetFacing)
        {
            var blockPos = position.AddCopy(toFacing);
            var block = world.BlockAccessor.GetBlock(blockPos);
            if (block is not BlockTrack)
            {
                return false;
            }

            var opposite = toFacing.Opposite;
            var facingsFromType = GetFacingsFromType(block.Variant["type"]);
            if (world.BlockAccessor.GetBlock(blockPos.AddCopy(facingsFromType[0])) is BlockTrack && world.BlockAccessor.GetBlock(blockPos.AddCopy(facingsFromType[1])) is BlockTrack)
            {
                return false;
            }

            var openedEndedFace = GetOpenedEndedFace(facingsFromType, world, position.AddCopy(toFacing));
            if (openedEndedFace == null)
            {
                return false;
            }

            var railBlock = GetRailBlock(world, "curved_", toFacing, targetFacing);
            if (railBlock != null)
            {
                return PlaceIfSuitable(world, byPlayer, railBlock, position);
            }

            string text = block.Variant["type"].Split('_')[1];
            var dir = ((text[0] == openedEndedFace.Code[0]) ? BlockFacing.FromFirstLetter(text[1]) : BlockFacing.FromFirstLetter(text[0]));
            var railBlock2 = GetRailBlock(world, "curved_", dir, opposite);
            if (railBlock2 == null)
            {
                return false;
            }

            railBlock2.DoPlaceBlock(world, byPlayer, new BlockSelection
            {
                Position = position.AddCopy(toFacing),
                Face = BlockFacing.UP
            }, null);
            return false;
        }

        private static bool PlaceIfSuitable(IWorldAccessor world, IPlayer byPlayer, Block block, BlockPos pos)
        {
            string failureCode = "";
            var blockSel = new BlockSelection
            {
                Position = pos,
                Face = BlockFacing.UP
            };
            if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                block.DoPlaceBlock(world, byPlayer, blockSel, null);
                return true;
            }

            return false;
        }

        private Block GetRailBlock(IWorldAccessor world, string prefix, BlockFacing dir0, BlockFacing dir1)
        {
            var block = world.GetBlock(CodeWithParts(prefix + dir0.Code[0] + dir1.Code[0]));
            if (block != null)
            {
                return block;
            }

            return world.GetBlock(CodeWithParts(prefix + dir1.Code[0] + dir0.Code[0]));
        }

        private static BlockFacing GetOpenedEndedFace(BlockFacing[] dirFacings, IWorldAccessor world, BlockPos blockPos)
        {
            if (!(world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[0])) is BlockTrack))
            {
                return dirFacings[0];
            }

            if (world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[1])) is not BlockTrack)
            {
                return dirFacings[1];
            }

            return null;
        }

        private static BlockFacing[] GetFacingsFromType(string type)
        {
            string text = type.Split('_')[1];
            return new BlockFacing[2]
            {
                BlockFacing.FromFirstLetter(text[0]),
                BlockFacing.FromFirstLetter(text[1])
            };
        }
    }
}
