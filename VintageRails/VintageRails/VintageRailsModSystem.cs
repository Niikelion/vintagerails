using HarmonyLib;
using VintageRails.Behaviors;
using VintageRails.Behaviors.Collectibles;
using VintageRails.Behaviors.Collectibles.Blocks;
using VintageRails.Behaviors.Entities;
using VintageRails.Blocks;
using VintageRails.Entities;
using VintageRails.Global;
using VintageRails.Items;
using VintageRails.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageRails
{
    public class VintageRailsModSystem : ModSystem {
        public static readonly PhysicsBatch MinecartsBatch = new();
        
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".ItemTrackWrench", typeof(ItemTrackWrench));
            
            api.RegisterBlockClass(Mod.Info.ModID + ".Rails", typeof(BlockTrack));

            api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".CartTrack", typeof(BlockBehaviorCartTrack));
            api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".OverridePick", typeof(BlockBehaviorOverridePick));
            api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".TrackVariantProvider", typeof(BlockBehaviorTrackVariantProvider));
            
            api.RegisterEntity(Mod.Info.ModID + ".Cart", typeof(EntityCart));
            api.RegisterEntity(Mod.Info.ModID + ".SeatSup", typeof(EntitySeatInstSupplier));
            
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".TrackRider", typeof(EntityBehaviorTrackRider));
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".TrackCollision", typeof(EntityBehaviorTrackCollision));
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".OrderedPhysics", typeof(EntityBehaviorOrderedPhysics));
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".TickAttachments", typeof(EntityBehaviorTickingAttachments));
            api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".InfoAttachments", typeof(EntityBehaviorAttachmentInfo));
            
            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".SimpleEngine", typeof(CollectibleBehaviorSimpleCartEngine));
            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".ControlledEngine", typeof(CollectibleBehaviorControlledCartEngine));
            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".CombustionEngine", typeof(CollectibleBehaviorCombustionCartEngine));
            api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".RClickFuel", typeof(CollectibleBehaviorSimpleCartEngineRCFuel));
            new Harmony(Mod.Info.ModID).PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI api) {
            api.RegisterEntityRendererClass(Mod.Info.ModID + ".ShapeFixedRot", typeof(YawPitchEntityShapeRenderer));
        }

        public override void StartServerSide(ICoreServerAPI api) {
            api.Server.AddPhysicsTickable(MinecartsBatch);
        }
    }
}