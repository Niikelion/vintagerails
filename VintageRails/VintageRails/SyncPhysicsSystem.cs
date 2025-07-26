using VintageRails.Global;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageRails;

public class SyncPhysicsSystem : ModSystem {

    // private long listenerId;
    public PhysicsBatch Behaviors { get; } = new();

    public override void StartServerSide(ICoreServerAPI api) {
        base.StartServerSide(api);
        api.World.RegisterGameTickListener(HandleTick, 1000 / 15, 1000);
    }

    public void HandleTick(float deltaTime) {
        Behaviors.OnPhysicsTick(deltaTime);
    }

}