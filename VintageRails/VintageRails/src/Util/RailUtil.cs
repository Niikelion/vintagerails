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

    public static (BlockPos pos, TrackAnchorData? anchors, int entryAnchor) GetNextTrack(IWorldAccessor world, BlockPos bp, TrackAnchorData anchors, int entryAnchor) {
        var anchor = anchors[1 - entryAnchor];
        
        var pos = (anchor * 1.1).AddToCenter(bp);
        var (_, nextAnchors, foundAt) = world.GetTrackData(pos, SnapToleranceBase);

        return (foundAt, nextAnchors, nextAnchors?.ClosestAnchor(anchor.AddCopy(bp - foundAt)) ?? 1);
    }

    public static int AnchorToDirection(int idx) {
        return -(idx * 2 - 1);
    }

    public static int DirectionToAnchor(int idx) {
        return (-idx + 1) / 2;
    }
    
    public static double GetDistanceOnTrack(TrackRiderEntityBehavior from, TrackRiderEntityBehavior to, CartDirection searchDirection, double maxDistance = 1.0) {
        var fromEntity = from.entity;
        var toEntity = to.entity;
        var fp = fromEntity.Pos.XYZ;
        var tp = toEntity.Pos.XYZ;

        if (fp.SquareDistanceTo(tp) > maxDistance * maxDistance) {
            Console.Out.WriteLine("ToFar");
            return -1;
        }

        if (!from.WasOnTrack && ! to.WasOnTrack) {
            return -1;
        }
        
        var fromBp = from.PreviousBp!;
        var toBp = to.PreviousBp!;

        double distance;
        if (fromBp == toBp) {
            distance = Math.Abs(from.PosOnTrack - to.PosOnTrack);
        }
        else {
            var fromAnchor = from.GetFacingAnchorIndex(searchDirection);
            var fromAnchors = from.LastAnchorData!;

            if (fromAnchor == 0) {
                distance = from.PosOnTrack;
            }
            else {
                distance = fromAnchors.DeltaL - from.PosOnTrack;
            }

            BlockPos pos = fromBp;
            int i = fromAnchor;
            var anchors = fromAnchors;
            do {
                (pos, anchors, i) = GetNextTrack(from.entity.World, pos, anchors, i);

                if (anchors == null) {
                    break;
                }
                
                if (pos != toBp) {
                    distance += anchors.DeltaL;
                }
                else {
                    if (i == 0) {
                        //Invert distance
                        distance += anchors.DeltaL - to.PosOnTrack;
                    }
                    else {
                        distance += to.PosOnTrack;
                    }
                    break;
                }
            } while (distance < maxDistance);
        }
        Console.Out.WriteLine($"Distance: {distance}");
        if (distance < maxDistance) {
            return distance;
        }
        else {
            return -1;
        }
    }
    
    /// <summary>
    /// Calculates the direction between two <see cref="TrackRiderEntityBehavior"/>s (<see cref="CartDirection.Forward"/> or <see cref="CartDirection.Backward"/>), or returns <see cref="CartDirection.Any"/> if they are separated by more than <paramref name="maxDistance"/>
    /// </summary>
    /// <param name="maxDistance"></param>
    /// <returns>The direction in which <paramref name="to"/> was found, relative to <paramref name="from"/></returns>
    public static CartDirection AreCloseOnTrack(TrackRiderEntityBehavior from, TrackRiderEntityBehavior to, double maxDistance = 1.0) {
        //TODO Implement
        return CartDirection.Any;
    }
}

public enum CartDirection {
    Backward = -1,
    Any = 0,
    Forward = 1
}