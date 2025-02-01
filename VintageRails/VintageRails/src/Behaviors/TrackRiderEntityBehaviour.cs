using System;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class TrackRiderEntityBehaviour : EntityBehavior {

    public bool WasOnTrack { get; private set; } = false;

    private double PosOnTrack { get; set; } = 0;//TODO save
    private double SpeedOnTrack { get; set; } = 0;//TODO save

    private TrackAnchorData? _lastAnchors = null;

    private EntityBehaviorPassivePhysics? _physics = null;
    private EntityPartitioning partitionUtil;

    private BlockPos? PreviousBp { get; set; }
    
    public TrackRiderEntityBehaviour(Entity entity) : base(entity) {
        
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);
        
        partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
    }


    public override string PropertyName() {
        return "vrails_track_rider";
    }

    public override void OnEntitySpawn() {
        _physics = entity.GetBehavior<EntityBehaviorPassivePhysics>();
    }

    public override void OnEntityLoaded() {
        _physics = entity.GetBehavior<EntityBehaviorPassivePhysics>();
    }

    //TODO move to physics update
    public override void OnGameTick(float deltaTime) {
        if(entity.World.Side == EnumAppSide.Client) {
            return;
        }

        var dt = 1f / 30f;//deltaTime;
        
        // this.entity.Alive = false;

        var tolerance = RailUtil.SnapToleranceBase + (WasOnTrack ? Math.Abs(SpeedOnTrack) * dt : entity.SidedPos.Motion.Length());
        
        var entityPos = entity.SidedPos.XYZ;
        var (track, anchors, bp) = entity.World.GetTrackData(entityPos, tolerance);
        PreviousBp ??= bp;

        if (track == null || anchors == null /* Does nothing anchors are not null when track is not null */) {
            if (WasOnTrack) {
                Derail();
            }

            WasOnTrack = false;
            return;
        }
        // var anchors = track.GetAnchorData();
        _lastAnchors ??= anchors;

        var wasRerailed = false;
        if (!WasOnTrack) {
            Rerail(anchors, bp);
            wasRerailed = true;
        }
        
        WasOnTrack = true;
        var spdSign = Math.Sign(SpeedOnTrack);

        var dirOnTrack = (spdSign == 0 ? 1d : spdSign);
        
        if (bp != PreviousBp) {
            PreviousBp.Set(bp);
            if (!wasRerailed) {
                var localPos = entityPos.RelativeToCenter(bp);//.Sub(new Vec3d(bp.X + 0.5f, bp.Y + 0.5f, bp.Z + 0.5f));
                var i1 = anchors.ClosestAnchor(localPos);
                var a = anchors[i1];
                var i2 = _lastAnchors.GetFromMovement(SpeedOnTrack);
                var b = _lastAnchors[i2];
                dirOnTrack = -a.X * b.X + -a.Z * b.Z;
                // var reverse = i1 == 1 && false;
                PosOnTrack = i1 == 0 ? 0 : 1;
                SpeedOnTrack *= Math.Sign(SpeedOnTrack) * -(i1 * 2 - 1);
            }
            _lastAnchors = anchors;
        }

        //Stop on 90deg turn
        if (Math.Abs(dirOnTrack) < 1d / 64d) {
            SpeedOnTrack = 0;
            return;
        }
        
        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;
        
        SpeedOnTrack += track.ConstantAcceleration * dt - SpeedOnTrack * track.Friction * dt;

        ApplyCollisions();
        
        PosOnTrack += dt * SpeedOnTrack / anchors.DeltaL;

        
        var s = Math.Sign(SpeedOnTrack);
        var adn2 = /*TrackAnchorData.OfDirections(BlockFacing.WEST, BlockFacing.NORTH, false).AnchorDeltaNorm;*/anchors.AnchorDeltaNorm.Clone();//.Mul(s, 1, s);

        var y = -(float)(Math.Atan2(adn2.Z * s, adn2.X * s));// - Math.PI / 2.0);
        var p = (float)(Math.Acos(adn2.Dot(new Vec3d(0, 1, 0))) - Math.PI / 2.0) * s;
        var r = 0f;//-(float)Math.Asin(adn2.Y);// - Math.PI / 2.0);
        
        // var quat = Quaterniond.Create();
        // var quat2 = Quaterniond.Create();
        
        // Quaterniond.RotateZ(quat2, quat2, p);
        // Quaterniond.RotateY(quat, quat, y);
        // var random = Math.PI;//Random.Shared.NextSingle() * Math.PI * 2;
        // Quaterniond.RotateY(quat, quat, random);
        
        // EntityBehaviorInterpolatePosition
        // var euler = Quaterniond.ToEulerAngles(quat);
        
        entity.ServerPos
            .SetAngles(r, y, p)
            .SetPos(
                new Vec3d(
                    GameMath.Lerp(la.X + 0.5, ha.X + 0.5, PosOnTrack),
                    GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, PosOnTrack),
                    GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, PosOnTrack)
                ).Add(bp)
            );
        
        entity.Pos.SetFrom(entity.ServerPos);
    }

    private void ApplyCollisions() {
        var pos = entity.Pos.XYZ;
        var radius = Math.Max(
            Math.Max(
                entity.SelectionBox.Height,
                entity.SelectionBox.Width
                ),
            entity.SelectionBox.Length) / 2;
        
        partitionUtil.WalkEntityPartitions(pos, radius + partitionUtil.LargestTouchDistance + 0.1, HandleEntityCollision);
    }

    private bool HandleEntityCollision(Entity e) {
        if (_lastAnchors == null) {
            //Stop iteration
            return false;
        }
        
        var box1 = this.entity.SelectionBox;
        var box2 = e.SelectionBox;

        var dv = (box1.Center - box2.Center) + (entity.SidedPos.XYZ - e.SidedPos.XYZ);

        var dx = Math.Abs(dv.X);
        var dy = Math.Abs(dv.Y);
        var dz = Math.Abs(dv.Z);
        
        var maxDx = box1.Width + box2.Width;
        var maxDy = box1.Height + box2.Height;
        var maxDz = box1.Length + box2.Length;
        
        if (dx < maxDx &&
            dy < maxDy &&
            dz < maxDz) {

            var maxDist = Math.Min(Math.Min(maxDx, maxDy), maxDz);
            
            // var relativePos = entity.Pos.XYZ.RelativeToCenter(entity.SidedPos.AsBlockPos);
            var l = dv.Length();

            var dot = _lastAnchors.AnchorDeltaNorm.Dot(dv) / l;
            var l2 = Math.Clamp(l, 0, maxDist) / maxDist;
            var l3 = 1 - l2;

            var pushFactor = 5.0;
            
            SpeedOnTrack += l3 * dot * pushFactor;
        }
        
        return true;
    }

    private void Derail() {
        if (_physics != null) {
             _physics.Ticking = true;
            if (_lastAnchors != null) {
                var i = _lastAnchors.GetFromMovement(SpeedOnTrack);
                var speed = Math.Abs(SpeedOnTrack);
                entity.SidedPos.Motion.Set(_lastAnchors[i] - _lastAnchors[1 - i]).Normalize().Mul(speed * U.PhysicsTickInterval);
            }
        }
        SpeedOnTrack = 0;
        // Add more involved logic?
    }
    
    private void Rerail(TrackAnchorData anchors, BlockPos railPos) {
        if (_physics != null) {
            _physics.Ticking = false;
            var motion = entity.SidedPos.Motion;
            var anchorDeltaNorm = anchors.AnchorDeltaNorm;
            var motionDot = anchorDeltaNorm.Dot(motion);
            SpeedOnTrack = motionDot / U.PhysicsTickInterval;
            
            var localPos = anchors.LowerAnchor.SubCopy(entity.Pos.XYZ.RelativeToCenter(railPos));
            var positionDot = -localPos.Dot(anchorDeltaNorm) / anchors.DeltaL;

            PosOnTrack = positionDot;
        }
    }
}