using System;
using System.Collections.Generic;
using System.Text;
using VintageRails.Behaviors.Callbacks;
using VintageRails.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace VintageRails.Behaviors;

public class CollectibleBehaviorCombustionCartEngine : CollectibleBehaviorCartEngineBase, IInfoAttachment {

    public const string CurrentFuelTimeAttribute = "fuel.time";
    public const string CurrentFuelTemperatureAttribute = "fuel.temperature";
    public const string CurrentTemperatureAttribute = "temperature";

    private float minimumTemperature = 100;
    private float markerTemperature = 1350;
    
    private float forceAtMinimum = 3;
    private float forceAtMarker = 9;
    private float backwardsForceMul = 0.75f;
    private float animationSpeedMul = 1;

    private WorldInteraction[]? _interactions = null;
    
    public CollectibleBehaviorCombustionCartEngine(CollectibleObject collObj) : base(collObj) {
        
    }

    public override void Initialize(JsonObject properties) {
        base.Initialize(properties);
        forceAtMinimum = properties["forceAtMin"].AsFloat();
        forceAtMarker = properties["forceAtMark"].AsFloat();
        minimumTemperature = properties["minimumTemperature"].AsFloat(100f);
        markerTemperature = properties["markerTemperature"].AsFloat(1350); // Coke burning temperature
        backwardsForceMul = properties["backwardsForceMul"].AsFloat(1f);
    }

    protected override bool IsWorking(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        return TemperatureRatio(engineAttributes) > 0;
    }

    protected override void AfterWork(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        
    }

    protected override float GetAnimationSpeed(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, bool isWorking, bool movesBackwards, double dt) {
        return TemperatureRatio(engineAttributes) * animationSpeedMul;
    }

    protected override void TickEngine(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, double dt) {
        var entity = rider.entity;
        var world = entity.World;
        var pos = entity.Pos.XYZ;
        
        TickFuel(world, pos, slot, engineAttributes, dt);
    }

    protected override double GetCurrentForce(ItemSlot slot, TrackRiderEntityBehavior rider, ITreeAttribute engineAttributes, bool movesBackwards, double dt) {
        return GameMath.Lerp(forceAtMinimum, forceAtMarker, TemperatureRatio(engineAttributes)) * (movesBackwards ? backwardsForceMul : 1f);
    }

    private static void TickFuel(IWorldAccessor world, Vec3d pos, ItemSlot slot, ITreeAttribute engineAttributes, double dt) {
        var burnTime = engineAttributes.GetDouble(CurrentFuelTimeAttribute);
        
        if(burnTime <= 0 && IsEnabled(engineAttributes)) {
            IgniteNextFuel(world, pos, slot, engineAttributes, dt);
            burnTime = engineAttributes.GetDouble(CurrentFuelTimeAttribute);
        }
        
        if (burnTime > 0) {
            burnTime -= dt;
        }
        
        engineAttributes.SetDouble(CurrentFuelTimeAttribute, burnTime);
        
        //20 is a default temperature for a campfire
        var targetTemperature = 20f;
        if (burnTime > 0) {
            targetTemperature = engineAttributes.GetFloat(CurrentFuelTemperatureAttribute);
        }

        TickTemperature(engineAttributes, targetTemperature, dt);
        slot.MarkDirty();
    }
    
    /// <summary>
    /// Does not mark dirty
    /// </summary>
    private static float TickTemperature(ITreeAttribute engineAttributes, float targetTemperature, double dt) {
        var temp = engineAttributes.GetFloat(CurrentTemperatureAttribute);
        temp = U.ChangeTemperature(temp, targetTemperature, (float)dt);
        engineAttributes.SetFloat(CurrentTemperatureAttribute, temp);
        return temp;
    }

