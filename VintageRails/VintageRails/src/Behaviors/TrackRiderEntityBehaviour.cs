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

    private double CurrentPosOnTrack { get; set; } = 0;//TODO save
    private double CurrentTrackSpeed { get; set; } = 0;//TODO save

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
        
        // this.entity.Alive = false;
        
        var entityPos = entity.SidedPos.XYZ;
        var (track, bp)= entity.World.GetTrackData(entityPos);
        PreviousBp ??= bp;

        if (track == null) {
            if (WasOnTrack) {
                Derail();
            }

            WasOnTrack = false;
            return;
        }
        var anchors = track.GetAnchorData();
        _lastAnchors ??= anchors;

        if (!WasOnTrack) {
            Rerail(anchors);
        }
        
        WasOnTrack = true;
        var spdSign = Math.Sign(CurrentTrackSpeed);

        var dirOnTrack = (spdSign == 0 ? 1d : spdSign);
        
        if (bp != PreviousBp) {
            PreviousBp.Set(bp);
            var localPos = entityPos.Sub(new Vec3d(bp.X + 0.5f, bp.Y + 0.5f, bp.Z + 0.5f));
            var i1 = anchors.ClosestAnchor(localPos);
            var a = anchors[i1];
            var i2 = _lastAnchors.GetFromMovement(CurrentTrackSpeed);
            var b = _lastAnchors[i2];
            dirOnTrack = -a.X * b.X + -a.Z * b.Z;
            _lastAnchors = anchors;
            // var reverse = i1 == 1 && false;
            CurrentPosOnTrack = i1 == 0 ? 0 : 1;
            CurrentTrackSpeed *= Math.Sign(CurrentTrackSpeed) * -(i1 * 2 - 1);
        }

        //Stop on 90deg turn
        if (Math.Abs(dirOnTrack) < 1d / 64d) {
            CurrentTrackSpeed = 0;
            return;
        }

        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;

        var dt = deltaTime;
        
        CurrentTrackSpeed += track.ConstantAcceleration * dt - CurrentTrackSpeed * track.Friction * dt;
        
        CurrentPosOnTrack += dt * CurrentTrackSpeed / anchors.DeltaL;
        
        entity.ServerPos
            .SetPos(
                new Vec3d(
                    GameMath.Lerp(la.X + 0.5, ha.X + 0.5, CurrentPosOnTrack),
                    GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, CurrentPosOnTrack),
                    GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, CurrentPosOnTrack)
                ).Add(bp)
            );
    }
    
    private void Derail() {
        if (_physics != null) {
             _physics.Ticking = true;
            if (_lastAnchors != null) {
                var i = _lastAnchors.GetFromMovement(CurrentTrackSpeed);
                entity.SidedPos.Motion.Set(_lastAnchors[i] -_lastAnchors[1 - i]).Normalize().Mul(CurrentTrackSpeed * U.PhysicsTickInterval);
            }
        }
        CurrentTrackSpeed = 0;
        //Add more involved logic?
    }
    
    private void Rerail(TrackAnchorData anchors) {
        if (_physics != null) {
            _physics.Ticking = false;
            var motion = entity.SidedPos.Motion;
            var dot = (anchors.LowerAnchor - anchors.HigherAnchor).Dot(motion);
            CurrentTrackSpeed = dot / U.PhysicsTickInterval;
            // entity.RemoveBehavior(_physics);
        }
    }
}