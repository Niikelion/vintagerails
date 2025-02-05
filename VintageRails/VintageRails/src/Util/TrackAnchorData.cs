using System;
using Vintagestory.API.MathTools;

namespace VintageRails.Rails;

public class TrackAnchorData {

    public const int AnchorResolutionOffset = 1;
    public const int AnchorResolution = AnchorResolutionOffset * 2 + 1;
    public const int BoundY = 1;
    public const int BoundZ = AnchorResolution;
    public const int BoundX = BoundZ * BoundZ;
    
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

    public static int GetEntryFromMovement(double movement) => movement > 0 ? 0 : 1;

    public static TrackAnchorData OfDirections(BlockFacing first, BlockFacing second, bool raised)
    {
        var upOffset = BlockFacing.UP.Normali;
        var downOffset = BlockFacing.DOWN.Normali;

        var up = upOffset.AsVec3d() / 2;
        
        var f = first.Normali;
        var s = second.Normali;
        
        bool shouldSwap = SortOffsets(f, s);

        var firstAnchor = (f.AsVec3d() / 2 - up, f);
        var secondAnchor = (s.AsVec3d() / 2 + up * (raised ? 1 : -1), raised ? upOffset + s : s);
        
        var (lowerAnchor, higherAnchor) = Swap(shouldSwap, firstAnchor, secondAnchor);

        var anchorDelta = higherAnchor.Item1 - lowerAnchor.Item1;
        double anchorDeltaLength = anchorDelta.Length();
        
        return new()
        {
            LowerAnchor = lowerAnchor,
            HigherAnchor = higherAnchor,
            AnchorDelta = anchorDelta,
            DeltaL = anchorDeltaLength,
            AnchorDeltaNorm = anchorDelta / anchorDeltaLength 
        };
    }

    private static (T, T) Swap<T>(bool swap, T a, T b) => swap ? (b, a) : (a, b);
    
    private static bool SortOffsets(Vec3i a1, Vec3i a2) => Encode(a1) > Encode(a2);

    private static int Encode(Vec3i anchor) =>
        (anchor.X + AnchorResolutionOffset) * BoundX +
        (anchor.Y + AnchorResolutionOffset) * BoundY +
        (anchor.Z + AnchorResolutionOffset) * BoundZ;
}

public static class StupidMathExtensions
{
    public static Vec3d AsVec3d(this Vec3i vec) => new (vec.X, vec.Y, vec.Z);
}