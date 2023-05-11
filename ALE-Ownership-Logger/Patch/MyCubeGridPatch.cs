using ALE_Core.Utils;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;
using VRage.Network;

namespace ALE_Ownership_Logger.Patch {

    [PatchShim]
    public static class MyCubeGridPatch {

        private static readonly Logger Log = LogManager.GetLogger("OwnershipLogger");

        [ReflectedMethodInfo(typeof(MyCubeGrid), "OnChangeOwnersRequest")]
        private static readonly MethodInfo OnChangeOwnersRequest;
        [ReflectedMethodInfo(typeof(MyCubeGrid), "ChangeOwnerRequest")]
        private static readonly MethodInfo ChangeOwnerRequest;
        [ReflectedMethodInfo(typeof(MyCubeBlock), "OnDestroy")]
        private static readonly MethodInfo DestroyRequest;

        internal static readonly MethodInfo patchOnChangeOwnersRequest =
            typeof(MyCubeGridPatch).GetMethod(nameof(PatchOnChangeOwnersRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patchChangeOwnerRequest =
            typeof(MyCubeGridPatch).GetMethod(nameof(PatchChangeOwnerRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patchOnDestroyRequest =
            typeof(MyCubeGridPatch).GetMethod(nameof(PatchOnDestroyRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void ApplyLogging() {

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--) {

                var rule = rules[i];

                if (rule.LoggerNamePattern == "OwnershipLogger")
                    rules.RemoveAt(i);
            }

            var config = OwnershipLoggerPlugin.Instance.Config;

            var logTarget = new FileTarget {
                FileName = "Logs/"+config.LoggingFileName,
                Layout = "${var:logStamp} ${var:logContent}"
            };

            var logRule = new LoggingRule("OwnershipLogger", LogLevel.Debug, logTarget) {
                Final = true
            };

            rules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }

        public static void Patch(PatchContext ctx) {

            ctx.GetPattern(OnChangeOwnersRequest).Prefixes.Add(patchOnChangeOwnersRequest);
            ctx.GetPattern(ChangeOwnerRequest).Prefixes.Add(patchChangeOwnerRequest);
            ctx.GetPattern(DestroyRequest).Prefixes.Add(patchOnDestroyRequest);

            LogManager.GetCurrentClassLogger().Debug("Patched MyCubeGrid!");
        }

        public static void PatchOnDestroyRequest(MyCubeBlock __instance) {

            MyCubeBlock block = __instance;

            if (block as MyTerminalBlock == null)
                return;

            MyCubeGrid grid = block.CubeGrid;

            string gridName = grid.DisplayName;

            string oldOwnerName = PlayerUtils.GetPlayerNameById(block.OwnerId);

            bool isOnline = PlayerUtils.isOnline(block.OwnerId);
            string onlineString = "[Off]";
            if (isOnline)
                onlineString = "[On]";

            string oldFactionTag = FactionUtils.GetFactionTagStringForPlayer(block.OwnerId);
            string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
            string causeName = "[Unknown]";

            ChangingEntity cause = OwnershipLoggerPlugin.Instance.DamageCache.Get(block.EntityId);
            string additionalInfo = null;

            if (cause != null) {

                additionalInfo = cause.AdditionalInfo;

                if (!cause.IsPlanet) {

                    long causeId;

                    if (cause.Controller != 0L)
                        causeId = cause.Controller;
                    else
                        causeId = cause.Owner;

                    /* Can be offline when weapons are the cause */
                    bool isCauseOnline = PlayerUtils.isOnline(causeId);
                    string causeOnlineString = "[Off]";
                    if (isCauseOnline)
                        causeOnlineString = "[On]";

                    string causePlayerName = PlayerUtils.GetPlayerNameById(causeId);
                    string causeFactionTag = FactionUtils.GetFactionTagStringForPlayer(causeId);

                    causeName = (causePlayerName + " " + causeOnlineString + causeFactionTag).PadRight(25) + " with " + cause.ChangingCause;
                
                } else {

                    causeName = "Planet".PadRight(25) + " with " + cause.ChangingCause;
                }
            }

            string blockpairName = block.BlockDefinition.BlockPairName;

            blockpairName = ChangeName(additionalInfo, blockpairName);

            string location = GetLocationWhenNeeded(block);

            Log.Info(causeName.PadRight(45) + " destroyed block        " + blockpairName.PadRight(20) + " from " + oldName.PadRight(25) + "    " + "".PadRight(20) + " of grid: " + gridName + location);
        }

        public static bool PatchChangeOwnerRequest(
              MyCubeGrid grid,
              MyCubeBlock block,
              long playerId,
              MyOwnershipShareModeEnum shareMode) {

            string gridName = grid.DisplayName;

            string oldOwnerName = PlayerUtils.GetPlayerNameById(block.OwnerId);
            string newOwnerName = PlayerUtils.GetPlayerNameById(playerId);

            /* Only shared mode was changed */
            if (oldOwnerName == newOwnerName)
                return true;

            bool isOnline = PlayerUtils.isOnline(block.OwnerId);
            string onlineString = "[Off]";
            if (isOnline)
                onlineString = "[On]";

            string oldFactionTag = FactionUtils.GetFactionTagStringForPlayer(block.OwnerId);
            string newFactionTag = FactionUtils.GetFactionTagStringForPlayer(playerId);

            string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
            string newName = newOwnerName + " " + newFactionTag;

            string causeName = "[Unknown]";

            ChangingEntity cause = OwnershipLoggerPlugin.Instance.DamageCache.Get(block.EntityId);

            string additionalInfo = null;

            if (playerId == 0L && cause != null) {

                additionalInfo = cause.AdditionalInfo;

                if(!cause.IsPlanet) {

                    long causeId;

                    if (cause.Controller != 0L)
                        causeId = cause.Controller;
                    else
                        causeId = cause.Owner;

                    /* Can be offline when weapons are the cause */
                    bool isCauseOnline = PlayerUtils.isOnline(causeId);
                    string causeOnlineString = "[Off]";
                    if (isCauseOnline)
                        causeOnlineString = "[On]";

                    string causePlayerName = PlayerUtils.GetPlayerNameById(causeId);
                    string causeFactionTag = FactionUtils.GetFactionTagStringForPlayer(causeId);

                    causeName = (causePlayerName + " " + causeOnlineString + causeFactionTag).PadRight(25) + " with " + cause.ChangingCause;

                } else {

                    causeName = "Planet".PadRight(25) + " with " + cause.ChangingCause;
                }

            } else if(playerId != 0L) {

                /* Can be offline when weapons are the cause */
                bool isCauseOnline = PlayerUtils.isOnline(playerId);
                string causeOnlineString = "[Off]";
                if (isCauseOnline)
                    causeOnlineString = "[On]";

                /* Must be Online then */
                causeName = newOwnerName + " " + causeOnlineString + newFactionTag;
            }

            string blockpairName = block.BlockDefinition.BlockPairName;

            blockpairName = ChangeName(additionalInfo, blockpairName);

            string location = GetLocationWhenNeeded(block);

            Log.Info(causeName.PadRight(45) + " changed owner on block " + blockpairName.PadRight(20) + " from " + oldName.PadRight(25) + " to " + newName.PadRight(20) + " on grid: " + gridName + location);

            return true;
        }

        private static string ChangeName(string additionalInfo, string blockpairName) {

            if (additionalInfo == null)
                return blockpairName;
            
            return blockpairName+"_"+ additionalInfo;
        }

        public static bool PatchOnChangeOwnersRequest(
            MyOwnershipShareModeEnum shareMode,
            List<MyCubeGrid.MySingleOwnershipRequest> requests) {

            StringBuilder sb = new StringBuilder();

            /* No Requests nothing to do */
            if (requests == null)
                return true;

            ulong senderSteamId = MyEventContext.Current.Sender.Value;

            long requestingPlayer = MySession.Static.Players.TryGetIdentityId(senderSteamId);

            string resquesterName;
            string requestFactionTag;
            
            if(requestingPlayer != 0) {
            
                resquesterName = PlayerUtils.GetPlayerNameById(requestingPlayer);
                requestFactionTag = FactionUtils.GetFactionTagStringForPlayer(requestingPlayer);
            
            } else {

                resquesterName = "Unknown Player (ID: "+ senderSteamId+")";
                requestFactionTag = "";
            }
            
            /* Dont want to print the grid information over and over again. */
            bool first = true;

            foreach (MyCubeGrid.MySingleOwnershipRequest request in requests) {

                MyCubeBlock block = MyEntities.GetEntityById(request.BlockId, false) as MyCubeBlock;

                /* No block found, or different ownership? */
                if (block == null)
                    continue;

                /* No Grid? unlikely but ignore */
                MyCubeGrid grid = block.CubeGrid;
                if (grid == null)
                    continue;

                string gridName = grid.DisplayName;

                string oldOwnerName = PlayerUtils.GetPlayerNameById(block.OwnerId);
                string newOwnerName = PlayerUtils.GetPlayerNameById(request.Owner);

                if (oldOwnerName == newOwnerName)
                    return true;

                bool isOnline = PlayerUtils.isOnline(block.OwnerId);
                string onlineString = "[Off]";
                if (isOnline)
                    onlineString = "[On]";

                string oldFactionTag = FactionUtils.GetFactionTagStringForPlayer(block.OwnerId);
                string newFactionTag = FactionUtils.GetFactionTagStringForPlayer(request.Owner);

                string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
                string newName = newOwnerName + " " + newFactionTag;

                string location = GetLocationWhenNeeded(block);

                /* Opening statement */
                if (first) {

                    sb.AppendLine("Player " + resquesterName + " " + requestFactionTag + " requested the following ownership changes on grid: '" + gridName+ "'");

                    first = false;
                }

                sb.AppendLine("   block " + block.BlockDefinition.BlockPairName.PadRight(20) + " from " + oldName.PadRight(25) + " to " + newName.PadRight(20) + location);
            }

            Log.Info(sb);
            
            return true;
        }

        private static string GetLocationWhenNeeded(MyCubeBlock block) {

            var config = OwnershipLoggerPlugin.Instance.Config;

            if (!config.LogCoords)
                return "";

            var location = block.PositionComp.GetPosition();

            return $" Location: {location.X.ToString("#,##0.0")}, {location.Y.ToString("#,##0.0")}, {location.Z.ToString("#,##0.0")}";
        }
    }
}
