using System;
using VintageRails.Util;

namespace VintageRails.Behaviors.Callbacks;

public interface ITrackCartCollisionResolver : ITrackCollisionResolver {

    /// <param name="otherSpeed">Relative to <paramref name="self"/></param>
    public void OnCollideWith(TrackRiderEntityBehavior self, TrackRiderEntityBehavior other, double selfSpeed, double otherSpeed, ref CollisionResult resultRef);
}