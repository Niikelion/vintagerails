using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace VintageRails.Utils;

public class TrackAnchorData {

    private const int AnchorResolutionOffset = 1;
    private const int AnchorResolution = AnchorResolutionOffset * 2 + 1;
    private const int BoundY = 1;
    private const int BoundZ = AnchorResolution;
    private const int BoundX = BoundZ * BoundZ;

    private static readonly Dictionary<Vec3i, TrackAnchorData> ForwardAnchors = new();

    static TrackAnchorData() {
        var anchors = OfDirections(BlockFacing.EAST, BlockFacing.WEST, false);
        ForwardAnchors.Add(anchors.LowerAnchor.blockOffset, anchors);
        ForwardAnchors.Add(anchors.HigherAnchor.blockOffset, anchors);
        
        anchors = OfDirections(BlockFacing.NORTH, BlockFacing.SOUTH, false);
        ForwardAnchors.Add(anchors.LowerAnchor.blockOffset, anchors);
        ForwardAnchors.Add(anchors.HigherAnchor.blockOffset, anchors);
        
        // anchors = OfDirections(BlockFacing.EAST, BlockFacing.WEST, true);
        // ForwardAnchors.Add(anchors.LowerAnchor.blockOffset, anchors);
        // ForwardAnchors.Add(anchors.HigherAnchor.blockOffset, anchors);
        //
        // anchors = OfDirections(BlockFacing.NORTH, BlockFacing.SOUTH, true);
        // ForwardAnchors.Add(anchors.LowerAnchor.blockOffset, anchors);
        // ForwardAnchors.Add(anchors.HigherAnchor.blockOffset, anchors);
        
        //TODO add diagonal when implemented
    }
    
    public (Vec3d offset, Vec3i blockOffset) this[int i] => i switch
        {
            0 => LowerAnchor,
            1 => HigherAnchor,
            _ => throw new IndexOutOfRangeException()
        };

    public required (Vec3d offset, Vec3i blockOffset) LowerAnchor { get; init; }
    public required (Vec3d offset, Vec3i blockOffset) HigherAnchor { get; init; }
    public required Vec3d AnchorDelta { get; init; }
    
    public required Vec3d AnchorDeltaNorm { get; init; }
    
    public required double DeltaL { get; init; }
    
    private TrackAnchorData() { }

    public int ClosestAnchor(Vec3d localPoint)
    {
        var l = LowerAnchor.offset;
        var h = HigherAnchor.offset;
        return l.SquareDistanceTo(localPoint) < h.SquareDistanceTo(localPoint) ? 0 : 1;
    }

    //TODO maybe serialize to a single int
    public ITreeAttribute Encode() {
        var tree = new TreeAttribute();
        tree.SetVec3i("lower", LowerAnchor.blockOffset);
        tree.SetVec3i("higher", HigherAnchor.blockOffset);
        
        tree.SetVec3d("lowerA", LowerAnchor.offset);
        tree.SetVec3d("higherA", HigherAnchor.offset);
        return tree;
    }
    
    public static TrackAnchorData? Decode(ITreeAttribute? attributes) {
        if (attributes == null) {
            return null;
        }
        
        var lower = attributes.GetVec3i("lower");
        var higher = attributes.GetVec3i("higher");
        
        var lowerA = attributes.GetVec3d("lowerA");
        var higherA = attributes.GetVec3d("higherA");

        return Create(
            (lowerA, lower),
            (higherA, higher)
        );
    }
    
    public static int GetEntryFromMovement(double movement) => movement > 0 ? 0 : 1;

    public static TrackAnchorData OfDirections(BlockFacing first, BlockFacing second, bool raised) {
        var upOffset = BlockFacing.UP.Normali;
        var downOffset = BlockFacing.DOWN.Normali;

        var up = upOffset.AsVec3d() / 2;
        
        var f = first.Normali;
        var s = second.Normali;
        
        var firstAnchor = (f.AsVec3d() / 2 - up, f);
        var secondAnchor = (s.AsVec3d() / 2 + up * (raised ? 1 : -1), raised ? upOffset + s : s);

        return Create(firstAnchor, secondAnchor);
    }
    
    public static TrackAnchorData Create((Vec3d offset, Vec3i blockOffset) firstAnchor, (Vec3d offset, Vec3i blockOffset) secondAnchor) {
        bool shouldSwap = SortOffsets(firstAnchor.Item2, secondAnchor.Item2);
        
        var (lowerAnchor, higherAnchor) = Swap(shouldSwap, firstAnchor, secondAnchor);

        var anchorDelta = higherAnchor.Item1 - lowerAnchor.Item1;
        double anchorDeltaLength = anchorDelta.Length();
        
        return new() {
            LowerAnchor = lowerAnchor,
            HigherAnchor = higherAnchor,
            AnchorDelta = anchorDelta,
            DeltaL = anchorDeltaLength,
            AnchorDeltaNorm = anchorDelta / anchorDeltaLength
        };
    }

    public static TrackAnchorData GetForward(Vec3i dir1) {
        return ForwardAnchors[dir1];
    }
    
    private static (T, T) Swap<T>(bool swap, T a, T b) => swap ? (b, a) : (a, b);
    
    private static bool SortOffsets(Vec3i a1, Vec3i a2) => Encode(a1) <= Encode(a2);

    private static int Encode(Vec3i anchor) =>
        (anchor.X + AnchorResolutionOffset) * BoundX +
        (anchor.Y + AnchorResolutionOffset) * BoundY +
        (anchor.Z + AnchorResolutionOffset) * BoundZ;
}

public static class StupidMathExtensions
{
    public static Vec3d AsVec3d(this Vec3i vec) => new (vec.X, vec.Y, vec.Z);
}