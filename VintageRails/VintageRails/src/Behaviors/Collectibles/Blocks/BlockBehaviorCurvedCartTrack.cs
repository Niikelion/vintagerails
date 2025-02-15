using VintageRails.Behaviors.Entities;
using VintageRails.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VintageRails.Behaviors.Collectibles.Blocks;

public class BlockBehaviorCurvedCartTrack : BlockBehaviorCartTrack {
    
    public BlockBehaviorCurvedCartTrack(Block block) : base(block) {
    }

    public override TrackAnchorData? GetAnchorDataForEntrySide(EntityBehaviorTrackRider rider, BlockPos pos, Vec3i? entrySide) {
        var anchors = base.GetAnchorDataForEntrySide(rider, pos, entrySide);
        if (anchors != null) {
            return anchors;
        }

        if (entrySide!.Y != 0) {
            return null;
        }

        return TrackAnchorData.GetForward(entrySide);
    }
}