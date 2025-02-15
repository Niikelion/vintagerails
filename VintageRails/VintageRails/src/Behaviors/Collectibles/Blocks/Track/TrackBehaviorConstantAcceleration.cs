using System;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks.Track;

public class TrackBehaviorConstantAcceleration : ITrackBehavior {
    private readonly double _acceleration;
    
    public static ITrackBehavior Create(JsonObject properties) {
        return new TrackBehaviorConstantAcceleration(properties);
    }
    
    private TrackBehaviorConstantAcceleration(JsonObject properties) {
        _acceleration = properties["acceleration"].AsDouble();
    }

    public void OnTickCart(EntityBehaviorTrackRider rider, BlockPos trackPos, double dt) {
        rider.Speed += _acceleration * dt;
    }
}