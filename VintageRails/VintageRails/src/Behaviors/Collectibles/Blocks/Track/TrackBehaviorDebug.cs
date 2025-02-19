using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks.Track;

public class TrackBehaviorDebug : ITrackBehavior {

    public static ITrackBehavior Create(JsonObject properties) {
        return new TrackBehaviorDebug();
    }

    public void OnCartEntered(EntityBehaviorTrackRider rider, BlockPos currentTrackPos, BlockPos previousTrackPos) {
        rider.entity.World.Logger.Debug($"Track Entered: {rider.entity.EntityId}");
    }

    public void OnCartExited(EntityBehaviorTrackRider rider, BlockPos currentTrackPos, BlockPos previousTrackPos) {
        rider.entity.World.Logger.Debug($"Track Exited: {rider.entity.EntityId}");
    }

    public void OnTickCart(EntityBehaviorTrackRider rider, BlockPos trackPos, double dt) {
        rider.entity.World.Logger.Debug($"Tick On Track: {rider.entity.EntityId}");
    }
}