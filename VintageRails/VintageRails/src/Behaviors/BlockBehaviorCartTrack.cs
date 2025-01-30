using VintageRails.Rails;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors
{
    public class BlockBehaviorCartTrack : BlockBehavior
    {
        public float SpeedMultiplier { get; private set; }
        public float Friction { get; private set; } = 0.1f;
        public float ConstantAcceleration { get; private set; } = 0f;
        
        public BlockFacing StartDir { get; private set; }
        public BlockFacing EndDir { get; private set; }
        public bool Raised { get; private set; }

        public BlockBehaviorCartTrack(Block block) : base(block) {}

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            SpeedMultiplier = properties["speedMultiplier"].AsFloat(1);

            bool hasStartDir = properties["startDir"].Exists;
            bool hasEndDir = properties["endDir"].Exists;

            if (!hasStartDir || !hasEndDir || !properties["raised"].Exists)
            {
                string codePart = block.LastCodePart();
                string[] parts = codePart.Split("_");

                if (parts.Length == 2)
                {
                    Raised = parts[0] == "raised";
                    StartDir = BlockFacing.FromFirstLetter(parts[1][1]);
                    EndDir = BlockFacing.FromFirstLetter(parts[1][0]);
                }
            }
            Raised = properties["raised"].AsBool(Raised);
            StartDir = hasStartDir ? BlockFacing.FromFirstLetter(properties["startDir"].AsString()) : StartDir;
            EndDir = hasEndDir ? BlockFacing.FromFirstLetter(properties["endDir"].AsString()) : EndDir;
            
            ConstantAcceleration = properties["acceleration"].AsFloat();
        }

        public TrackAnchorData GetAnchorData() {
            return TrackAnchorData.OfDirections(StartDir, EndDir, Raised);
        }
        
    }
}
