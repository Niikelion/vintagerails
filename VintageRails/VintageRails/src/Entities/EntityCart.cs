
using VintageRails.src.Behaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace VintageRails.src.Entities
{
    internal class EntityCart : Entity
    {
        private double baseSpeed = 0.05;
        private string dropItem = "vintagerails:minecart";

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);

            if (properties.Attributes != null)
            {
                baseSpeed = properties.Attributes["baseSpeed"].AsDouble(baseSpeed);
                dropItem = properties.Attributes["dropItem"].AsString(dropItem);
            }
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            if (!Api.Side.IsServer())
                HandleMovementOnTrack();
        }

        private void HandleMovementOnTrack()
        {
            var currentTrack = World.BlockAccessor.GetBlock(SidedPos.XYZ.AsBlockPos);

            if (currentTrack == null)
                return;

            var trackBehavior = currentTrack.GetBehavior<BlockBehaviorCartTrack>();
            if (trackBehavior == null)
                return;

            string codePart = currentTrack.LastCodePart();

            string[] parts = codePart.Split("_");

            bool raised = parts[0] == "raised";
            string variant = parts[1];

            var firstDir = BlockFacing.FromFirstLetter(variant[0]);
            var secondDir = BlockFacing.FromFirstLetter(variant[1]);

            var targetBlockPos = SidedPos.XYZ.AsBlockPos;
            var targetPos = new Vec3d(targetBlockPos.X + 0.5, targetBlockPos.Y + 0.2, targetBlockPos.Z + 0.5);

            //

            SidedPos.SetPos(targetPos);
        }
    }
}
