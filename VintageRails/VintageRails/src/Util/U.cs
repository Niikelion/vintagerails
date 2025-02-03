using Vintagestory.API.MathTools;

namespace VintageRails.Rails;

public static class U {

    //From PhysicsManager
    public const float PhysicsTickInterval = 0.033333335f * 1f;

    public static Vec3d RelativeToCenter(this Vec3d v, BlockPos bp) {
        return v.SubCopy(bp.X + 0.5, bp.Y + 0.5, bp.Z + 0.5);
    }
    
    public static Vec3d AddToCenter(this Vec3d v, BlockPos bp) {
        return v.AddCopy(bp.X + 0.5, bp.Y + 0.5, bp.Z + 0.5);
    }

    public static double VelocityAfterCollision(double vSelf, double vOther, double restitution = 0, double mSelf = 1, double mOther = 1) {
        var a = restitution * mOther * (vOther - vSelf);
        var b = mSelf * vSelf + mOther * vOther;
        var c = mSelf + mOther;
        return (a + b) / c;
    }
}