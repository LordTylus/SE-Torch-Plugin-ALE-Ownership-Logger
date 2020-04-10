using ALE_Ownership_Logger.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;

namespace ALE_Ownership_Logger.Patch {

    [PatchShim]
    public static class MyCubeGridPatch {

        public static readonly Logger Log = LogManager.GetLogger("OwnershipLogger");

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

            string oldFactionTag = PlayerUtils.GetFactionTagStringForPlayer(block.OwnerId);
            string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
            string causeName = "[Unknown]";

            ChangingEntity cause = OwnershipLoggerPlugin.Instance.DamageCache.Get(block.EntityId);
            string additionalInfo = null;

            if (cause != null) {

                additionalInfo = cause.AdditionalInfo;

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
                string causeFactionTag = PlayerUtils.GetFactionTagStringForPlayer(causeId);

                causeName = (causePlayerName + " " + causeOnlineString + causeFactionTag).PadRight(25) + " with " + cause.ChangingCause;
            }

            string blockpairName = block.BlockDefinition.BlockPairName;

            blockpairName = ChangeName(additionalInfo, blockpairName);

            Log.Info(causeName.PadRight(45) + " destroyed block        " + blockpairName.PadRight(20) + " from " + oldName.PadRight(25) + "    " + "".PadRight(20) + " of grid: " + gridName);
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

            string oldFactionTag = PlayerUtils.GetFactionTagStringForPlayer(block.OwnerId);
            string newFactionTag = PlayerUtils.GetFactionTagStringForPlayer(playerId);

            string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
            string newName = newOwnerName + " " + newFactionTag;

            string causeName = "[Unknown]";

            ChangingEntity cause = OwnershipLoggerPlugin.Instance.DamageCache.Get(block.EntityId);

            string additionalInfo = null;

            if (playerId == 0L && cause != null) {

                additionalInfo = cause.AdditionalInfo;

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
                string causeFactionTag = PlayerUtils.GetFactionTagStringForPlayer(causeId);

                causeName = (causePlayerName + " " + causeOnlineString + causeFactionTag).PadRight(25) + " with " + cause.ChangingCause;

            } else if(playerId != 0L) {

                /* Must be Online then */
                causeName = newOwnerName + " [On]" + newFactionTag;
            }

            string blockpairName = block.BlockDefinition.BlockPairName;

            blockpairName = ChangeName(additionalInfo, blockpairName);

            Log.Info(causeName.PadRight(45) + " changed owner on block " + blockpairName.PadRight(20) + " from " + oldName.PadRight(25) + " to " + newName.PadRight(20) + " on grid: " + gridName);

            return true;
        }

        private static string ChangeName(string additionalInfo, string blockpairName) {

            if (additionalInfo == null)
                return blockpairName;
            
            return blockpairName+"_"+ additionalInfo;
        }

        public static bool PatchOnChangeOwnersRequest(List<MyCubeGrid.MySingleOwnershipRequest> requests, long requestingPlayer) {

            StringBuilder sb = new StringBuilder();

            /* No Requests nothing to do */
            if (requests == null)
                return true;

            string resquesterName = PlayerUtils.GetPlayerNameById(requestingPlayer);
            string requestFactionTag = PlayerUtils.GetFactionTagStringForPlayer(requestingPlayer);

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

                string oldFactionTag = PlayerUtils.GetFactionTagStringForPlayer(block.OwnerId);
                string newFactionTag = PlayerUtils.GetFactionTagStringForPlayer(request.Owner);

                string oldName = oldOwnerName + " " + onlineString + oldFactionTag;
                string newName = newOwnerName + " " + newFactionTag;

                /* Opening statement */
                if (first) {

                    sb.AppendLine("Player " + resquesterName + " " + requestFactionTag + " requested the following ownership changes on grid: '" + gridName+ "'");

                    first = false;
                }

                sb.AppendLine("   block " + block.BlockDefinition.BlockPairName.PadRight(20) + " from " + oldName.PadRight(25) + " to " + newName.PadRight(20));
            }

            Log.Info(sb);
            
            return true;
        }
    }
}
