using System;
using System.Diagnostics.CodeAnalysis;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Collectibles.Blocks;
using VintageRails.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Entities;

public class EntityBehaviorTrackRider : EntityBehavior, IOrderedPhysicsTickBehavior
{
    private const string RootAttribute = "vrails.trackRider";
    private const string PosOnTrackAttribute = "posOnTrack";
    private const string SpeedAttribute = "vrails.speed";
    private const string PreviousSpeedAttribute = "previousSpeedStd";
    private const string WasOnTrackAttribute = "wasOnTrack";
    private const string FacingAttribute = "vrails.facing";
    private const string PreviousTrackPosAttribute = "previousTrackPos";
    private const string AnchorsAttribute = "anchors";
    
    private const string EngineAnimationSpeedAttribute = "vrails.engineAnim";

    private const double Mass = 1.0;

    public double SideSnappingDistance { get; set; }
    public double UpSnappingDistance { get; set; }
    public double DownSnappingDistance { get; set; }
    
    public bool WasOnTrack {
        get => PersistentData.GetBool(WasOnTrackAttribute);
        private set {
            PersistentData.SetBool(WasOnTrackAttribute, value);
            if (_physics != null)
                _physics.Ticking = !value;
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

    public double EngineAnimationSpeed {
        get => entity.WatchedAttributes.GetDouble(EngineAnimationSpeedAttribute, 0);
        set => entity.WatchedAttributes.SetDouble(EngineAnimationSpeedAttribute, value);
    }
    
    public TrackAnchorData? LastAnchorData { get; private set; }

    private EntityBehaviorPassivePhysics? _physics;
    private EntityBehaviorSeatable? _seats;
    private EntityPartitioning _partitionUtil;
    
    private BlockPos? PreviousBp {
        get => PersistentData.GetBlockPos(PreviousTrackPosAttribute);
        set {
            if(value != null) {
                PersistentData.SetBlockPos(PreviousTrackPosAttribute, value);
            }
            else {
                PersistentData.RemoveBlockPos(PreviousTrackPosAttribute);
            }
            MarkDirty();
        } 
    }

    private EntityPos _nextPos = new();
    private double _nextPosOnTrack;
    
    private string _movingAnimation = "moving";
    private string _engineAnimation = "engine_running";

    private AnimationMetaData _movingAnimMeta = new();
    private AnimationMetaData _engineAnimMeta = new();

    private bool shouldDerail = false;
    
    [NotNull] private ITreeAttribute? PersistentData { get; set; }

    public EntityBehaviorTrackRider(Entity entity) : base(entity) {}
    
    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);
        
        _movingAnimMeta.Animation = attributes["movingAnimation"].AsString(_movingAnimation);
        _movingAnimMeta.Code = _movingAnimMeta.Animation;
        _movingAnimMeta.AnimationSpeed = 1;
        _movingAnimMeta = _movingAnimMeta.Init();
        
        _engineAnimMeta.Animation = attributes["engineAnimation"].AsString(_engineAnimation);
        _engineAnimMeta.Code = _engineAnimMeta.Animation;
        _engineAnimMeta.AnimationSpeed = 1;
        _engineAnimMeta = _engineAnimMeta.Init();
        
        SideSnappingDistance = attributes["sideSnappingDistance"].AsDouble(0.5);
        DownSnappingDistance = attributes["downSnappingDistance"].AsDouble(0.15);
        UpSnappingDistance = attributes["upSnappingDistance"].AsDouble(0.4);
        
        _partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
        PersistentData = entity.Attributes.GetOrAddTreeAttribute(RootAttribute);
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        if (entity.World.Side == EnumAppSide.Client) {
            entity.AnimManager.StartAnimation(_movingAnimMeta);
            entity.AnimManager.StartAnimation(_engineAnimMeta);
        }
        
        _physics = entity.GetBehavior<EntityBehaviorPassivePhysics>();
        _seats = entity.GetBehavior<EntityBehaviorSeatable>();
    }

    public override string PropertyName() => "vrails.track_rider";

    public override void OnGameTick(float deltaTime) {
        if(entity.World.Side == EnumAppSide.Client) {
            _movingAnimMeta.AnimationSpeed = (float)Speed * Facing;
            _engineAnimMeta.AnimationSpeed = (float)EngineAnimationSpeed;
        }
    }

    public override void OnEntityLoaded() {
        //Restores anchors (needed after save loading); May not work after derailment
        LastAnchorData = TrackAnchorData.Decode(PersistentData.GetTreeAttribute(AnchorsAttribute));
        if (WasOnTrack &&  _physics != null) {
           _physics.Ticking = false;
        }
    }

