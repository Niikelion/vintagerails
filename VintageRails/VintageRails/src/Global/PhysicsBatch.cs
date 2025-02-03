using System.Collections.Concurrent;
using System.Collections.Generic;
using VintageRails.Behaviors;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace VintageRails.Global;

public class PhysicsBatch : IPhysicsTickable {

    private volatile int _flag;

    private readonly ConcurrentQueue<(EntityBehaviorOrderedPhysics ticker, bool addition)> _events = new();
    // private readonly ConcurrentBag<EntityBehaviorOrderedPhysics> _toRemove = new();
    
    private readonly HashSet<EntityBehaviorOrderedPhysics> _tickers = new();
    
    public void OnPhysicsTick(float dt) {
        HandleAdditionAndRemovals();
        
        foreach (var ticker in _tickers) {
            ticker.OnPhysicsTick(dt);
        }
        foreach (var ticker in _tickers) {
            ticker.AfterPhysicsTick(dt);
        }
    }

    public void AfterPhysicsTick(float dt) {
        //Nothing
    }

    public bool CanProceedOnThisThread() {
        return AsyncHelper.CanProceedOnThisThread(ref _flag);
    }

    public void Add(EntityBehaviorOrderedPhysics ticker) {
        _events.Enqueue((ticker, true));
    }

    public void Remove(EntityBehaviorOrderedPhysics ticker) {
        _events.Enqueue((ticker, false));
    }
    
    public void OnPhysicsTickDone() => _flag = 0;
    
    private void HandleAdditionAndRemovals() {
        while (_events.TryDequeue(out var evnt)) {
            if (evnt.addition) {
                _tickers.Add(evnt.ticker);
            }
            else {
                _tickers.Remove(evnt.ticker);
            }
        }
    }
    
    public bool Ticking { get; set; }
}