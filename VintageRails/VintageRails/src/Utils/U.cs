using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Utils;

public static class U {

    public static readonly CollectibleBehaviorContainer ContainerHelper = new(null);
    
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
    
    //From BlockEntityFirepit
    public static float ChangeTemperature(float fromTemp, float toTemp, float dt) {
        float num = Math.Abs(fromTemp - toTemp);
        dt += dt * (num / 28f);
        if (num < dt || num < 1.0)
            return toTemp;
        if (fromTemp >  toTemp)
            dt = -dt;
        return fromTemp + dt;
    }

    public static T[] GetCollectibleInterfaces<T>(this CollectibleObject collectible) where T : class {
        return collectible.CollectibleBehaviors.Where(behavior => behavior is T).Cast<T>().ToArray();
    }
    
}