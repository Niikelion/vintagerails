using System.Collections.Concurrent;
using System.Collections.Generic;
using VintageRails.Behaviors;
using VintageRails.Behaviors.Entities;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;

namespace VintageRails.Global;

public class PhysicsBatch {

    // private readonly ConcurrentQueue<(EntityBehaviorOrderedPhysics ticker, bool addition)> _events = new();
    // // private readonly ConcurrentBag<EntityBehaviorOrderedPhysics> _toRemove = new();
    
    private readonly HashSet<EntityBehaviorOrderedPhysics> _tickers = new();
    
    public void OnPhysicsTick(float dt) {
        // HandleAdditionAndRemovals();
        
        foreach (var ticker in _tickers) {
            ticker.OnPhysicsTick(dt);
        }
        foreach (var ticker in _tickers) {
            ticker.AfterPhysicsTick(dt);
        }
    }
    
    public void Add(EntityBehaviorOrderedPhysics ticker) {
        // _events.Enqueue((ticker, true));
        _tickers.Add(ticker);
    }

    public void Remove(EntityBehaviorOrderedPhysics ticker) {
        // _events.Enqueue((ticker, false));
        _tickers.Remove(ticker);
    }

    public void Clear() {
        _tickers.Clear();
    }
    
    // private void HandleAdditionAndRemovals() {
    //     while (_events.TryDequeue(out var evnt)) {
    //         if (evnt.addition) {
    //             _tickers.Add(evnt.ticker);
    //         }
    //         else {
    //             _tickers.Remove(evnt.ticker);
    //         }
    //     }
    // }
}