    /// <summary>
    /// Does not mark dirty
    /// </summary>
    public static void IgniteNextFuel(IWorldAccessor world, Vec3d pos, ItemSlot slot, ITreeAttribute engineAttributes, double dt) {
        var stacks = U.ContainerHelper.GetNonEmptyContents(world, slot.Itemstack);
        for (int i=0; i<stacks.Length; i++) {
            var stack = stacks[i];
            var collectible = stack.Collectible;
            var combustible = collectible.CombustibleProps;

            if (combustible != null && combustible.BurnTemperature > 0 && combustible.BurnDuration > 0) {
                engineAttributes.SetDouble(CurrentFuelTimeAttribute, combustible.BurnDuration);
                engineAttributes.SetFloat(CurrentFuelTemperatureAttribute, combustible.BurnTemperature);
                if (--stack.StackSize == 0) {
                    stacks[i] = null;
                }
                break;
            }
            else {
                world.SpawnItemEntity(stack, pos);
                stacks[i] = null;
            }
        }
        U.ContainerHelper.SetContents(slot.Itemstack, stacks);
    }

    public float TemperatureRatio(ITreeAttribute engineAttributes) {
        return TemperatureRatio(engineAttributes.GetFloat(CurrentTemperatureAttribute));
    }
    
    public float TemperatureRatio(float temperature) {
        return MathF.Max((temperature - minimumTemperature) / (markerTemperature - minimumTemperature), 0f);
    }

    #region Attached Interactions
    public override bool OnTryDetach(ItemSlot slot, int slotIndex, Entity toEntity) {
        var world = toEntity.World;
        var pos = toEntity.SidedPos.XYZ.AsBlockPos;
        foreach (var content in U.ContainerHelper.GetNonEmptyContents(world, slot.Itemstack)) {
            world.SpawnItemEntity(content, pos);
        }
        slot.Itemstack.Attributes.RemoveAttribute("contents");
        return base.OnTryDetach(slot, slotIndex, toEntity); 
    }

    public override void OnInteract(ItemSlot thisItemSlot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave) {
        if (byEntity.World.Side == EnumAppSide.Server) {
            if (mode != EnumInteractMode.Interact || byEntity.Controls.CtrlKey || !byEntity.Controls.ShiftKey) {
                base.OnInteract(thisItemSlot, slotIndex, onEntity, byEntity, hitPosition, mode, ref handled, onRequireSave);
                return;
            }
            
            var activeSlot = byEntity.ActiveHandItemSlot;
            var handStack = activeSlot.Itemstack;
            if (handStack != null) {
                var thisStack = thisItemSlot.Itemstack;
                
                var content = U.ContainerHelper.GetNonEmptyContents(onEntity.World, thisStack);
                U.ContainerHelper.SetContents(thisStack, content.Append(handStack));
                thisItemSlot.MarkDirty();
                activeSlot.Itemstack = null;
                activeSlot.MarkDirty();
                handled = EnumHandling.PreventSubsequent;
            }
        }
        else {
            base.OnInteract(thisItemSlot, slotIndex, onEntity, byEntity, hitPosition, mode, ref handled, onRequireSave);
        }
    }
    #endregion

    public WorldInteraction[] GetInteractionHelps(ItemSlot thisSlot, Entity thisEntity) {
        return GetOrMakeWorldInteractions(thisEntity.World);
    }

    public void GetInfo(ItemSlot thisSlot, Entity thisEntity, StringBuilder infoText) {
        var temperature = thisSlot.Itemstack.Attributes.GetOrAddTreeAttribute(EngineTreeAttribute).GetFloat(CurrentTemperatureAttribute);
        infoText.AppendLine($"{(int)MathF.Round(temperature)}\u00b0C");
    }

    private WorldInteraction[] GetOrMakeWorldInteractions(IWorldAccessor world) {
        if (_interactions == null) {
            var fuels = new List<ItemStack>();
            foreach (var collectible in world.Collectibles) {
                var combustible = collectible.CombustibleProps;
                if (combustible != null && combustible.BurnDuration > 0 && combustible.BurnTemperature > 0) {
                    fuels.Add(new ItemStack(collectible));
                }
            }
            var interaction = new WorldInteraction();
            interaction.Itemstacks = fuels.ToArray();
            interaction.MouseButton = EnumMouseButton.Right;
            interaction.HotKeyCode = "shift";
            interaction.ActionLangCode = "vintagerails:combustion-engine-add-fuel";
            _interactions = new[] {
                interaction
            };
        }
        return _interactions;
    }
    
}