using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace VintageRails.Entities;

public class EntitySeatInstSupplier : Entity, ISeatInstSupplier {
    
    public IMountableSeat CreateSeat(IMountable mountable, string seatId, SeatConfig config = null) {
        return new GenericSeat(mountable, seatId, config);
    }

    public class GenericSeat : EntityRideableSeat {

        public override AnimationMetaData? SuggestedAnimation => null;

        public GenericSeat(IMountable mountablesupplier, string seatId, SeatConfig config) : base(mountablesupplier, seatId, config) {
            
        }
        
    }
    
}