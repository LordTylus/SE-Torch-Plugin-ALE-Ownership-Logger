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

                Log.Info("Patched MyCubeGrid!");

            } catch (Exception e) {

                Log.Error("Unable to patch MyCubeGrid", e);
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

            Log.Info("Ownership change for block "+block.BlockDefinition.BlockPairName+" from "+oldOwnerName+" to "+newOwnerName+" on grid: "+gridName);

            return true;
        }

        public static bool PatchOnChangeOwnersRequest(List<MyCubeGrid.MySingleOwnershipRequest> requests, long requestingPlayer) {

            StringBuilder sb = new StringBuilder();

            /* No Requests nothing to do */
            if (requests == null)
                return true;

            string resquesterName = PlayerUtils.GetPlayerNameById(requestingPlayer);

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

                /* Opening statement */
                if (first) {

                    sb.AppendLine("Player " + resquesterName + " requested the following ownership changes on grid: " + gridName);

                    first = false;
                }

                sb.AppendLine("   block " + block.BlockDefinition.BlockPairName + " from " + oldOwnerName + " to " + newOwnerName);
            }

            Log.Info(sb);
            
            return true;
        }
    }
}
