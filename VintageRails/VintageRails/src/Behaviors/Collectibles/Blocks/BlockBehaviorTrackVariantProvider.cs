using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace VintageRails.Behaviors.Collectibles.Blocks;

public class BlockBehaviorTrackVariantProvider: BlockBehavior
{
    public AssetLocation[] RaisedTypes { get; private set; } = Array.Empty<AssetLocation>();
    public AssetLocation[] CurvedTypes { get; private set; } = Array.Empty<AssetLocation>();
    public AssetLocation[] FlatTypes { get; private set; } = Array.Empty<AssetLocation>();

    public AssetLocation CurrentType => block.Code;
    
    public BlockBehaviorTrackVariantProvider(Block block) : base(block) {}

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        
        RaisedTypes = properties["raised"].AsArray().Select(i => ResolveVariantName(i.AsString())).ToArray();
        CurvedTypes = properties["curved"].AsArray().Select(i => ResolveVariantName(i.AsString())).ToArray();
        FlatTypes = properties["flat"].AsArray().Select(i => ResolveVariantName(i.AsString())).ToArray();
    }
    
    protected virtual AssetLocation ResolveVariantName(string variantName) => block.CodeWithVariant("type", variantName);
}