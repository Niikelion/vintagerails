using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageRails.Renderer;

public class YawPitchEntityShapeRenderer : EntityShapeRenderer {

    protected IMountable? IMS;
  
    public YawPitchEntityShapeRenderer(Entity entity, ICoreClientAPI api) : base(entity, api) {
    }

    public override void TesselateShape() {
      base.TesselateShape();
      IMS = this.entity.GetInterface<IMountable>();
    }

    public override void DoRender3DOpaque(float dt, bool isShadowPass) {
        // base.DoRender3DOpaque(dt, isShadowPass);
        if (isSpectator)
            return;
        loadModelMatrixFixed(this.entity, dt, isShadowPass);
        Vec3d cameraPos = this.capi.World.Player.Entity.CameraPos;
        OriginPos.Set((float) (this.entity.Pos.X - cameraPos.X), (float) (this.entity.Pos.InternalY - cameraPos.Y), (float) (this.entity.Pos.Z - cameraPos.Z));
        if (isShadowPass)
            DoRender3DAfterOIT(dt, true);
        if (!DoRenderHeldItem || this.entity.AnimManager.ActiveAnimationsByAnimCode.ContainsKey("lie") || this.isSpectator)
            return;
        RenderHeldItem(dt, isShadowPass, false);
        RenderHeldItem(dt, isShadowPass, true);
    }

    
    private void loadModelMatrixFixed(Entity entity, float dt, bool isShadowPass) {
        EntityPlayer entityPlayer = capi.World.Player.Entity;
        Mat4f.Identity(ModelMat);

        // If the player is sitting on this entity, simply render it at the offset of the seat to the entity, this prevents jitter.
        IMountableSeat seat;
        if (IMS != null && (seat = IMS.GetSeatOfMountedEntity(entityPlayer)) != null)
        {
            var offset = seat.SeatPosition.XYZ - seat.MountSupplier.Position.XYZ;
            ModelMat = Mat4f.Translate(ModelMat, ModelMat, -(float)offset.X, -(float)offset.Y, -(float)offset.Z);
        }
        else
        {
            seat = eagent?.MountedOn;
            // If this entity itself is riding something, render it as offset of that entity
            if (IMS != null && seat != null)
            {
                if (entityPlayer.MountedOn?.Entity == eagent.MountedOn.Entity)
                {
                    var selfMountPos = entityPlayer.MountedOn.SeatPosition;
                    Mat4f.Translate(ModelMat, ModelMat, (float)(seat.SeatPosition.X - selfMountPos.X), (float)(seat.SeatPosition.InternalY - selfMountPos.Y), (float)(seat.SeatPosition.Z - selfMountPos.Z));
                }
                else
                {
                    Mat4f.Translate(ModelMat, ModelMat, (float)(seat.SeatPosition.X - entityPlayer.CameraPos.X), (float)(seat.SeatPosition.InternalY - entityPlayer.CameraPos.Y), (float)(seat.SeatPosition.Z - entityPlayer.CameraPos.Z));
                }
            }
            else
            {
                Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));
            }
        }

        float rotX = entity.Properties.Client.Shape?.rotateX ?? 0;
        float rotY = entity.Properties.Client.Shape?.rotateY ?? 0;
        float rotZ = entity.Properties.Client.Shape?.rotateZ ?? 0;

        Mat4f.Translate(ModelMat, ModelMat, 0, entity.SelectionBox.Y2 / 2, 0);
        
        double[] quat = Quaterniond.Create();
        float bodyPitch = entity is EntityPlayer ? 0 : entity.Pos.Pitch;
        float yaw = entity.Pos.Yaw + (rotY + 90) * GameMath.DEG2RAD;


        BlockFacing climbonfacing = entity.ClimbingOnFace;

        // To fix climbing locust rotation weirdnes on east and west faces. Brute forced fix. There's probably a correct solution to this.
        bool fuglyHack = entity.Properties.RotateModelOnClimb && entity.ClimbingOnFace?.Axis == EnumAxis.X;
        float sign = -1;

        //Was XYZ
        Quaterniond.RotateY(quat, quat, (fuglyHack ? 0 : yaw));
        Quaterniond.RotateZ(quat, quat, entity.Pos.Roll + /*stepPitch +*/ rotZ * GameMath.DEG2RAD + (fuglyHack ? GameMath.PIHALF * (climbonfacing == BlockFacing.WEST ? -1 : 1) : 0));
        Quaterniond.RotateX(quat, quat, bodyPitch + rotX * GameMath.DEG2RAD + (fuglyHack ? yaw * sign : 0));
        
        // Quaterniond.RotateX(quat, quat, xangle);
        // Quaterniond.RotateY(quat, quat, yangle);
        // Quaterniond.RotateZ(quat, quat, zangle);


        float[] qf = new float[quat.Length];
        for (int i = 0; i < quat.Length; i++) qf[i] = (float)quat[i];
        Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), qf));
        if (shouldSwivelFromMotion)
        {
            Mat4f.RotateX(ModelMat, ModelMat, nowSwivelRad);
        }

        float scale = entity.Properties.Client.Size;
        Mat4f.Translate(ModelMat, ModelMat, 0, -entity.SelectionBox.Y2 / 2, 0f);
        Mat4f.Scale(ModelMat, ModelMat, new float[] { scale, scale, scale });
        Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0, -0.5f);
    }
}