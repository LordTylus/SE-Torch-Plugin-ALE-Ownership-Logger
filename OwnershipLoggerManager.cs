using ALE_Ownership_Logger.Patch;
using ALE_Ownership_Logger.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Threading.Tasks;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.PatchManager;
using Torch.Session;
using VRage.Game.ModAPI;

namespace ALE_Ownership_Logger {

    public class OwnershipLoggerManager : Manager {

        [Dependency]
        private TorchSessionManager sessionManager;

        [Dependency]
        private readonly PatchManager patchManager;
        private PatchContext ctx;

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public OwnershipLoggerManager(ITorchBase torchInstance)
            : base(torchInstance) {
        }

        /// <inheritdoc />
        public override void Attach() {

            base.Attach();

            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            if (ctx == null)
                ctx = patchManager.AcquireContext();
        }

        /// <inheritdoc />
        public override void Detach() {
            base.Detach();

            patchManager.FreeContext(ctx);

            Log.Info("Detached!");
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state) {
            switch (state) {
                case TorchSessionState.Loaded:

                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageCheck);

                    Task.Delay(3000).ContinueWith((t) => {
                        MyCubeGridPatch.Patch(ctx);
                        patchManager.Commit();
                    });

                    break;
            }
        }

        private void DamageCheck(object target, ref MyDamageInformation info) {

            MySlimBlock block = target as MySlimBlock;

            if (block == null)
                return;

            MyCubeBlock cubeBlock = block.FatBlock;

            if (cubeBlock == null)
                return;

            if (cubeBlock.OwnerId == 0L)
                return;

            if (cubeBlock.EntityId == 0L)
                return;

            long playerId = getAttacker(info.AttackerId);
            if (playerId == 0L)
                return;

            OwnershipLoggerPlugin.Instance.DamageCache.Store(cubeBlock.EntityId, playerId, TimeSpan.FromSeconds(30));
        }

        public static long getAttacker(long attackerId) {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return 0L;

            MyCharacter character = entity as MyCharacter;
            if(character != null) 
                return character.GetPlayerIdentityId();

            MyEngineerToolBase toolbase = entity as MyEngineerToolBase;
            if (toolbase != null) 
                return toolbase.OwnerIdentityId;

            MyCubeBlock block = entity as MyCubeBlock;
            if (block != null)
                return block.OwnerId;

            IMyGunBaseUser gunUser = entity as IMyGunBaseUser;
            if (gunUser != null)
                return gunUser.OwnerId;

            MyCubeGrid grid = entity as MyCubeGrid;
            if (grid != null) {

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                return gridOwner;
            }
                
            return 0L;
        }
    }
}
