using System;
using System.Reflection.Metadata;
using VintageRails.Behaviors;
using VintageRails.Rails;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Util;

public static class RailUtil {

    public static (BlockBehaviorCartTrack? track, BlockPos foundAt) GetTrackData(this IWorldAccessor world, Vec3d pos) {
        var bp = pos.AsBlockPos;
        var track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);
;
        if (track == null) {
            var offset = new BlockPos(0, 1, 0);
            if ((pos.Y % 1.0) < 0.5) {
                offset *= -1;
            }

            bp.Add(offset);
            return (world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp), bp);
        }

        return (track, bp);
    }

    public static T? GetBlockBehaviour<T>(this IWorldAccessor world, BlockPos pos) where T : BlockBehavior{
        var block = world.BlockAccessor.GetBlock(pos);
        
        if (block == null) {
            return null;
        } 
        return block.GetBehavior<T>();
    }
    
}