using System;
using VintageRails.Behaviors;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Utils;

public static class RailUtil {
    public static (BlockBehaviorCartTrack? track, BlockPos foundAt) GetTrackData(
        this IWorldAccessor world,
        Vec3d pos,
        double sideTolerance = 0.5,
        double downTolerance = 0.15,
        double upTolerance = 0.4
    ) {
        var bp = pos.AsBlockPos;
        var track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);

        if (track == null) {
            var offset = new BlockPos(0, 1, 0);
            if ((pos.Y % 1.0) < 0.5) {
                offset *= -1;
            }
            bp.Add(offset);
            track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);
        }
        
        if (track == null) return (null, bp);
        
        var anchors = track.AnchorData;

        var localPos = pos.RelativeToCenter(bp);
        
        var (sideDelta, upDelta) = CalculateDistances(anchors.LowerAnchor.offset, anchors.HigherAnchor.offset, localPos);
        
        if (Math.Abs(sideDelta) < sideTolerance && ((upDelta >= 0 && upDelta < downTolerance) || (upDelta < 0 && upDelta > -upTolerance)))
            return (track, bp);

        return (null, bp);
    }

    private static (double sideDistance, double upDistance) CalculateDistances(Vec3d fromPos, Vec3d toPos, Vec3d targetPos)
    {
        var posDelta = toPos - fromPos;
        var dir = posDelta.Normalize();
        var relativePos = targetPos - fromPos;

        var sideDir = dir.Cross(new(0, 1, 0));
        var upDir = sideDir.Cross(dir);
        
        return (relativePos.Dot(sideDir), relativePos.Dot(upDir));
    }
    
    public static T? GetBlockBehaviour<T>(this IWorldAccessor world, BlockPos pos) where T : BlockBehavior =>
        world.BlockAccessor.GetBlock(pos)?.GetBehavior<T>();

    public static (BlockBehaviorCartTrack? track, BlockPos pos, int entryAnchor) GetNextTrack(IWorldAccessor world, BlockPos bp, TrackAnchorData anchors, int entryAnchor)
    {
        var anchor = anchors[1 - entryAnchor];

        var nextTrackPos = bp.AddCopy(anchor.blockOffset);
        
        var nextTrack = world.GetTrackAtPos(nextTrackPos);

        if (nextTrack == null) // look down
        {
            nextTrackPos = nextTrackPos.AddCopy(BlockFacing.DOWN);
            nextTrack = world.GetTrackAtPos(nextTrackPos);
            if (nextTrack != null)
            {
                var d = nextTrack.AnchorData;
                var lp = nextTrackPos.AddCopy(d.LowerAnchor.blockOffset).AsVec3i;
                var hp = nextTrackPos.AddCopy(d.HigherAnchor.blockOffset).AsVec3i;

                if (!nextTrack.Raised) nextTrack = null;
                
                if (lp != bp.AsVec3i && hp != bp.AsVec3i) nextTrack = null;
            }
        }
        
        var nextAnchors = nextTrack?.AnchorData;

        return (nextTrack, nextTrackPos, nextAnchors?.ClosestAnchor(anchor.offset.AddCopy(bp - nextTrackPos)) ?? 1);
    }

    public static BlockBehaviorCartTrack? GetTrackAtPos(this IWorldAccessor world, BlockPos pos) =>
        world.GetBlockBehaviour<BlockBehaviorCartTrack>(pos);
}