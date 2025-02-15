using System;
using System.Collections.Generic;
using System.Linq;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Behaviors.Entities;
using VintageRails.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks
{
    public class BlockBehaviorCartTrack : BlockBehavior
    {
        public BlockFacing StartDir { get; private set; } = BlockFacing.NORTH;
        public BlockFacing EndDir { get; private set; } = BlockFacing.SOUTH;
        public bool Raised { get; private set; }

        public BlockFacing[] EndsDirections => endsDirections ??= new[] { StartDir, EndDir };
        
        private BlockFacing[]? endsDirections;

        private List<TrackBehavior> _trackBehaviors = new();
        
        public TrackAnchorData AnchorData { get; protected set; }
        
        public BlockBehaviorCartTrack(Block block) : base(block) {}

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            
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

            AnchorData = TrackAnchorData.OfDirections(StartDir, EndDir, Raised);

            foreach (var behaviorJson in properties["trackBehaviors"].AsArray() ?? Enumerable.Empty<JsonObject>()) {
                var behavior = VintageRailsModSystem.TrackBehaviors.FromJson(behaviorJson);

                if (behavior == null) {
                    continue;
                }
                
                var minSpeed = behaviorJson["minSpeed"].AsDouble(0);
                var maxSpeed = behaviorJson["maxSpeed"].AsDouble(double.PositiveInfinity);

                _trackBehaviors.Add(new TrackBehavior {
                    Behavior = behavior,
                    MaxSpeed = maxSpeed,
                    MinSpeed = minSpeed
                });
            }
        }

        public virtual IEnumerable<ITrackBehavior> GetValidTrackBehaviors(EntityBehaviorTrackRider rider) {
            return _trackBehaviors.Where(behavior => Math.Abs(rider.Speed) <= behavior.MaxSpeed &&  Math.Abs(rider.Speed) >= behavior.MinSpeed).Select(behavior => behavior.Behavior);
        }

        public virtual void OnCartEntered(EntityBehaviorTrackRider rider, BlockPos currentPos, BlockPos previousPos) {
            foreach (var behavior in GetValidTrackBehaviors(rider)) {
                behavior.OnCartEntered(rider, currentPos, previousPos);
            }
        }
        
        public virtual void OnCartExited(EntityBehaviorTrackRider rider, BlockPos currentPos, BlockPos nextPos) {
            foreach (var behavior in GetValidTrackBehaviors(rider)) {
                behavior.OnCartExited(rider, currentPos, nextPos);
            }
        }
        
        public virtual void OnCartTick(EntityBehaviorTrackRider rider, BlockPos pos, double dt) {
            foreach (var behavior in GetValidTrackBehaviors(rider)) {
                behavior.OnTickCart(rider, pos, dt);
            }
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

        private struct TrackBehavior {
            public double MinSpeed { get; init; }
            public double MaxSpeed { get; init; }
            public required ITrackBehavior Behavior { get; init; }
        }
    }
}
