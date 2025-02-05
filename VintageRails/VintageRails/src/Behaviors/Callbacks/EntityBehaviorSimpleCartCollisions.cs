using System;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Util;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace VintageRails.Behaviors.Callbacks;

public class EntityBehaviorSimpleCartCollisions : EntityBehavior, ITrackCartCollisionResolver {
    
    [NotNull] public Ranged? HandledCollisionSpeedRange { get; private set; }

    public EntityBehaviorSimpleCartCollisions(Entity entity) : base(entity) {
        
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);

        HandledCollisionSpeedRange = new Ranged(
            attributes[ITrackCollisionResolver.minVelocityKey].AsDouble(double.NegativeInfinity), 
            attributes[ITrackCollisionResolver.maxVelocityKey].AsDouble(double.PositiveInfinity)
            );
    }

    public override string PropertyName() {
        return "vrails.simpleCartCollision";
    }
    
    public void OnCollideWith(TrackRiderEntityBehavior self, TrackRiderEntityBehavior other, double selfSpeed, double otherSpeed,
        ref CollisionResult resultRef) {

        var otherEnt = other.entity;
        
        var otherBp = otherEnt.SidedPos.AsBlockPos;
        var thisBp = entity.SidedPos.AsBlockPos;
            
        double signedDistance;
        if (otherBp == thisBp) {
            signedDistance = self.PosOnTrack - other.PosOnTrack;
        }
        else {
            var an1 = self.LastAnchorData!;
            var an2 = other.LastAnchorData!;
            var a1 = an1.ClosestAnchor((otherBp - thisBp).ToVec3d());
            var a2 = an2.ClosestAnchor((thisBp - otherBp).ToVec3d());
            var distance = 0.0;
            if (a1 == 0) {
                distance += self.PosOnTrack;
            }
            else {
                distance += an1.DeltaL - self.PosOnTrack;
            }
                
            if (a2 == 0) {
                distance += other.PosOnTrack;
            }
            else {
                distance += an2.DeltaL - other.PosOnTrack;
            }

            distance *= a1 == 0 ? 1 : -1;
            signedDistance = distance;
        }
            
        const double maxDistance = 1;
        const double maxForce = 5;
        const double constantForce = 0;
        resultRef.AddDeltas(
            (constantForce + Math.Clamp((maxDistance - Math.Abs(signedDistance)) / maxDistance * maxForce, 0, maxForce)) * Math.Sign(signedDistance),
            0.0);
    }
}