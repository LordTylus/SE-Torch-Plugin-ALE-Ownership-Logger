using ALE_Ownership_Logger.Utils;
using NLog;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;

namespace ALE_Ownership_Logger.Patch {
    public class MyCubeGridPatch {

        public static readonly Logger Log = LogManager.GetLogger("OwnershipLogger");

        [ReflectedMethodInfo(typeof(MyCubeGrid), "OnChangeOwnersRequest")]
        private static readonly MethodInfo OnChangeOwnersRequest;
        [ReflectedMethodInfo(typeof(MyCubeGrid), "ChangeOwnerRequest")]
        private static readonly MethodInfo ChangeOwnerRequest;

        internal static readonly MethodInfo patchOnChangeOwnersRequest =
            typeof(MyCubeGridPatch).GetMethod(nameof(PatchOnChangeOwnersRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo patchChangeOwnerRequest =
            typeof(MyCubeGridPatch).GetMethod(nameof(PatchChangeOwnerRequest), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx) {

            ReflectedManager.Process(typeof(MyCubeGridPatch));

            try {

                ctx.GetPattern(OnChangeOwnersRequest).Prefixes.Add(patchOnChangeOwnersRequest);
                ctx.GetPattern(ChangeOwnerRequest).Prefixes.Add(patchChangeOwnerRequest);

                LogManager.GetCurrentClassLogger().Info("Patched MyCubeGrid!");

            } catch (Exception e) {

                LogManager.GetCurrentClassLogger().Error("Unable to patch MyCubeGrid", e);
            }
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

            long causeId = OwnershipLoggerPlugin.Instance.DamageCache.Get(block.EntityId);
            if (playerId == 0L && causeId != 0L) {

                string causePlayerName = PlayerUtils.GetPlayerNameById(causeId);
                string causeFactionTag = PlayerUtils.GetFactionTagStringForPlayer(causeId);

                causeName = causePlayerName + " " + causeFactionTag;

            } else if(playerId != 0L) {

                causeName = newName;
            }

            Log.Info(causeName.PadRight(20) + " changed owner on block " +block.BlockDefinition.BlockPairName.PadRight(20) + " from " + oldName.PadRight(25) + " to " + newName.PadRight(20) + " on grid: " + gridName);

            return true;
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
