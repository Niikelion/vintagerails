using System;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Rails;
using VintageRails.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

using static VintageRails.Util.RailUtil;

namespace VintageRails.Behaviors;

public class TrackRiderEntityBehavior : EntityBehavior, IOrderedPhysicsTickBehavior {

    public const string RootAttribute = "vrails.trackRider";
    public const string PosOnTrackAttribute = "posOnTrack";
    public const string SpeedAttribute = "vrails.speed";
    public const string PreviousSpeedAttribute = "previousSpeedStd";
    public const string WasOnTrackAttribute = "wasOnTrack";
    public const string FacingAttribute = "vrails.facing";
    public const string PreviousTrackPosAttribute = "previousTrackPos";

    private const double Mass = 1.0;
    
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
        get => entity.WatchedAttributes.GetInt(FacingAttribute, 1);
        private set => entity.WatchedAttributes.SetInt(FacingAttribute, value);
    }
    
    public double PosOnTrack {
        get => PersistentData.GetDouble(PosOnTrackAttribute, 0);
        set {
            PersistentData.SetDouble(PosOnTrackAttribute, value);
            MarkDirty();
        } 
    }
    
    public double Speed {
        get => entity.WatchedAttributes.GetDouble(SpeedAttribute, 0);
        set {
            entity.WatchedAttributes.SetDouble(SpeedAttribute, value);
        }
    }

    public double PreviousSpeed {
        get => PersistentData.GetDouble(PreviousSpeedAttribute, 0);
        set {
            PersistentData.SetDouble(PreviousSpeedAttribute, value);
            MarkDirty();
        } 
    }
    
    public BlockPos? PreviousBp {
        get => PersistentData.GetBlockPos(PreviousTrackPosAttribute, null);
        private set {
            if(value != null) {
                PersistentData.SetBlockPos(PreviousTrackPosAttribute, value);
            }
            else {
                PersistentData.RemoveAttribute(PreviousTrackPosAttribute);
            }
            MarkDirty();
        } 
    }
    
    public TrackAnchorData? LastAnchorData => _lastAnchors;

    private TrackAnchorData? _lastAnchors = null;

    private EntityBehaviorPassivePhysics? _physics = null;
    private EntityPartitioning _partitionUtil;

    private readonly EntityPos _nextPos = new EntityPos();
    
    private string _movingAnimation = "moving";

    private AnimationMetaData _animMeta = new AnimationMetaData();
    
    [NotNull] private ITreeAttribute? PersistentData { get; set; }

    public TrackRiderEntityBehavior(Entity entity) : base(entity) {
        
    }
    
    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);
        
        _animMeta.Animation = attributes["movingAnimation"].AsString(_movingAnimation);
        _animMeta.Code = _animMeta.Animation;
        _animMeta.AnimationSpeed = 1;
        _animMeta = _animMeta.Init();
        
        _partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
        PersistentData = entity.Attributes.GetOrAddTreeAttribute(RootAttribute);
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        if (entity.World.Side == EnumAppSide.Client) {
            entity.AnimManager.StartAnimation(_animMeta);
        }
        
        
        _physics = entity.GetBehavior<EntityBehaviorPassivePhysics>();
    }

    public override string PropertyName() {
        return "vrails.track_rider";
    }

    // public override void OnEntitySpawn() {
    //     _physics = entity.GetBehavior<EntityBehaviorPassivePhysics>();
    // }
    //
    // public override void OnEntityLoaded() {
    // }

    public override void OnGameTick(float deltaTime) {
        if(entity.World.Side == EnumAppSide.Client) {
            _animMeta.AnimationSpeed = (float)entity.WatchedAttributes.GetDouble(SpeedAttribute) * Facing;
        }
    }

    public int GetFacingAnchorIndex(CartDirection cartDirection) => cartDirection switch {
        CartDirection.Forward => DirectionToAnchor(Facing),
        CartDirection.Backward => DirectionToAnchor(-Facing),
        CartDirection.Any => throw new ArgumentException("Invalid direction (cannot use Any)"),
        _ => throw new ArgumentException("Invalid direction")
    };
    
    //TODO move to physics update
    void IOrderedPhysicsTickBehavior.OnTick(double dt) {
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
            Rerail(anchors, bp, ref speed, ref posOnTrack);
            wasRerailed = true;
        }
        
        WasOnTrack = true;
        
        if (bp != previousBp) {
            PreviousBp = previousBp.Set(bp);
            _lastAnchors = anchors;
        }
        
        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;
        
        speed += track.ConstantAcceleration * dt - speed * track.Friction * dt;

        ApplyCollisionsAndPushing(ref speed);
        
        // posOnTrack +=  / anchors.DeltaL;
        ApplyMovement(posOnTrack,dt * speed, out var movementCorrection, out var facingCorrection);
        speed *= movementCorrection;
        Facing *= facingCorrection;
        
        var s = Facing;
        var adn2 = anchors.AnchorDeltaNorm.Clone();

        var y = -(float)(Math.Atan2(adn2.Z * s, adn2.X * s) - Math.PI / 2.0);
        var p = 0f;
        var r = (float)(Math.Acos(adn2.Dot(new Vec3d(0, 1, 0))) - Math.PI / 2.0) * s;

        _nextPos.SetAngles(r, y, p);
        
        Speed = speed;
    }
    
    void IOrderedPhysicsTickBehavior.AfterTick(double dt) {
        PreviousSpeed = Speed;
        if (WasOnTrack) {
            entity.ServerPos.SetAngles(_nextPos).SetPos(_nextPos);
            entity.Pos.SetFrom(entity.ServerPos);
        }
    }
    
    private void ApplyCollisionsAndPushing(ref double speed) {
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
            // EntityBehaviorCreatureCarrier
            // EntityBehaviorSeatable
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
    
    private void Rerail(TrackAnchorData anchors, BlockPos railPos, ref double speed, ref double posOnTrack) {
        if (_physics != null) {
            _physics.Ticking = false;
            var motion = entity.SidedPos.Motion;
            var anchorDeltaNorm = anchors.AnchorDeltaNorm;
            var motionDot = anchorDeltaNorm.Dot(motion);
            speed = motionDot / U.PhysicsTickInterval;
            
            var localPos = anchors.LowerAnchor.SubCopy(entity.Pos.XYZ.RelativeToCenter(railPos));
            var positionDot = -localPos.Dot(anchorDeltaNorm) / anchors.DeltaL;

            posOnTrack = positionDot;
        }
    }
    
    private void ApplyMovement(double currentPos, double movement, out int movementCorrection, out int facingCorrection) {
        var newPos = currentPos + movement;

        var deltaL = _lastAnchors!.DeltaL;
        
        // var posAbs = Math.Abs(deltaL / 2 - newPos) * 2;
        
        var bp = entity.Pos.AsBlockPos;
        
        /*
         * dL = 1; nP = 1.1
         * i = nP - dL / 2 = 0.6
         * s = sign(i)
         * pA = abs(i) * 2 = abs(1 / 2 - 1.1) * 2 = 1.2
         * pA -= dL = 0.2
         * pA1 = (pA / 2) / dL = pA / (2 * dL) = 0.2 / 2 = 0.1
         * pA2 = i > 0 ? pA1 : 1 - pA1 = 0.1 Correct
         */
        /*
         * dl = 1; nP = -0.1
         * i = -0.1 - 0.5 = -0.6
         * s = -1
         * pA = 1.2
         * pA -= 1 = 0.2
         * pA1 = 0.2 / 2 = 0.1
         * pA2 = 1 - pA1 = 0.9
         */
        /*
         * dl = 0.5; nP = 2.1 //4 rails forward 0.2 leftover
         * i = 2.1 - 0.25 = 1.85
         * s = 1
         * pA = 1.85 * 2 = 3.7
         * pA -= dL * 7 = 0.2
         * pA1 = 0.1 / dl = 0.2
         */
        
        if (newPos > deltaL || newPos < 0) {
            // var deltaL2 = deltaL;
            //Always replaced
            Vec3d pEnd = Vec3d.Zero;
            var anchors = _lastAnchors;
            var previousAnchors = anchors;
            var entry = anchors.GetEntryFromMovement(movement);
            var previousEntry = entry;
            var entryOrig = entry;
            
            var posAbs = newPos > deltaL ? newPos - deltaL : -newPos;
            while (posAbs > 0) {
                pEnd = (anchors[1 - entry]).AddToCenter(bp);
                previousAnchors = anchors;
                previousEntry = entry;
                (bp, anchors, entry) = RailUtil.GetNextTrack(entity.World, bp, anchors, entry);
                if (anchors == null) {
                    break;
                }
                //For derailment
                _lastAnchors = anchors;
                deltaL = anchors.DeltaL;
                posAbs -= deltaL;
            }
            posAbs += deltaL;
            
            if (anchors == null) {
                _nextPos.SetPos(pEnd + previousAnchors.AnchorDeltaNorm * 0.1 * -(previousEntry * 2 - 1));
                movementCorrection = 1;
                facingCorrection = 1;
                return;
            }

            movementCorrection = entryOrig == entry ? 1 : -1;
            facingCorrection = entryOrig == entry ? 1 : -1;
            
            //posAbs *= -(entryOrig * 2 - 1);
            if (entry == 1) {
                posAbs = deltaL - posAbs;
            }
            
            ApplyNextPos(bp, anchors, posAbs / deltaL);
            PosOnTrack = posAbs;
        }
        else {
            ApplyNextPos(bp, _lastAnchors, newPos / deltaL);
            movementCorrection = 1;
            facingCorrection = 1;
            PosOnTrack = newPos;
        }
    }

    private void ApplyNextPos(BlockPos bp, TrackAnchorData anchors, double posFactor) {
        var la = anchors.LowerAnchor;
        var ha = anchors.HigherAnchor;
        _nextPos.SetPos(
            new Vec3d(
                GameMath.Lerp(la.X + 0.5, ha.X + 0.5, posFactor),
                GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, posFactor),
                GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, posFactor)
            ).Add(bp)
        );
    }
    
    private void MarkDirty() {
        entity.Attributes.MarkPathDirty(RootAttribute);
    }
}