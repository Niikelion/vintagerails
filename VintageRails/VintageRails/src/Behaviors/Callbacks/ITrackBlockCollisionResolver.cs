using VintageRails.Util;

namespace VintageRails.Behaviors.Callbacks;

public interface ITrackBlockCollisionResolver : ITrackCollisionResolver {

    public void ResolveCollision(bool args, ref CollisionResult result) { }
    
}