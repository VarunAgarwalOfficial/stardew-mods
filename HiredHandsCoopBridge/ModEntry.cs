using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace HiredHandsCoopBridge;

/// <summary>
/// Makes Hired Hands 1.2.1 safe to use on a co-op host by disabling its
/// unsynchronized custom NPC simulation. Hired Hands' existing host-only task,
/// economy, and save logic remains responsible for all gameplay mutations.
/// </summary>
public sealed class ModEntry : Mod
{
    private const string HiredHandsModId = "dreams897.HirelingSystem";
    private Harmony? Harmony { get; set; }

    public override void Entry(IModHelper helper)
    {
        if (!helper.ModRegistry.IsLoaded(HiredHandsModId))
            throw new InvalidOperationException("Hired Hands must be loaded before its co-op bridge.");

        Assembly? hiredHandsAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == "HiredHands");
        if (hiredHandsAssembly is null)
            throw new InvalidOperationException("Couldn't find the loaded Hired Hands assembly.");

        Type? managerType = hiredHandsAssembly.GetType("HirelingSystem.Hirelings.HirelingManager");
        Type? schedulerType = hiredHandsAssembly.GetType("HirelingSystem.Scheduling.LaborScheduler");
        MethodInfo? spawnMethod = managerType is null ? null : AccessTools.Method(managerType, "TrySpawn");
        MethodInfo? visibleSimulationGetter = schedulerType is null
            ? null
            : AccessTools.PropertyGetter(schedulerType, "VisibleSim");

        if (spawnMethod is null || visibleSimulationGetter is null)
        {
            throw new InvalidOperationException(
                "Hired Hands internals don't match version 1.2.1. Remove this bridge until it is updated."
            );
        }

        this.Harmony = new Harmony(this.ModManifest.UniqueID);
        this.Harmony.Patch(
            spawnMethod,
            prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeWorkerSpawn))
        );
        this.Harmony.Patch(
            visibleSimulationGetter,
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterVisibleSimulationCheck))
        );

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Monitor.Log(
            "Hired Hands co-op bridge is ready. Multiplayer work will be host-authoritative and processed in the background.",
            LogLevel.Info
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMultiplayer)
            return;

        string message = Context.IsMainPlayer
            ? "Hired Hands co-op mode: press L to manage background workers."
            : "Hired Hands co-op mode: workers are managed by the host.";

        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
        this.Monitor.Log(message, LogLevel.Info);
    }

    /// <summary>Skip creation of custom worker NPCs only in multiplayer.</summary>
    private static bool BeforeWorkerSpawn()
    {
        return !Context.IsMultiplayer;
    }

    /// <summary>
    /// Tell Hired Hands to use its background task path in multiplayer. Its own
    /// main-player checks ensure only the host changes the farm, money, or save data.
    /// </summary>
    private static void AfterVisibleSimulationCheck(ref bool __result)
    {
        if (Context.IsMultiplayer)
            __result = false;
    }
}
