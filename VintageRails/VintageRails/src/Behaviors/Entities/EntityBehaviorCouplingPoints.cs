using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace VintageRails.Behaviors.Entities;

public class EntityBehaviorCouplingPoints : EntityBehavior, IRenderer {

    public const double SearchRadius = 2;
    
    public static readonly int OpenColor = ColorUtil.ColorFromRgba(0, 255, 255, 255); // Yellow
    public static readonly int ClosedColor = ColorUtil.ColorFromRgba(0, 0, 255, 255); // Red
    public static readonly int LinkedColor = ColorUtil.ColorFromRgba(0, 255, 0, 255); // Green

    private static readonly SimpleParticleProperties MarkerParticles = new() {
        MinVelocity = Vec3f.Zero,
        AddVelocity = Vec3f.Zero,
        AddPos = Vec3d.Zero,
        GravityEffect = 0,
        MinQuantity = 1,
        AddQuantity = 0,
        LifeLength = 0.1f,
        MinSize = 1f,
        MaxSize = 1f,
        ParticleModel = EnumParticleModel.Cube
    };

    [NotNull] private EntityPartitioning? EntityPartitioning { get; set; }

    [NotNull] private CouplingPoint[]? CouplingPoints { get; set; }

    public EntityBehaviorCouplingPoints(Entity entity) : base(entity) {
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        base.Initialize(properties, attributes);

        var defs = attributes["points"].AsObject<CouplingPoint.Def[]>();

        var i = 0;
        CouplingPoints = defs.Select(def => new CouplingPoint(def, i++, this)).ToArray();

        var api = entity.Api;
        EntityPartitioning = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
        
        if (api is ICoreClientAPI capi) {
            InitRenderer(capi);
        }
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);
    }

    public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled) {
        base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);

        var player = (EntityPlayer)byEntity;
        var sel = player.EntitySelection;
        if (sel == null || sel.Entity != entity || player.World.Side != EnumAppSide.Server) {
            return;
        }

        var cp = GetCouplingPointFromSelection(sel.SelectionBoxIndex - 1);
        cp?.ToggleState();
    }

    //Needed so Attachments won't crash
    public override WorldInteraction[]? GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled) {
        if (es.SelectionBoxIndex >= 0 && GetCouplingPointFromSelection(es.SelectionBoxIndex - 1) != null) {
            handled = EnumHandling.PreventSubsequent;
        }

        return null;
    }

    public override string PropertyName() {
        return "vrails.coupling_points";
    }

    public override void OnGameTick(float deltaTime) {
        base.OnGameTick(deltaTime);
        if (entity.World.Side != EnumAppSide.Server) {
            return;
        }
        
        var ePos = entity.ServerPos;
        var mat = CouplingPoint.Def.CreateMat(ePos.Yaw, ePos.Roll, ePos.XYZ); // I know roll is passed as pitch

        HandleConnecting(mat);
        
        #region Debug Particles
        foreach (var cp in CouplingPoints) {
            var cuboid = cp.def.GetCuboid(mat);
            var pos = new Vec3d(
                (cuboid.MinX + cuboid.MaxX) / 2,
                (cuboid.MinY + cuboid.MaxY) / 2,
                (cuboid.MinZ + cuboid.MaxZ) / 2
            );

            MarkerParticles.MinPos = pos;
            MarkerParticles.Color = cp.State switch {
                CouplingPointState.Open => OpenColor,
                CouplingPointState.ClosedConnected => LinkedColor,
                CouplingPointState.ClosedDisconnected => ClosedColor,
                _ => throw new Exception("Never")
            };
            entity.World.SpawnParticles(MarkerParticles);
        }
        #endregion
    }

    private void HandleConnecting(double[] transform) {
        var openPoints = CouplingPoints
            .Where(point => point.State == CouplingPointState.Open)
            .Select(point => (point.def.GetPoint(transform), point))
            .ToArray();
        
        EntityPartitioning.WalkEntities(entity.ServerPos.XYZ, SearchRadius, ent => {
            if (ent == entity) {
                return true;
            }
            var ebcp = ent.GetBehavior<EntityBehaviorCouplingPoints>();
            if (ebcp != null) {
                var entPos = ent.ServerPos;
                var mat = CouplingPoint.Def.CreateMat(entPos.Yaw, entPos.Roll, entPos.XYZ);
                var otherCuboids = ebcp.CouplingPoints
                    .Where(point => point.State == CouplingPointState.Open)
                    .Select(point => (point.def.GetCuboid(mat), point.index))
                    .ToArray();

                foreach (var cuboid in otherCuboids) {
                    foreach (var point in openPoints) {
                        var pointPos = point.Item1;
                        if (cuboid.Item1.Contains(pointPos.X, pointPos.Y, pointPos.Z)) {
                            point.point.ConnectTo(ebcp, cuboid.index);
                            return false;
                        }
                    }
                }
            }
            return true;
        }, EnumEntitySearchType.Inanimate);
    }
    
    public override void OnEntityDespawn(EntityDespawnData despawn) {
        base.OnEntityDespawn(despawn);

        DeinitRenderer();
    }

    private CouplingPoint? GetCouplingPointFromSelection(int selectionBoxId) {
        var selBoxes = entity.GetBehavior<EntityBehaviorSelectionBoxes>().selectionBoxes;
        if (selBoxes.Length < selectionBoxId || selectionBoxId < 0) {
            return null;
        }
        var selBox = selBoxes[selectionBoxId];
        return CouplingPoints.FirstOrDefault(cp => selBox.AttachPoint.Code == cp.def.ap);
    }
    
    
    #region Debug Renderer

    [NotNull] private WireframeCube? Cube { get; set; }

    private ICoreClientAPI? Capi { get; set; }

    public void InitRenderer(ICoreClientAPI capi) {
        Cube = WireframeCube.CreateUnitCube(capi);
        Capi = capi;
        capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition);
    }
    
    public void Dispose() { }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage) {
        var ePos = entity.Pos;
        var mat = CouplingPoint.Def.CreateMat(ePos.Yaw, ePos.Roll, entity.Pos.XYZ); // I know roll is passed as pitch
        foreach (var cp in CouplingPoints) {
            var cuboid = cp.def.GetCuboid(mat);
            var pos = new Vec3d(
                (cuboid.MinX + cuboid.MaxX) / 2,
                (cuboid.MinY + cuboid.MaxY) / 2,
                (cuboid.MinZ + cuboid.MaxZ) / 2
            );

            var eplr = Capi!.World.Player.Entity;
            
            var matf = new Matrixf();
            matf.Set(Capi.Render.CameraMatrixOrigin);
            var relPos = new Vec3d(pos.X - eplr.CameraPos.X, pos.Y - eplr.CameraPos.Y, pos.Z - eplr.CameraPos.Z);
            matf.Translate(relPos.X, relPos.Y, relPos.Z);
            matf.Translate(cuboid.Width / -2, cuboid.Height / -2, cuboid.Length / -2);
            // matf.Invert();
            // matf.Translate(pos.X, pos.Y, pos.Z);
            matf.Scale((float)cuboid.Width, (float)cuboid.Height, (float)cuboid.Length);
            
            Cube.Render(Capi, matf, lineWidth: 5f);
        }
    }

    public void DeinitRenderer() {
        if (Capi == null) {
            return;
        }
        
        Capi.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
        Cube.Dispose();
    }

    public double RenderOrder => 1.0;
    public int RenderRange => 24;
    #endregion
    
    public class CouplingPoint {
        public CouplingPointState State { get; private set; } = CouplingPointState.Open;
        public readonly int index;
        public readonly EntityBehaviorCouplingPoints owner;
        public readonly Def def;
        public (EntityBehaviorCouplingPoints behavior, int idx)? ConnectedTo {
            get {
                if (State == CouplingPointState.ClosedConnected) {
                    if (_connectedTo == null) {
                        throw new ApplicationException("Invalid state");
                    }
                }
                else {
                    if (_connectedTo != null) {
                        throw new ApplicationException("Invalid state");
                    }
                }
                return _connectedTo;
            }
        }

        private (EntityBehaviorCouplingPoints behavior, int idx)? _connectedTo = null;
        
        public CouplingPoint(Def def, int index, EntityBehaviorCouplingPoints owner) {
            this.def = def;
            this.index = index;
            this.owner = owner;
        }

        public void ConnectTo(EntityBehaviorCouplingPoints other, int otherPoint) {
            if (State != CouplingPointState.Open) {
                return;
            }
            if (other.CouplingPoints[otherPoint].State != CouplingPointState.Open) {
                return;
            }
            ConnectToUnchecked(other, otherPoint);
        }
        
        public void ConnectToUnchecked(EntityBehaviorCouplingPoints other, int otherPoint) {
            var p1 = this;
            var p2 = other.CouplingPoints[otherPoint];

            p1._connectedTo = (other, otherPoint);
            p2._connectedTo = (owner, index);
            
            p1.State = CouplingPointState.ClosedConnected;
            p2.State = CouplingPointState.ClosedConnected;
        }

        public void Disconnect(bool setOpen, bool setOtherOpen) {
            if (_connectedTo == null) {
                return;
            }

            var ct = _connectedTo.Value;

            var otherPoint = ct.behavior.CouplingPoints[ct.idx];
            if (otherPoint._connectedTo == null ||
                otherPoint._connectedTo.Value.behavior != owner ||
                otherPoint._connectedTo.Value.idx != index) {
                owner.entity.World.Logger.Warning("[Rails] Invalid coupling data, may encounter further issues");
            }

            _connectedTo = null;
            otherPoint._connectedTo = null;
            
            State = setOpen ? CouplingPointState.Open : CouplingPointState.ClosedDisconnected;
            otherPoint.State = setOtherOpen ? CouplingPointState.Open : CouplingPointState.ClosedDisconnected;
        }

        public void ToggleState() {
            if (State == CouplingPointState.ClosedConnected) {
                Disconnect(true, false);
            }

            if ((State & CouplingPointState.OpenBit) == 0) {
                State = CouplingPointState.Open;
            }
            else {
                State = CouplingPointState.ClosedDisconnected;
            }
        }
        
        public void Validate() {
            var ct = ConnectedTo;
            if (ct != null) {
                if (!ct.Value.behavior.entity.Alive) {
                    Disconnect(true, false);
                }
            }
        }
        
        public struct Def {
            public Vec3d localPos;
            public Vec3d boxSize;
            public string ap;
            
            [Pure]
            public Vec3d GetPoint(double[] transform) {
                var actualPos = new Vec4d();
                
                Mat4d.MulWithVec4(transform, new Vec4d(localPos.X, localPos.Y, localPos.Z, 1), actualPos);

                return actualPos.XYZ;
            }
            
            [Pure]
            public Cuboidd GetCuboid(double[] transform) {
                return GetCuboid(GetPoint(transform));
            }
            
            [Pure]
            public Cuboidd GetCuboid(Vec3d pos) {
                var bs2 = boxSize / 2;
                return new Cuboidd(
                    pos.X - bs2.X,
                    pos.Y - bs2.Y,
                    pos.Z - bs2.Z,
                    pos.X + bs2.X,
                    pos.Y + bs2.Y,
                    pos.Z + bs2.Z);
            }
            
            public static double[] CreateMat(double yaw, double pitch, Vec3d pos) {
                var mat = Mat4d.Create();
                Mat4d.Translate(mat, mat, pos.X, pos.Y, pos.Z );
                Mat4d.RotateY(mat, mat, yaw);
                //-X forward (I think so)
                Mat4d.RotateX(mat, mat, pitch);
                return mat;
            }
        }
    }
}

[Flags]
public enum CouplingPointState {
    ClosedDisconnected = 0, // IsOpen => 0 | IsConnected => 0
    Open = 1, // IsOpen => 1 | IsConnected => 0
    ClosedConnected = 2, // IsOpen => 0 | IsConnected => 2,
    
    OpenBit = 1,
    ConnectedBit = 2
}