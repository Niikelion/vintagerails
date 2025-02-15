using System;
using System.Collections.Generic;
using System.Linq;
using VintageRails.Behaviors;
using VintageRails.Behaviors.Collectibles.Blocks;
using VintageRails.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace VintageRails.Items;

public class ItemTrackWrench: Item
{
    private delegate void ToolModeAction(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world);
    
    private SkillItem[]? toolModes;
    private readonly ToolModeAction[] modeActions = {
        CycleSmart,
        CycleConnect,
        CycleFlat,
        CycleCurved,
        CycleRaised
    };

    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        
        var clientApi = coreApi as ICoreClientAPI;

        toolModes = ObjectCacheUtil.GetOrCreate(api, "trackWrenchToolModes", () => new []
        {
            CreateSkill(clientApi, "smart", "mode-cart-wrench-smart"),
            CreateSkill(clientApi, "connect", "mode-cart-wrench-connect"),
            CreateSkill(clientApi, "straight", "mode-cart-wrench-straight"),
            CreateSkill(clientApi, "curved", "mode-cart-wrench-curved"),
            CreateSkill(clientApi, "raised", "mode-cart-wrench-raised"),
        });
    }

    private static SkillItem CreateSkill(ICoreClientAPI? api, string code, string nameKey)
    {
        SkillItem item = new()
        {
            Code = new(code),
            Name = Lang.Get($"vintagerails:{nameKey}")
        };

        if (api != null) item.WithLetterIcon(api, code[..2].ToUpper());
        
        return item;
    }
    
    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) =>
        toolModes!;

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection) =>
        Math.Min(toolModes!.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        => slot.Itemstack.Attributes.SetInt("toolMode", toolMode);

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection? blockSel,
        EntitySelection? entitySel,
        bool firstEvent,
        ref EnumHandHandling handling
    )
    {
        if (blockSel == null) {
            return;
        }
        
        var playerEntity = byEntity as EntityPlayer;
        
        var block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        var trackVariantsProvider = block.GetBehavior<BlockBehaviorTrackVariantProvider>();

        if (trackVariantsProvider == null || playerEntity == null)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        handling = EnumHandHandling.Handled;

        if (!playerEntity.Api.Side.IsServer()) return;
        
        modeActions[GetToolMode(slot, playerEntity.Player, blockSel)](blockSel, playerEntity.Controls.ShiftKey, trackVariantsProvider, byEntity.World);
    }

    private static void CycleSmart(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world)
    {
        var variantsSet = new HashSet<AssetLocation>();
        var allVariants = new List<(Block block, int score)>();

        var neighbours = new List<(BlockPos pos, BlockBehaviorCartTrack track)>();

        foreach (var facing in BlockFacing.HORIZONTALS)
            for (int i = -1; i <= 1; ++i)
            {
                var blockPos = block.Position.AddCopy(facing);
                blockPos.Y += i;
                var neighbourBlock = world.BlockAccessor.GetBlock(blockPos);
                var neighbourTrack = neighbourBlock.GetCollectibleBehavior<BlockBehaviorCartTrack>(true);
                if (neighbourTrack == null) continue;
                
                neighbours.Add((blockPos, neighbourTrack));
            }
        
        foreach (string variant in variantProvider.FlatTypes)
            AddIfNotPresent(variant);
        foreach (string variant in variantProvider.CurvedTypes)
            AddIfNotPresent(variant);
        foreach (string variant in variantProvider.RaisedTypes)
            AddIfNotPresent(variant);
        
        var currentVariant = variantProvider.CurrentType;

        int currentIndex = allVariants.IndexOf(v => v.block.Code == currentVariant);
        int maxScore = allVariants.Max(v => v.score);
        
        for (int i = 1; i < allVariants.Count; i++)
        {
            int offset = reverse ? -i : i;
            int index = (currentIndex + offset + allVariants.Count) % allVariants.Count;
            
            var variant = allVariants[index];
            
            if (variant.score < maxScore) continue;
            
            world.BlockAccessor.SetBlock(variant.block.BlockId, block.Position);
            return;
        }
        
        return;

        void AddIfNotPresent(AssetLocation variant)
        {
            if (variantsSet.Contains(variant))
                return;

            var variantBlock = world.BlockAccessor.GetBlock(variant);

            var variantTrack = variantBlock?.GetCollectibleBehavior<BlockBehaviorCartTrack>(true);
            
            if (variantTrack == null || variantBlock == null) return;

            int score = neighbours.Count(neighbour =>
                RailUtil.CanConnect(neighbour, (block.Position, variantTrack)));

            allVariants.Add((variantBlock, score));
            variantsSet.Add(variant);
        }
    }
    
    private static void CycleConnect(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world)
    {
        //
    }

    private static void CycleFlat(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world) =>
        CycleVariants(block, world, reverse, variantProvider.FlatTypes, variantProvider.CurrentType);

    private static void CycleCurved(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world) =>
        CycleVariants(block, world, reverse, variantProvider.CurvedTypes, variantProvider.CurrentType);

    private static void CycleRaised(BlockSelection block, bool reverse, BlockBehaviorTrackVariantProvider variantProvider, IWorldAccessor world) =>
        CycleVariants(block, world, reverse, variantProvider.RaisedTypes, variantProvider.CurrentType);

    private static void CycleVariants(BlockSelection block, IWorldAccessor world, bool reverse, AssetLocation[] availableVariants, AssetLocation currentVariant)
    {
        int currentVariantId = availableVariants.IndexOf(currentVariant);
        int nextVariantId = (currentVariantId + (reverse ? -1 : 1) + availableVariants.Length) % availableVariants.Length;
        
        var nextVariant = availableVariants[nextVariantId];
        
        var nextBlock = world.GetBlock(nextVariant);
        
        world.BlockAccessor.SetBlock(nextBlock.BlockId, block.Position);
    }
}