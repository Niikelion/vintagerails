using System;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors;

public class TrackRiderEntityBehaviour : EntityBehavior {

    public const string RootAttribute = "vrails.trackRider";
    public const string PosOnTrackAttribute = "posOnTrack";
    public const string SpeedAttribute = "speed";
    public const string WasOnTrackAttribute = "wasOnTrack";
    public const string FacingAttribute = "facing";
    public const string PreviousTrackPosAttribute = "previousTrackPos";
    
    public bool WasOnTrack {
        get => PersistentData.GetBool(WasOnTrackAttribute, false);
        private set {
            PersistentData.SetBool(WasOnTrackAttribute, value);
            MarkDirty();
        } 
    }
    /// <summary>
    /// Only 1 or -1
    /// </summary>
    public int Facing {
        get => PersistentData.GetInt(FacingAttribute, 1);
        private set {
            PersistentData.SetInt(FacingAttribute, value);
            MarkDirty();
        } 
    }
    
    private double PosOnTrack {
        get => PersistentData.GetDouble(PosOnTrackAttribute, 0);
        set {
            PersistentData.SetDouble(PosOnTrackAttribute, value);
            MarkDirty();
        } 
    }
    private double Speed {
        get => PersistentData.GetDouble(SpeedAttribute, 0);
        set {
            PersistentData.SetDouble(SpeedAttribute, value);
            MarkDirty();
        } 
    }

    private TrackAnchorData? _lastAnchors = null;

    private EntityBehaviorPassivePhysics? _physics = null;
    private EntityPartitioning _partitionUtil;
    
    private BlockPos? PreviousBp {
        get => PersistentData.GetBlockPos(PreviousTrackPosAttribute, null);
        set {
            if(value != null) {
                PersistentData.SetBlockPos(PreviousTrackPosAttribute, value);
            }
            else {
                PersistentData.RemoveAttribute(PreviousTrackPosAttribute);
            }
            MarkDirty();
        } 
    }

    [NotNull] private ITreeAttribute? PersistentData { get; set; }

    public TrackRiderEntityBehaviour(Entity entity) : base(entity) {
        
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);
        
        _partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
        
        PersistentData = entity.Attributes.GetOrAddTreeAttribute(RootAttribute);
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
        //cache1
        var speed = Speed;
        
        var tolerance = RailUtil.SnapToleranceBase + (WasOnTrack ? Math.Abs(speed) * dt : entity.SidedPos.Motion.Length());
        
        var entityPos = entity.SidedPos.XYZ;
        var (track, anchors, bp) = entity.World.GetTrackData(entityPos, tolerance);
        PreviousBp ??= bp;
        //cache2
        var previousBp = PreviousBp;
        var posOnTrack = PosOnTrack;
        
        
        //Restores anchors from previous position (needed after save loading)
        _lastAnchors ??= entity.World.GetBlockBehaviour<BlockBehaviorCartTrack>(previousBp)?.GetAnchorData();
        
        
        if (track == null || anchors == null /* Does nothing anchors are not null when track is not null */) {
            if (WasOnTrack) {
                Derail();
            }

            WasOnTrack = false;
            return;
        }
        
        var wasRerailed = false;
        if (!WasOnTrack) {
            Rerail(anchors, bp, ref posOnTrack);
            wasRerailed = true;
        }
        
        WasOnTrack = true;
        var spdSign = Math.Sign(speed);

        var dirOnTrack = (spdSign == 0 ? 1d : spdSign);
        
        if (bp != previousBp) {
            if (!wasRerailed) {
                // var localPos = entityPos.RelativeToCenter(bp);//.Sub(new Vec3d(bp.X + 0.5f, bp.Y + 0.5f, bp.Z + 0.5f));
                var i1 = _lastAnchors.GetEntryFromMovement(speed);
                var b = _lastAnchors[1 - i1];
                var i2 = anchors.ClosestAnchor(b.AddCopy(previousBp - bp));
                var a = anchors[i2];
                
                dirOnTrack = -a.X * b.X + -a.Z * b.Z;
                
                var i1s = -(i1 * 2 - 1);
                var i2s = -(i2 * 2 - 1);

                //12
                //      2.1 => 0.1
                //      1.1 => 0.1
                //00 => pos = pos - trunc(abs(pos))
                //      2.1 => 0.9
                //      1.1 => 0.9
                //01 => pos = (1 + trunc(abs(pos))) - pos
                //     -1.1 => 0.1
                //     -0.1 => 0.1
                //10 => pos = -pos - trunc(abs(pos))
                //     -1.1 => 0.9
                //     -0.1 => 0.9
                //11 => pos = (1 + trunc(abs(pos))) + pos

                //this is abs
                posOnTrack *= i1s;
                var tPos = (int) posOnTrack;
                if (i2 == 0) {
                    posOnTrack -= tPos;
                }
                else {
                    posOnTrack = (1 + tPos) - posOnTrack;
                }
                
                speed *= Math.Sign(speed) * i2s;
                Facing *= i1 == i2 ? 1 : -1;
            }
            PreviousBp = previousBp.Set(bp);
            _lastAnchors = anchors;
        }

