using VintageRails.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks
{
    public class BlockBehaviorCartTrack : BlockBehavior
    {
        public float SpeedMultiplier { get; private set; }
        public float Friction { get; private set; } = 0.1f;
        public float ConstantAcceleration { get; private set; }

        public BlockFacing StartDir { get; private set; } = BlockFacing.NORTH;
        public BlockFacing EndDir { get; private set; } = BlockFacing.SOUTH;
        public bool Raised { get; private set; }

        public BlockFacing[] EndsDirections => endsDirections ??= new[] { StartDir, EndDir };
        
        private BlockFacing[]? endsDirections;

        public TrackAnchorData AnchorData { get; protected set; }
        
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
            Friction = properties["friction"].AsFloat();

            AnchorData = TrackAnchorData.OfDirections(StartDir, EndDir, Raised);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, blockPos, ref handling);
            //
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            base.OnBlockRemoved(world, pos, ref handling);
            //
        }
    }
}
