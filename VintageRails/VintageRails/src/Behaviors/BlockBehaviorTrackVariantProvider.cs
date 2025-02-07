using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace VintageRails.Behaviors;

public class BlockBehaviorTrackVariantProvider: BlockBehavior
{
    public string[] RaisedTypes { get; private set; } = Array.Empty<string>();
    public string[] CurvedTypes { get; private set; } = Array.Empty<string>();
    public string[] FlatTypes { get; private set; } = Array.Empty<string>();

    public virtual string CurrentType => block.LastCodePart();
    
    public BlockBehaviorTrackVariantProvider(Block block) : base(block) {}

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        
        RaisedTypes = properties["raised"].AsArray().Select(i => i.AsString()).ToArray();
        CurvedTypes = properties["curved"].AsArray().Select(i => i.AsString()).ToArray();
        FlatTypes = properties["flat"].AsArray().Select(i => i.AsString()).ToArray();
    }
}