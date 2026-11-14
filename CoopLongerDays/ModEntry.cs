using System.Globalization;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CoopLongerDays;

public sealed class ModEntry : Mod
{
    private const int ProtocolVersion = 1;
    private const int VanillaMillisecondsPerGameMinute = 700;
    private const double MinimumMultiplier = 1.0;
    private const double MaximumMultiplier = 6.0;
    private const string SettingsMessage = "ClockSettings";
    private const string RequestMessage = "RequestClockSettings";

    private static readonly IReadOnlyDictionary<string, string> CoopProfile =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Annosz.UiInfoSuite2"] = "2.3.7",
            ["dreams897.HirelingSystem"] = "1.2.1",
            ["Esca.FarmTypeManager"] = "1.26.1",
            ["FlashShifter.SVE-FTM"] = "1.15.11",
            ["FlashShifter.SVECode"] = "1.15.11",
            ["FlashShifter.StardewValleyExpandedCP"] = "1.15.11",
            ["LocalCoop.CoopLongerDays"] = "1.2.1",
            ["LocalCoop.HiredHandsCoopBridge"] = "1.0.0",
            ["Pathoschild.Automate"] = "2.6.1",
            ["Pathoschild.ChestsAnywhere"] = "1.30.1",
            ["Pathoschild.ContentPatcher"] = "2.9.1",
            ["Pathoschild.LookupAnything"] = "1.55.0",
            ["SMAPI.ConsoleCommands"] = "4.5.2",
            ["SMAPI.SaveBackup"] = "4.5.2"
        };

    private ModConfig Config { get; set; } = new();

    private int ActiveMillisecondsPerGameMinute { get; set; } = VanillaMillisecondsPerGameMinute;

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        this.NormalizeAndSaveConfig();
        this.ApplyClock(ToMilliseconds(this.Config.DayLengthMultiplier));

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Multiplayer.PeerContextReceived += this.OnPeerContextReceived;
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;

        helper.ConsoleCommands.Add(
            "coop_days",
            "Show or set the day-length multiplier. Usage: coop_days [1-6]. Only the host can change it during multiplayer.",
            this.OnCommand
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            this.ApplyHostConfig();
            this.BroadcastSettings();
            return;
        }

        // A farmhand never controls the world clock. Ask for the host's authoritative
        // setting; the host's reply replaces this client's local setting.
        this.RequestSettingsFromHost();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // Reapply at the start of each day in case the game or another component reset it.
        this.ApplyClock(this.ActiveMillisecondsPerGameMinute);

        if (Context.IsMainPlayer)
            this.BroadcastSettings();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // Stop using the previous host's setting before another save is selected.
        this.ApplyClock(ToMilliseconds(this.Config.DayLengthMultiplier));
    }

    private void OnPeerContextReceived(object? sender, PeerContextReceivedEventArgs e)
    {
        this.ValidatePeerProfile(e.Peer);

        bool hasThisMod = e.Peer.HasSmapi && e.Peer.GetMod(this.ModManifest.UniqueID) is not null;

        if (Context.IsMainPlayer)
        {
            if (!hasThisMod)
            {
                this.Monitor.Log(
                    $"Player {e.Peer.PlayerID} does not have {this.ModManifest.Name}. They can still join, but everyone should install the same version for a consistent clock.",
                    LogLevel.Warn
                );
                return;
            }

            this.SendSettings(e.Peer.PlayerID);
            return;
        }

        if (!e.Peer.IsHost)
            return;

        if (!hasThisMod)
        {
            this.Monitor.Log(
                $"The host does not have {this.ModManifest.Name}; using vanilla day length on this client.",
                LogLevel.Warn
            );
            this.ApplyClock(VanillaMillisecondsPerGameMinute);
            return;
        }

        this.RequestSettingsFromHost(e.Peer.PlayerID);
    }

    private void ValidatePeerProfile(IMultiplayerPeer peer)
    {
        var problems = new List<string>();

        if (!peer.HasSmapi)
        {
            problems.Add("SMAPI is not installed");
        }
        else
        {
            if (!string.Equals(peer.GameVersion.ToString(), "1.6.15", StringComparison.OrdinalIgnoreCase))
                problems.Add($"Stardew {peer.GameVersion} (expected 1.6.15)");

            if (!string.Equals(peer.ApiVersion.ToString(), "4.5.2", StringComparison.OrdinalIgnoreCase))
                problems.Add($"SMAPI {peer.ApiVersion} (expected 4.5.2)");

            foreach ((string modId, string expectedVersion) in CoopProfile)
            {
                IMultiplayerPeerMod? installed = peer.GetMod(modId);
                if (installed is null)
                {
                    problems.Add($"missing {modId} {expectedVersion}");
                    continue;
                }

                if (!string.Equals(installed.Version.ToString(), expectedVersion, StringComparison.OrdinalIgnoreCase))
                    problems.Add($"{modId} {installed.Version} (expected {expectedVersion})");
            }
        }

        string player = peer.IsHost ? "host" : $"player {peer.PlayerID}";
        if (problems.Count == 0)
        {
            this.Monitor.Log($"Co-op mod profile matches for {player}.", LogLevel.Info);
            return;
        }

        this.Monitor.Log(
            $"Co-op mod profile mismatch for {player}:\n- {string.Join("\n- ", problems)}",
            LogLevel.Error
        );

        if (Context.IsWorldReady)
        {
            Game1.addHUDMessage(new HUDMessage(
                $"Mod mismatch for {player}; check the SMAPI console before playing.",
                HUDMessage.error_type
            ));
        }
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID)
            return;

        if (e.Type == RequestMessage)
        {
            if (Context.IsMainPlayer)
                this.SendSettings(e.FromPlayerID);
            return;
        }

        if (e.Type != SettingsMessage || Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        if (e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
        {
            this.Monitor.Log("Ignored clock settings from a non-host player.", LogLevel.Warn);
            return;
        }

        ClockSettings settings = e.ReadAs<ClockSettings>();
        if (settings.ProtocolVersion != ProtocolVersion)
        {
            this.Monitor.Log(
                $"The host uses an incompatible {this.ModManifest.Name} protocol version ({settings.ProtocolVersion}).",
                LogLevel.Error
            );
            return;
        }

        int milliseconds = Math.Clamp(
            settings.MillisecondsPerGameMinute,
            ToMilliseconds(MinimumMultiplier),
            ToMilliseconds(MaximumMultiplier)
        );
        this.ApplyClock(milliseconds);
        this.Monitor.Log($"Applied the host's {FormatMultiplier(milliseconds)}x day length.", LogLevel.Info);
    }

    private void OnCommand(string command, string[] args)
    {
        if (args.Length == 0)
        {
            this.Monitor.Log(
                $"Current day length: {FormatMultiplier(this.ActiveMillisecondsPerGameMinute)}x ({this.ActiveMillisecondsPerGameMinute * 10 / 1000.0:0.###} real seconds per 10 in-game minutes).",
                LogLevel.Info
            );
            return;
        }

        if (args.Length != 1
            || !double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double multiplier)
            || !double.IsFinite(multiplier)
            || multiplier < MinimumMultiplier
            || multiplier > MaximumMultiplier)
        {
            this.Monitor.Log("Usage: coop_days [1-6]. Example: coop_days 2", LogLevel.Error);
            return;
        }

        if (Context.IsWorldReady && Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            this.Monitor.Log("Only the host can change the day length in multiplayer.", LogLevel.Error);
            return;
        }

        this.Config.DayLengthMultiplier = multiplier;
        this.Helper.WriteConfig(this.Config);

        if (!Context.IsWorldReady)
        {
            this.Monitor.Log($"Saved {multiplier:0.###}x day length. It will apply when a save is loaded.", LogLevel.Info);
            return;
        }

        this.ApplyHostConfig();
        this.BroadcastSettings();
    }

    private void NormalizeAndSaveConfig()
    {
        double normalized = this.Config.DayLengthMultiplier;
        if (!double.IsFinite(normalized))
            normalized = 2.0;

        normalized = Math.Clamp(normalized, MinimumMultiplier, MaximumMultiplier);
        if (Math.Abs(normalized - this.Config.DayLengthMultiplier) < 0.0001)
            return;

        this.Config.DayLengthMultiplier = normalized;
        this.Helper.WriteConfig(this.Config);
    }

    private void ApplyHostConfig()
    {
        int milliseconds = ToMilliseconds(this.Config.DayLengthMultiplier);
        this.ApplyClock(milliseconds);
        this.Monitor.Log(
            $"Host clock set to {this.Config.DayLengthMultiplier:0.###}x day length ({milliseconds * 10 / 1000.0:0.###} real seconds per 10 in-game minutes).",
            LogLevel.Info
        );
    }

    private void ApplyClock(int millisecondsPerGameMinute)
    {
        this.ActiveMillisecondsPerGameMinute = millisecondsPerGameMinute;
        Game1.realMilliSecondsPerGameMinute = millisecondsPerGameMinute;
        Game1.realMilliSecondsPerGameTenMinutes = millisecondsPerGameMinute * 10;
    }

    private void BroadcastSettings()
    {
        if (Context.IsMultiplayer && Context.IsMainPlayer)
            this.SendSettings();
    }

    private void SendSettings(long? playerId = null)
    {
        var settings = new ClockSettings
        {
            ProtocolVersion = ProtocolVersion,
            MillisecondsPerGameMinute = this.ActiveMillisecondsPerGameMinute
        };

        this.Helper.Multiplayer.SendMessage(
            settings,
            SettingsMessage,
            modIDs: new[] { this.ModManifest.UniqueID },
            playerIDs: playerId is null ? null : new[] { playerId.Value }
        );
    }

    private void RequestSettingsFromHost(long? hostPlayerId = null)
    {
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
            return;

        long hostId = hostPlayerId ?? Game1.MasterPlayer.UniqueMultiplayerID;
        this.Helper.Multiplayer.SendMessage(
            new ClockSettings { ProtocolVersion = ProtocolVersion },
            RequestMessage,
            modIDs: new[] { this.ModManifest.UniqueID },
            playerIDs: new[] { hostId }
        );
    }

    private static int ToMilliseconds(double multiplier)
    {
        double clamped = Math.Clamp(multiplier, MinimumMultiplier, MaximumMultiplier);
        return (int)Math.Round(VanillaMillisecondsPerGameMinute * clamped, MidpointRounding.AwayFromZero);
    }

    private static string FormatMultiplier(int millisecondsPerGameMinute)
    {
        return (millisecondsPerGameMinute / (double)VanillaMillisecondsPerGameMinute)
            .ToString("0.###", CultureInfo.InvariantCulture);
    }
}
