using System;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks.Track;

public class TrackBehaviorConstantFriction : ITrackBehavior {

    private readonly double _friction;
    
    public static ITrackBehavior Create(JsonObject properties) {
        return new TrackBehaviorConstantFriction(properties);
    }
    
    private TrackBehaviorConstantFriction(JsonObject properties) {
        _friction = properties["friction"].AsDouble();
    }

    public void OnTickCart(EntityBehaviorTrackRider rider, BlockPos trackPos, double dt) {
        var speed = rider.Speed;
        var speedAbs = Math.Abs(speed);
        var speedSign = Math.Sign(speed);

        speedAbs -= _friction;
        speedAbs = Math.Max(speedAbs, 0);
        rider.Speed = speedAbs * speedSign;
    }
}