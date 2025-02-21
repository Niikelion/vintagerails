using System;
using System.Collections.Generic;
using System.Linq;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VintageRails.Behaviors.Entities;

public class EntityBehaviorOrderedPhysics : EntityBehavior {

    private List<IOrderedPhysicsTickBehavior> _tickers = new();
    
    public EntityBehaviorOrderedPhysics(Entity entity) : base(entity) {
    }

    public override string PropertyName() {
        return "vrails.physics_tick";
    }

    public override void AfterInitialized(bool onFirstSpawn) {
        base.AfterInitialized(onFirstSpawn);

        if (entity.World.Side != EnumAppSide.Server) {
            return;
        }
        
        var tickers = entity.GetInterfaces<IOrderedPhysicsTickBehavior>();
        var sorter = new TopoSorter<Type>();
        foreach (var ticker in tickers) {
            sorter.AddElement(ticker.GetType());
        }
        foreach (var ticker in tickers) {
            var tickerType = ticker.GetType();
            foreach (var type in ticker.After) { 
                sorter.AddSoftDependency(tickerType, type);
            }
            foreach (var type in ticker.Before) {
                sorter.AddSoftDependency(type, tickerType);
            }
        }

        _tickers = sorter.Sort().Select(type => tickers.First(ticker => ticker.GetType() == type)).ToList();
        
        VintageRailsModSystem.MinecartsBatch.Add(this);
    }

    public override void OnEntityDespawn(EntityDespawnData despawn) {
        if (entity.World.Side == EnumAppSide.Server) {
            VintageRailsModSystem.MinecartsBatch.Remove(this);   
        }
    }

    public void OnPhysicsTick(float dt) {
        foreach (var ticker in _tickers) {
            ticker.OnTick(dt);
        }
    }
    
    public void AfterPhysicsTick(float dt) {
        foreach (var ticker in _tickers) {
            ticker.AfterTick(dt);
        }
    }
    
}