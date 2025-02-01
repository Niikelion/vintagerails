using System;
using VintageRails.Behaviors;
using Vintagestory.API.MathTools;
using static System.Math;

namespace VintageRails.Rails;

public class TrackAnchorData {

    public const int AnchorResolutionOffset = 1;
    public const int AnchorResolution = AnchorResolutionOffset * 2 + 1;
    public const int BoundY = 1;
    public const int BoundZ = AnchorResolution;
    public const int BoundX = BoundZ * BoundZ;
    
    public Vec3d this[int i] {
        get => i switch {
            0 => LowerAnchor,
            1 => HigherAnchor,
            _ => throw new IndexOutOfRangeException()
        };
    }
    
    public required Vec3d LowerAnchor { get; init; }
    public required Vec3d HigherAnchor { get; init; }
    public required Vec3d AnchorDelta { get; init; }
    
    public required Vec3d AnchorDeltaNorm { get; init; }
    
    public required double DeltaL { get; init; }
    
    private TrackAnchorData() { }

    public int ClosestAnchor(Vec3d localPoint) {
        var p = localPoint;
        // var mask = new Vec3d(-1, 1, -1);
        var l = LowerAnchor;//.Clone().Mul(-1, 1, -1);
        var h = HigherAnchor;//.Clone().Mul(-1, 1, -1);
        var d1 = l.SquareDistanceTo(p);
        var d2 = h.SquareDistanceTo(p);
        return d1 < d2 ? 0 : 1;
    }

    public int GetEntryFromMovement(double movement) {
        return movement > 0 ? 0 : 1;
    }
    
    public static TrackAnchorData OfDirections(BlockFacing first, BlockFacing second, bool raised) {
        var offset = new Vec3i(0, -AnchorResolutionOffset ,0);
        return OfAnchors(first.Normali + offset, second.Normali + offset * (raised ? -1 : 1));
    }

    public static TrackAnchorData OfAnchors(Vec3i a1, Vec3i a2) {
        var sorted = SortAnchors(a1, a2);
        var hai = sorted.highter;
        var lai = sorted.lower;
        var ha = NormalizeMax(new Vec3d(hai.X, hai.Y, hai.Z)) / 2;
        var la = NormalizeMax(new Vec3d(lai.X, lai.Y, lai.Z)) / 2;
        var d = ha - la;
        var l = d.Length();
        return new TrackAnchorData {
            LowerAnchor = la,
            HigherAnchor = ha,
            AnchorDelta = d,
            DeltaL = l,
            AnchorDeltaNorm = d / l
        };
    }

    private static (Vec3i lower, Vec3i highter) SortAnchors(Vec3i a1, Vec3i a2) {
        return Encode(a1) > Encode(a2) ? (a1, a2) : (a2, a1);
    }

    private static int Encode(Vec3i anchor) {
        return (anchor.X + AnchorResolutionOffset) * BoundX +
               (anchor.Y + AnchorResolutionOffset) * BoundY +
               (anchor.Z + AnchorResolutionOffset) * BoundZ;
    }

    private static Vec3d NormalizeMax(Vec3d vec) {
        var max = Max(Max(Abs(vec.X), Abs(vec.Y)), Abs(vec.Z));
        return vec / max;
    }
    
}