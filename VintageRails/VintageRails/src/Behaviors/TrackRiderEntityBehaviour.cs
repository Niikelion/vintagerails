using System;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Server;

namespace VintageRails.Behaviors;

public class TrackRiderEntityBehaviour : EntityBehavior {

    public bool WasOnTrack { get; private set; } = false;

    private double PosOnTrack { get; set; } = 0;//TODO save
    private double SpeedOnTrack { get; set; } = 0;//TODO save

    private TrackAnchorData? _lastAnchors = null;

    private EntityBehaviorPassivePhysics? _physics = null;
    
    private BlockPos? PreviousBp { get; set; }


    public TrackRiderEntityBehaviour(Entity entity) : base(entity) {
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

    //TODO move to physics
    public override void OnGameTick(float deltaTime) {
        if(entity.World.Side == EnumAppSide.Client) {
            return;
        }
        var dt = deltaTime;
        
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
        
        PosOnTrack += dt * SpeedOnTrack / anchors.DeltaL;
        
        entity.ServerPos
            .SetPos(
                new Vec3d(
                    GameMath.Lerp(la.X + 0.5, ha.X + 0.5, PosOnTrack),
                    GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, PosOnTrack),
                    GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, PosOnTrack)
                ).Add(bp)
            );
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