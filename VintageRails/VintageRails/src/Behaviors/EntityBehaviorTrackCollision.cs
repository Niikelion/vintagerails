using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Rails;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class EntityBehaviorTrackCollision : EntityBehavior, IOrderedPhysicsTickBehavior {

    public IEnumerable<Type> Before { get; } = new[] { typeof(TrackRiderEntityBehavior) };
    
    private EntityPartitioning _partitionUtil;
    
    [NotNull] private TrackRiderEntityBehavior? TrackRider { get; set; }

    private double _restitution = 0.0;
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
        var hitCout = 0;
        _partitionUtil.WalkEntities(pos, radius + _partitionUtil.LargestTouchDistance + 0.1, e => {
            if (HandleEntityCollision(e, speed, out var delta)) {
                deltaAcc += delta;
                hitCout++;
            }
            return true;
        }, EnumEntitySearchType.Inanimate);

        if (hitCout > 0) {
            TrackRider.Speed = speed + deltaAcc / hitCout;   
        }
    }

    private bool HandleEntityCollision(Entity other, double previousSpeed, out double speedDelta) {
        var otherRider = other.GetBehavior<TrackRiderEntityBehavior>();
        var otherCollisions = other.GetBehavior<EntityBehaviorTrackCollision>();

        if (otherRider == null || otherCollisions == null) {
            speedDelta = 0;
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
            
            var thisDir = TrackRider.LastAnchorData!.AnchorDeltaNorm * thisSpdSign;
            var otherDir = otherRider.LastAnchorData!.AnchorDeltaNorm * otherSpdSign;

            var dot = thisDir.Dot(otherDir);
            var dotSign = Math.Sign(dot);

            otherSpeed *= dotSign;

            var targetSpeed = U.VelocityAfterCollision(previousSpeed, otherSpeed, 
                (_restitution + otherCollisions._restitution) / 2,
                Mass, Mass);

            speedDelta = targetSpeed - previousSpeed;
            return true;
        }
        speedDelta = 0;
        return false;
    }
}