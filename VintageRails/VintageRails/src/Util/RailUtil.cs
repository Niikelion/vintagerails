using System;
using VintageRails.Behaviors;
using VintageRails.Rails;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Util;

public static class RailUtil {

    public const double SnapToleranceBase = 0.125;
    
    public static (BlockBehaviorCartTrack? track, TrackAnchorData? anchors, BlockPos foundAt) GetTrackData(this IWorldAccessor world, Vec3d pos, double distanceTolerance) {
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
        
        if (track == null) return (null, null, bp);
        
        var anchors = track.GetAnchorData();

        var localPos = pos.RelativeToCenter(bp);
        
        var (sideDelta, upDelta) = CalculateDistances(anchors.LowerAnchor, anchors.HigherAnchor, localPos);

        const double sideTolerance = 0.5;
        const double upTolerance = 0.15;
        const double downTolerance = 0.4;
        
        if (Math.Abs(sideDelta) < sideTolerance && upDelta is >= 0 and < upTolerance or < 0 and > -downTolerance)
            return (track, anchors, bp);

        return (null, null, bp);
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
    
    public static T? GetBlockBehaviour<T>(this IWorldAccessor world, BlockPos pos) where T : BlockBehavior{
        var block = world.BlockAccessor.GetBlock(pos);
        
        if (block == null) {
            return null;
        } 
        return block.GetBehavior<T>();
    }

    public static (BlockPos pos, TrackAnchorData? anchors, int entryAnchor) GetNextTrack(IWorldAccessor world, BlockPos bp, TrackAnchorData anchors, int entryAnchor) {
        var anchor = anchors[1 - entryAnchor];
        
        var pos = (anchor * 1.1).AddToCenter(bp);
        var (_, nextAnchors, foundAt) = world.GetTrackData(pos, SnapToleranceBase);

        return (foundAt, nextAnchors, nextAnchors?.ClosestAnchor(anchor.AddCopy(bp - foundAt)) ?? 1);
    }
    
}