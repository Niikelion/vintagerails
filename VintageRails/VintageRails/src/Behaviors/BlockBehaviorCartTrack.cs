using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace VintageRails.src.Behaviors
{
    public class BlockBehaviorCartTrack : BlockBehavior
    {
        private float speedMultiplier;

        public BlockBehaviorCartTrack(Block block) : base(block) {}

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            speedMultiplier = properties["speedMultiplier"].AsFloat(1);
        }

        public float GetSpeedMultiplier() => speedMultiplier;
    }
}
