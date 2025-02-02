﻿using VintageRails.Behaviors;
using VintageRails.Blocks;
using VintageRails.Entities;
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
        }
    }
}