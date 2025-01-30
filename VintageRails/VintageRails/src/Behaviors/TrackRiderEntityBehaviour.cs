using System;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors;

public class TrackRiderEntityBehaviour : EntityBehavior {

    public bool WasOnTrack { get; private set; }

    private double CurrentPosOnTrack { get; set; } = 0;//TODO save
    private float CurrentTrackSpeed { get; set; } = 0;//TODO save

    private TrackAnchorData? LastAnchors = null; 
    
    // private Vec2d CurrentTrackDirection { get; set; } = new Vec2d(1, 0);//TODO save

    private bool WasMovingForward;
    
    private BlockPos? PreviousBp { get; set; }


    public TrackRiderEntityBehaviour(Entity entity) : base(entity) {
    }
    
    public override string PropertyName() {
        return "vrails_track_rider";
    }

    public override void OnGameTick(float deltaTime) {
        if(entity.World.Side == EnumAppSide.Client) {
            return;
        }
        
        // this.entity.Alive = false;
        
        // base.OnGameTick(deltaTime);
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
        LastAnchors ??= anchors;
        
        var spdSign = Math.Sign(CurrentTrackSpeed);

        var dirOnTrack = (spdSign == 0 ? 1d : spdSign);
        
        if (bp != PreviousBp) {
            PreviousBp.Set(bp);
            var localPos = entityPos.Sub(new Vec3d(bp.X + 0.5f, bp.Y + 0.5f, bp.Z + 0.5f));
            var i1 = anchors.ClosestAnchor(localPos);
            var a = anchors[i1];
            var i2 = LastAnchors.GetFromMovement(CurrentTrackSpeed);
            var b = LastAnchors[i2];
            dirOnTrack = -a.X * b.X + -a.Z * b.Z;
            LastAnchors = anchors;
            // var reverse = i1 == 1 && false;
            CurrentPosOnTrack = i1 == 0 ? 0 : 1;
            CurrentTrackSpeed *= Math.Sign(dirOnTrack);
        }
        
        WasOnTrack = true;

        //Stop on 90deg turn
        if (Math.Abs(dirOnTrack) < 1d / 64d) {
            CurrentTrackSpeed = 0;
            return;
        }
        // CurrentTrackDirection = anchors.DirDeltaNorm;

        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;

        WasMovingForward = CurrentTrackSpeed > 0;


        var dt = 1f / 60f;
        
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
        CurrentTrackSpeed = 0;
        //Add more involved logic?
    }
}