    void IOrderedPhysicsTickBehavior.OnTick(double dt) {
        //cache1
        var speed = Speed;

        // entity.Alive = false;
        
        var entityPos = entity.SidedPos.XYZ;
        //var (track, bp) = entity.World.GetTrackData(entityPos, SideSnappingDistance, DownSnappingDistance, UpSnappingDistance);
        BlockBehaviorCartTrack? track;
        var previousBp = PreviousBp;
        if (previousBp == null) {
            (track, previousBp) = entity.World.GetTrackData(entityPos, SideSnappingDistance, DownSnappingDistance, UpSnappingDistance);
        }
        else {
            track = entity.World.GetTrackAtPos(previousBp);
        }
        
        //cache2
        var posOnTrack = PosOnTrack;
        
        if (track == null) {
            if (WasOnTrack)
                Derail();
            return;
        }

        if (!WasOnTrack) {
            var anchors = track.GetAnchorDataForEntrySide(entity.World, previousBp, null);
            if (anchors == null) {
                return;
            }
            Rerail(anchors, previousBp, ref speed, ref posOnTrack);
            LastAnchorData = anchors;
            SaveLastAnchorData();
        }

        WasOnTrack = true;
        
        ApplyCollisionsAndPushing(ref speed);
        Speed = speed;
        
        ApplyMovement(track, ref posOnTrack, speed, out var facingCorrection, dt);
        Facing *= facingCorrection;
        {
            var anchors = LastAnchorData;
            
            if (anchors != null) {
                var s = Facing;
                var adn2 = anchors.AnchorDeltaNorm.Clone();

                var y = -(float)(Math.Atan2(adn2.Z * s, adn2.X * s) - Math.PI / 2.0);
                var p = 0f;
                //Model has -X Forward
                var r = (float)(Math.Acos(adn2.Dot(new(0, 1, 0))) - Math.PI / 2.0) * s;

                _nextPos.SetAngles(r, y, p);
                _nextPosOnTrack = posOnTrack;
            }
        }
    }
    
    void IOrderedPhysicsTickBehavior.AfterTick(double dt) {
        PreviousSpeed = Speed;
        if (!WasOnTrack) return;

        if (shouldDerail) {
            Derail();
            shouldDerail = false;
        }
        
        PosOnTrack = _nextPosOnTrack;
        // entity.TeleportTo(_nextPos);
        entity.ServerPos.SetAngles(_nextPos).SetPos(_nextPos);
        entity.Pos.SetFrom(entity.ServerPos);
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
        _partitionUtil.WalkEntityPartitions(pos, radius + _partitionUtil.LargestTouchDistance + 0.1, e =>
        {
            var ret = HandleEntityCollision(e, ref s);
            return ret;
        });
        speed = s;
    }
    
    private bool HandleEntityCollision(Entity e, ref double speed)
    {
        if (LastAnchorData == null)
        {
            //Stop iteration
            return false;
        }

        if (_seats != null && _seats.IsMountedBy(e)) {
            return true;
        }
        
        var box1 = entity.SelectionBox;
        var box2 = e.SelectionBox;

        var dv = (box1.Center - box2.Center) + (entity.SidedPos.XYZ - e.SidedPos.XYZ);

        var dx = Math.Abs(dv.X);
        var dy = Math.Abs(dv.Y);
        var dz = Math.Abs(dv.Z);
        
        var maxDx = box1.Width + box2.Width;
        var maxDy = box1.Height + box2.Height;
        var maxDz = box1.Length + box2.Length;

        if (!(dx < maxDx) ||
            !(dy < maxDy) ||
            !(dz < maxDz)) return true;
        
        var maxDist = Math.Min(Math.Min(maxDx, maxDy), maxDz);
            
        var l = dv.Length();
        var dot = LastAnchorData.AnchorDeltaNorm.Dot(dv) / l;
        var l2 = Math.Clamp(l, 0, maxDist) / maxDist;
        var l3 = 1 - l2;

        var pushFactor = 5.0;
            
        speed += l3 * dot * pushFactor;

        return true;
    }
    
    private void Derail() 
    {
        if (_physics != null && LastAnchorData is not null)
            entity.SidedPos.Motion.Set(LastAnchorData[1].offset - LastAnchorData[0].offset).Normalize().Mul(Speed * U.PhysicsTickInterval);
        Speed = 0;
        PreviousBp = null;
        LastAnchorData = null;
        SaveLastAnchorData();
        WasOnTrack = false;
        // Add more involved logic?
    }
    
