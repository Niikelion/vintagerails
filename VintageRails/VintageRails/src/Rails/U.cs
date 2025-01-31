using Vintagestory.API.MathTools;

namespace VintageRails.Rails;

public static class U {

    //From PhysicsManager
    public const float PhysicsTickInterval = 0.033333335f * 1f;

    public static Vec3d RelativeToCenter(this Vec3d v, BlockPos bp) {
        return v.SubCopy(bp.X + 0.5, bp.Y + 0.5, bp.Z + 0.5);
    }
    
}