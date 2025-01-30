using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors
{
    public class BlockBehaviorCartTrack : BlockBehavior
    {
        public float SpeedMultiplier { get; private set; }

        private BlockFacing startDir;
        private BlockFacing endDir;
        private bool raised;

        public BlockBehaviorCartTrack(Block block) : base(block) {}

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            SpeedMultiplier = properties["speedMultiplier"].AsFloat(1);

            bool hasStartDir = properties["startDir"].Exists;
            bool hasEndDir = properties["endDir"].Exists;

            if (!hasStartDir || !hasEndDir && !properties["raised"].Exists)
            {
                string codePart = block.LastCodePart();
                string[] parts = codePart.Split("_");

                if (parts.Length == 2)
                {
                    raised = parts[0] == "raised";
                    startDir = BlockFacing.FromFirstLetter(parts[2]);
                    endDir = BlockFacing.FromFirstLetter(parts[1]);
                }
            }

            raised = properties["raised"].AsBool(raised);
            startDir = hasStartDir ? BlockFacing.FromFirstLetter(properties["startDir"].AsString()) : startDir;
            endDir = hasEndDir ? BlockFacing.FromFirstLetter(properties["endDir"].AsString()) : endDir;
        }
    }
}