    private void Rerail(TrackAnchorData anchors, BlockPos railPos, ref double speed, ref double posOnTrack)
    {
        if (_physics == null) return;
        
        var motion = entity.SidedPos.Motion;
        var anchorDeltaNorm = anchors.AnchorDeltaNorm;
        var motionDot = anchorDeltaNorm.Dot(motion);
        speed = motionDot / U.PhysicsTickInterval;
        
        var localPos = anchors.LowerAnchor.offset.SubCopy(entity.Pos.XYZ.RelativeToCenter(railPos));
        var positionDot = -localPos.Dot(anchorDeltaNorm) / anchors.DeltaL;

        posOnTrack = positionDot;
    }

    private void ApplyMovement(BlockBehaviorCartTrack trackIn, ref double currentPos, double speed, out int facingCorrection, double dt) {
        var movement = speed * dt;
        var movementAbs = Math.Abs(movement);
        var newPos = currentPos + movement;
        
        var deltaL = LastAnchorData!.DeltaL;
        
        var bp = entity.Pos.AsBlockPos;

        var dt1 = dt; //I hope this is correct
        if (newPos > deltaL) {
            dt1 *= (newPos - currentPos) / movement;
        }
        else if (newPos < 0) {
            dt1 *= (currentPos - newPos) / movement;
        }
        
        trackIn.OnCartTick(this, PreviousBp!, dt1);
        if (newPos > deltaL || newPos < 0)
        {
            //Always replaced
            Vec3d pEnd = Vec3d.Zero;
            var anchors = LastAnchorData;
            var previousAnchors = anchors;
            var entry = TrackAnchorData.GetEntryFromMovement(movement);
            var previousEntry = entry;
            var entryOrig = entry;
            var track = (BlockBehaviorCartTrack?)trackIn;

            var posAbs = newPos > deltaL ? newPos - deltaL : -newPos;
            while (posAbs > 0) {
                pEnd = anchors[1 - entry].offset.AddToCenter(bp);
                previousAnchors = anchors;
                previousEntry = entry;
                var previousTrack = track!; // will not be null here
                var previousBp = bp;
                (track, bp, entry, anchors) = RailUtil.GetNextTrack(entity.World, bp, anchors, entry);
                previousTrack.OnCartExited(this, previousBp, bp);

                if (anchors == null) break;
                track!.OnCartEntered(this, bp,
                    previousBp); //"!" because nullability checks fail to detect that anchors can only be null when track is null

                var distanceRatio = Math.Min(posAbs, anchors.DeltaL) / movementAbs;
                track!.OnCartTick(this, bp, dt * distanceRatio);

                //For derailment
                // LastAnchorData = anchors;
                deltaL = anchors.DeltaL;
                posAbs -= deltaL;
            }

            if (anchors == null)
            {
                _nextPos.SetPos(pEnd + previousAnchors.AnchorDeltaNorm * 0.1 * -(previousEntry * 2 - 1));
                facingCorrection = 1;
                shouldDerail = true;
                //For derailment
                LastAnchorData = previousAnchors;
                return;
            }
            
            LastAnchorData = anchors;
            SaveLastAnchorData();

            PreviousBp = bp;
            posAbs += deltaL;
            var correction = entryOrig == entry ? 1 : -1;
            Speed *= correction;
            facingCorrection = correction;
            
            if (entry == 1) posAbs = deltaL - posAbs;
            
            ApplyNextPos(bp, anchors, posAbs / deltaL);
            currentPos = posAbs;
        }
        else
        {
            ApplyNextPos(bp, LastAnchorData, newPos / deltaL);
            facingCorrection = 1;
            currentPos = newPos;
        }
    }
    
    private void ApplyNextPos(BlockPos bp, TrackAnchorData anchors, double posFactor) {
        var la = anchors.LowerAnchor.offset;
        var ha = anchors.HigherAnchor.offset;
        _nextPos.SetPos(
            new Vec3d(
                GameMath.Lerp(la.X + 0.5, ha.X + 0.5, posFactor),
                GameMath.Lerp(la.Y + 0.5, ha.Y + 0.5, posFactor),
                GameMath.Lerp(la.Z + 0.5, ha.Z + 0.5, posFactor)
            ).Add(bp)
        );
    }
    
    private void MarkDirty() => entity.Attributes.MarkPathDirty(RootAttribute);

    private void SaveLastAnchorData() {
        if (LastAnchorData != null) {
            PersistentData[AnchorsAttribute] = LastAnchorData.Encode();
        }
        else {
            PersistentData.RemoveAttribute(AnchorsAttribute);
        }
        MarkDirty();
    }
}