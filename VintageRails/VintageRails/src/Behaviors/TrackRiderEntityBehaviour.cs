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

    private Vec3d LastExitAnchor = new Vec3d(0, 0, 0); 
    
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
        
        var spdSign = Math.Sign(CurrentTrackSpeed);

        var dirOnTrack = (spdSign == 0 ? 1d : spdSign);
        
        if (bp != PreviousBp) {
            PreviousBp.Set(bp);
            var localPos = new Vec3d(bp.X, bp.Y, bp.Z).Sub(entityPos);
            var i = anchors.ClosestAnchor(localPos);
            var a = anchors[i];
            dirOnTrack = a.Dot(LastExitAnchor);
            LastExitAnchor = anchors[1 - i];
            CurrentPosOnTrack = dirOnTrack > 0 ? 0 : 1;
        }
        
        WasOnTrack = true;

        if (Math.Abs(dirOnTrack) < 1d / 64d) {
            CurrentTrackSpeed = 0;
            return;
        }
        // CurrentTrackDirection = anchors.DirDeltaNorm;

        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;

        WasMovingForward = CurrentTrackSpeed > 0;

        CurrentTrackSpeed += track.ConstantAcceleration * deltaTime - CurrentTrackSpeed * track.Friction * deltaTime;
        
        CurrentPosOnTrack += deltaTime * CurrentTrackSpeed / anchors.DeltaL;
        
        entity.ServerPos
            .SetPos(
                new Vec3d(
                    GameMath.Lerp(la.X, ha.X, CurrentPosOnTrack),
                    GameMath.Lerp(la.Y, ha.Y, CurrentPosOnTrack),
                    GameMath.Lerp(la.Z, ha.Z, CurrentPosOnTrack)
                ).Add(bp)
            );
    }
    
    private void Derail() {
        CurrentTrackSpeed = 0;
        //Add more involved logic?
    }
}