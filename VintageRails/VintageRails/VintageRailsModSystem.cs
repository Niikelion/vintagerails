using VintageRails.Behaviors;
using VintageRails.Blocks;
using VintageRails.Entities;
using VintageRails.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VintageRails
{
    public class VintageRailsModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass(Mod.Info.ModID + ".Rails", typeof(BlockTrack));

            api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".CartTrack", typeof(BlockBehaviorCartTrack));
            api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".OverridePick", typeof(BlockBehaviorOverridePick));
            
            api.RegisterEntity(Mod.Info.ModID + ".Cart", typeof(EntityCart));
            api.RegisterEntity(Mod.Info.ModID + ".SeatSup", typeof(EntitySeatInstSupplier));
            
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".TrackRider", typeof(TrackRiderEntityBehaviour));
        }

        public override void StartClientSide(ICoreClientAPI api) {
            api.RegisterEntityRendererClass(Mod.Info.ModID + ".ShapeFixedRot", typeof(YawPitchEntityShapeRenderer));
        }
    }
}