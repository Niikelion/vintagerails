using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace VintageRails.Behaviors;

public class EntityBehaviorTrackCollision : EntityBehavior, IOrderedPhysicsTickBehavior {

    public IEnumerable<Type> Before { get; } = new[] { typeof(TrackRiderEntityBehavior) };
    
    private EntityPartitioning _partitionUtil;
    
    [NotNull] private TrackRiderEntityBehavior? TrackRider { get; set; }

    private double _restitution = 0.5;
    private const double Mass = 1.0;
    
    public EntityBehaviorTrackCollision(Entity entity) : base(entity) {
        
    }
    
    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        _partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        TrackRider = entity.GetBehavior<TrackRiderEntityBehavior>();
    }

    public override string PropertyName() {
        return "vrails_track_collisions";
    }

    public void OnTick(double dt) {
        if(!TrackRider.WasOnTrack)
            return;
        
        var pos = entity.Pos.XYZ;
        var radius = Math.Max(
            Math.Max(
                entity.SelectionBox.Height,
                entity.SelectionBox.Width
            ),
            entity.SelectionBox.Length) / 2;

        var speed = TrackRider.PreviousSpeed;
        var deltaAcc = 0.0;
        var separationAcc = 0.0;
        var hitCout = 0;
        _partitionUtil.WalkEntities(pos, radius + _partitionUtil.LargestTouchDistance + 0.1, e => {
            if (HandleEntityCollision(e, speed, out var delta, out var separation)) {
                deltaAcc += delta;
                separationAcc += separation;
                hitCout++;
            }
            return true;
        }, EnumEntitySearchType.Inanimate);

        double prevPosOnTrack = TrackRider.PosOnTrack;
        
        if (hitCout > 0) {
            TrackRider.Speed = speed + deltaAcc / hitCout;
            TrackRider.PosOnTrack += separationAcc / hitCout;
        }
        
        // bool willCollide = entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, entity.Pos.AheadCopy(-TrackRider.PreviousSpeed * dt * TrackRider.Facing).XYZ, false);
        // if (!willCollide) return;
        //
        // TrackRider.Speed = 0;
        // TrackRider.PosOnTrack = prevPosOnTrack;
    }

    private bool HandleEntityCollision(Entity other, double previousSpeed, out double speedDelta, out double separationDelta) {
        speedDelta = 0;
        separationDelta = 0;
        if (other == entity) {
            return false;
        }
        var otherRider = other.GetBehavior<TrackRiderEntityBehavior>();
        var otherCollisions = other.GetBehavior<EntityBehaviorTrackCollision>();

        if (otherRider == null || otherCollisions == null || !otherRider.WasOnTrack) {
            return false;
        }
        
        var box1 = entity.SelectionBox;
        var box2 = other.SelectionBox;

        var dv = (box1.Center - box2.Center) + (entity.SidedPos.XYZ - other.SidedPos.XYZ);

        var dx = Math.Abs(dv.X);
        var dy = Math.Abs(dv.Y);
        var dz = Math.Abs(dv.Z);
        
        var maxDx = box1.Width + box2.Width;
        var maxDy = box1.Height + box2.Height;
        var maxDz = box1.Length + box2.Length;

        if (dx < maxDx &&
            dy < maxDy &&
            dz < maxDz) {

            var otherSpeed = otherRider.PreviousSpeed;
            
            var thisSpdSign = Math.Sign(previousSpeed);
            var otherSpdSign = Math.Sign(otherSpeed);

            var thisDir = TrackRider.LastAnchorData!.AnchorDeltaNorm;// * thisSpdSign;
            var otherDir = otherRider.LastAnchorData!.AnchorDeltaNorm;// * otherSpdSign;
            
            var dot = thisDir.Dot(otherDir);
            var dotSign = Math.Sign(dot);

            otherSpeed *= dotSign;

            // var targetSpeed = U.VelocityAfterCollision(previousSpeed, otherSpeed, 
            //      (_restitution + otherCollisions._restitution) / 2,
            //     Mass, Mass);
            //
            // speedDelta = targetSpeed - previousSpeed;
            
            var otherBp = other.SidedPos.AsBlockPos;
            var thisBp = entity.SidedPos.AsBlockPos;
            
            double signedDistance;
            if (otherBp == thisBp) {
                signedDistance = TrackRider.PosOnTrack - otherRider.PosOnTrack;
            }
            else {
                // var (track, anchors, foundAt) = entity.World.GetTrackData(otherBp.ToVec3d(), RailUtil.SnapToleranceBase);
                // var 
                // var ca = anchors.ClosestAnchor(foundAt -);
                var an1 = TrackRider.LastAnchorData!;
                var an2 = otherRider.LastAnchorData!;
                var a1 = an1.ClosestAnchor((otherBp - thisBp).ToVec3d());
                var a2 = an2.ClosestAnchor((thisBp - otherBp).ToVec3d());
                var distance = 0.0;
                if (a1 == 0) {
                    distance += TrackRider.PosOnTrack;
                }
                else {
                    distance += an1.DeltaL - TrackRider.PosOnTrack;
                }
                
                if (a2 == 0) {
                    distance += otherRider.PosOnTrack;
                }
                else {
                    distance += an2.DeltaL - otherRider.PosOnTrack;
                }

                distance *= a1 == 0 ? 1 : -1;
                signedDistance = distance;
            }
            
            const double maxDistance = 1;
            const double maxForce = 5;
            const double constantForce = 0;
            speedDelta += (constantForce + Math.Clamp((maxDistance - Math.Abs(signedDistance)) / maxDistance * maxForce, 0, maxForce)) * Math.Sign(signedDistance);
            
            // var dv1 = new Vec2d(dv.X, dv.Z);
            // var otherDir2 = new Vec2d(otherDir.X, otherDir.Z);
            // var dot2 = dv1.Dot(otherDir2);
            // separationDelta = (1 - Math.Abs(dot2)) * Math.Sign(dot2);
            return true;
        }
        return false;
    }
}