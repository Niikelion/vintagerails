using VintageRails.Behaviors.Entities;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Callbacks;

public interface ITrackBehavior {

    void OnCartEntered(EntityBehaviorTrackRider rider, BlockPos currentTrackPos, BlockPos previousTrackPos) { }

    void OnCartExited(EntityBehaviorTrackRider rider, BlockPos currentTrackPos, BlockPos previousTrackPos) { }

    void OnTickCart(EntityBehaviorTrackRider rider, BlockPos trackPos, double dt) { }
    
}