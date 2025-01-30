using System;
using VintageRails.Behaviors;
using Vintagestory.API.MathTools;

namespace VintageRails.Rails;

public class TrackAnchorData {

    public const int AnchorResolutionOffset = 1;
    public const int AnchorResolution = AnchorResolutionOffset * 2 + 1;
    public const int BoundZ = 1;
    public const int BoundY = AnchorResolution;
    public const int BoundX = BoundY * BoundY;
    
    public Vec3d this[int i] {
        get => i switch {
            0 => LowerAnchor,
            1 => HigherAnchor,
            _ => throw new IndexOutOfRangeException()
        };
    }
    
    public required Vec3d LowerAnchor { get; init; }
    public required Vec3d HigherAnchor { get; init; }

    public required Vec3i Delta { get; init; }
    public required double DeltaL { get; init; }
    public required Vec2d DirDeltaNorm { get; init; }
    
    private TrackAnchorData() { }

    public int ClosestAnchor(Vec3d localPoint) {
        var d1 = LowerAnchor.SquareDistanceTo(localPoint);
        var d2 = HigherAnchor.SquareDistanceTo(localPoint);
        return d1 < d2 ? 0 : 1;
    }
    
    public static TrackAnchorData OfDirections(BlockFacing first, BlockFacing second, bool raised) {
        var offset = new Vec3i(0, -AnchorResolutionOffset ,0);
        return OfAnchors(first.Normali + offset, second.Normali + offset * (raised ? -1 : 1));
    }

    public static TrackAnchorData OfAnchors(Vec3i a1, Vec3i a2) {
        var sorted = SortAnchors(a1, a2);
        var hai = sorted.highter;
        var lai = sorted.lower;
        var d = hai - lai;
        var half = new Vec3d(0.5, 0.5, 0.5);
        var ha = NormalizeMax(new Vec3d(hai.X, hai.Y, hai.Z)) + half;
        var la = NormalizeMax(new Vec3d(lai.X, lai.Y, lai.Z)) + half;
        // var la = new Vec3d(lai.X + aro, lai.Y + aro, lai.Z + aro) / (aro * 2);
        var l = (ha - la).Length();
        return new TrackAnchorData {
            LowerAnchor = la,
            HigherAnchor = ha,
            Delta = d,
            DeltaL = l,
            DirDeltaNorm = new Vec2d(d.X, d.Z).Normalize()
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
        var max = Math.Max(Math.Max(vec.X, vec.Y), vec.Z);
        return vec / max;
    }
    
}