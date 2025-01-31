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
                // bp.Add(offset);
                // track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);
            }
            // if((pos.Y % 1.0) >= 0.5) {
            //     bp.Add(offset);
            //     track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);
            // }
            
            bp.Add(offset);
            track = world.GetBlockBehaviour<BlockBehaviorCartTrack>(bp);
        }

        TrackAnchorData? anchors = null;
        
        if (track != null) {
            anchors = track.GetAnchorData();

            var delta = anchors.AnchorDelta;
            var localPos = pos.RelativeToCenter(bp);//.SubCopy(bp.X, bp.Y, bp.Z);
            var a = anchors.LowerAnchor - localPos;
            var cross = delta.Cross(a);

            var distance = cross.Length() / anchors.DeltaL;
            if (distance > distanceTolerance * track.SnapToleranceMult) {
                anchors = null;
                track = null;
            }
        }
        
        return (track, anchors, bp);
    }

    public static T? GetBlockBehaviour<T>(this IWorldAccessor world, BlockPos pos) where T : BlockBehavior{
        var block = world.BlockAccessor.GetBlock(pos);
        
        if (block == null) {
            return null;
        } 
        return block.GetBehavior<T>();
    }
    
}