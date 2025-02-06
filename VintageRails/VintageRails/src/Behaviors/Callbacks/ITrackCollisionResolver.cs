using VintageRails.Utils;

namespace VintageRails.Behaviors.Callbacks;

public interface ITrackCollisionResolver {
    
    Ranged HandledCollisionSpeedRange { get; }
    
    const string minVelocityKey = "minVelocity";
    const string maxVelocityKey = "maxVelocity";
    
}