        //Stop on 90deg turn
        if (Math.Abs(dirOnTrack) < 1d / 64d) {
            Speed = 0;
            return;
        }
        
        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;
        
        speed += track.ConstantAcceleration * dt - speed * track.Friction * dt;

        ApplyCollisions(ref speed);
        
        posOnTrack += dt * speed / anchors.DeltaL;

        PosOnTrack = posOnTrack;

        Speed = speed;
        
        var s = Facing;
        var adn2 = anchors.AnchorDeltaNorm.Clone();

        var y = -(float)(Math.Atan2(adn2.Z * s, adn2.X * s));
        var p = (float)(Math.Acos(adn2.Dot(new Vec3d(0, 1, 0))) - Math.PI / 2.0) * s;
        var r = 0f;
        
        entity.ServerPos
            .SetAngles(r, y, p)
            .SetPos(
                new Vec3d(
                    GameMath.Lerp(la.X + 0.5, ha.X + 0.5, posOnTrack),
                    GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, posOnTrack),
                    GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, posOnTrack)
                ).Add(bp)
            );
        
        entity.Pos.SetFrom(entity.ServerPos);
    }

    private void ApplyCollisions(ref double speed) {
        var pos = entity.Pos.XYZ;
        var radius = Math.Max(
            Math.Max(
                entity.SelectionBox.Height,
                entity.SelectionBox.Width
                ),
            entity.SelectionBox.Length) / 2;
        var s = speed;
        _partitionUtil.WalkEntityPartitions(pos, radius + _partitionUtil.LargestTouchDistance + 0.1, e => {
            var ret = HandleEntityCollision(e, ref s);
            return ret;
        });
        speed = s;
    }

    private bool HandleEntityCollision(Entity e, ref double speed) {
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
            // EntityBoatSeat
            // EntityBehaviorSelectionBoxes
            // EntitySidedProperties
            // EntityBehaviorAttachable
            // EntityRideableSeat
            var dot = _lastAnchors.AnchorDeltaNorm.Dot(dv) / l;
            var l2 = Math.Clamp(l, 0, maxDist) / maxDist;
            var l3 = 1 - l2;

            var pushFactor = 5.0;
            
            speed += l3 * dot * pushFactor;
        }
        
        return true;
    }

    private void Derail() {
        if (_physics != null) {
             _physics.Ticking = true;
            if (_lastAnchors != null) {
                var i = _lastAnchors.GetEntryFromMovement(Speed);
                var speed = Math.Abs(Speed);
                entity.SidedPos.Motion.Set(_lastAnchors[1] - _lastAnchors[0]).Normalize().Mul(Speed * U.PhysicsTickInterval);
            }
        }
        Speed = 0;
        PreviousBp = null;
        _lastAnchors = null;
        // Add more involved logic?
    }
    
    private void Rerail(TrackAnchorData anchors, BlockPos railPos, ref double posOnTrack) {
        if (_physics != null) {
            _physics.Ticking = false;
            var motion = entity.SidedPos.Motion;
            var anchorDeltaNorm = anchors.AnchorDeltaNorm;
            var motionDot = anchorDeltaNorm.Dot(motion);
            Speed = motionDot / U.PhysicsTickInterval;
            
            var localPos = anchors.LowerAnchor.SubCopy(entity.Pos.XYZ.RelativeToCenter(railPos));
            var positionDot = -localPos.Dot(anchorDeltaNorm) / anchors.DeltaL;

            posOnTrack = positionDot;
        }
    }

    private void MarkDirty() {
        entity.Attributes.MarkPathDirty(RootAttribute);
    }
}