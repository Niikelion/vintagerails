using System;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks.Track;

public class TrackBehaviorDynamicFriction : ITrackBehavior {

    private readonly double _frictionCoefficient;
    private readonly double _minSpeed;

    public static ITrackBehavior Create(JsonObject properties) {
        return new TrackBehaviorDynamicFriction(properties);
    }
    
    private TrackBehaviorDynamicFriction(JsonObject properties) {
        _frictionCoefficient = properties["frictionCoefficient"].AsDouble();
        _minSpeed = properties["minSpeed"].AsDouble();
    }

    public void OnTickCart(EntityBehaviorTrackRider rider, BlockPos trackPos, double dt) {
        var speed = rider.Speed;
        var speedAbs = Math.Abs(speed);
        var speedSign = Math.Sign(speed);

        var friction = speedAbs - _minSpeed;
        friction = Math.Max(friction, 0);
        friction *= _frictionCoefficient * dt;
        
        speedAbs -= friction;
        speedAbs = Math.Max(speedAbs, 0);
        rider.Speed = speedAbs * speedSign;
    }
}