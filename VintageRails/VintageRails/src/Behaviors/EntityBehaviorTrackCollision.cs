using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Behaviors.Callbacks;
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

    private List<ITrackCartCollisionResolver> collisionResolvers;
    
    public EntityBehaviorTrackCollision(Entity entity) : base(entity) {
        
    }
    
    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        _partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        TrackRider = entity.GetBehavior<TrackRiderEntityBehavior>();
        collisionResolvers = entity.GetInterfaces<ITrackCartCollisionResolver>();
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
        var result = new CollisionResult();
        _partitionUtil.WalkEntities(pos, radius + _partitionUtil.LargestTouchDistance + 0.1, e => {
            HandleEntityCollision(e, speed, ref result);
            return true;
        }, EnumEntitySearchType.Inanimate);

        double prevPosOnTrack = TrackRider.PosOnTrack;
        
        if (result.HasAny) {
            TrackRider.Speed = speed + result.ScaledSpeedDelta;
            TrackRider.PosOnTrack += result.ScaledPosDelta;
        }
        
        // bool willCollide = entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, entity.CollisionBox, entity.Pos.AheadCopy(-TrackRider.PreviousSpeed * dt * TrackRider.Facing).XYZ, false);
        // if (!willCollide) return;
        //
        // TrackRider.Speed = 0;
        // TrackRider.PosOnTrack = prevPosOnTrack;
    }

    private void HandleEntityCollision(Entity other, double previousSpeed, ref CollisionResult result) {
        if (other == entity) {
            return;
        }
        var otherRider = other.GetBehavior<TrackRiderEntityBehavior>();
        var otherCollisions = other.GetBehavior<EntityBehaviorTrackCollision>();

        if (otherRider == null || otherCollisions == null || !otherRider.WasOnTrack) {
            return;
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
            
            var thisDir = TrackRider.LastAnchorData!.AnchorDeltaNorm;// * thisSpdSign;
            var otherDir = otherRider.LastAnchorData!.AnchorDeltaNorm;// * otherSpdSign;
            
            var dot = thisDir.Dot(otherDir);
            var dotSign = Math.Sign(dot);
            
            otherSpeed *= dotSign;

            var collisionSpeed = Math.Abs(previousSpeed - otherSpeed);

            foreach (var resolver in collisionResolvers) {
                if (resolver.HandledCollisionSpeedRange.IsInRange(collisionSpeed)) {
                    resolver.OnCollideWith(TrackRider, otherRider, previousSpeed, otherSpeed, ref result);
                }
            }
        }
    }
}

public struct CollisionResult {

    public bool HasAny => hitCount > 0;
    
    public double ScaledSpeedDelta => speedDelta / hitCount;
    public double ScaledPosDelta => posDelta / hitCount;
    
    private double speedDelta;
    private double posDelta;
    private int hitCount;

    public void AddDeltas(double speed, double pos) {
        hitCount++;
        speedDelta += speed;
        posDelta += pos;
    }
    
}