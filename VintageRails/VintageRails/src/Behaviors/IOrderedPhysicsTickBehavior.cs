using System;
using System.Collections.Generic;

namespace VintageRails.Behaviors;

public interface IOrderedPhysicsTickBehavior {

    IEnumerable<Type> After => Array.Empty<Type>();
    IEnumerable<Type> Before => Array.Empty<Type>();

    public void OnTick(double dt) { }

    public void AfterTick(double dt) { }
    

}