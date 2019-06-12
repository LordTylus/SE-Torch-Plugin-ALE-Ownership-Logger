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

            ChangingEntity entity = getAttacker(info.AttackerId);
            if (entity == null)
                return;

            OwnershipLoggerPlugin.Instance.DamageCache.Store(cubeBlock.EntityId, entity, TimeSpan.FromSeconds(30));
        }

        public static ChangingEntity getAttacker(long attackerId) {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return null;

            MyCharacter character = entity as MyCharacter;
            if(character != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = character.GetPlayerIdentityId();
                changingEntity.Controller = 0L;
                changingEntity.ChangingCause = ChangingEntity.Cause.Character;
                return changingEntity;
            }

            IMyEngineerToolBase toolbase = entity as IMyEngineerToolBase;
            if (toolbase != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = toolbase.OwnerIdentityId;
                changingEntity.Controller = 0L;
                changingEntity.ChangingCause = ChangingEntity.Cause.CharacterTool;
                return changingEntity;
            }

            MyLargeTurretBase turret = entity as MyLargeTurretBase;
            if (turret != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = turret.OwnerId;

                if (turret.IsPlayerControlled)
                    changingEntity.Controller = turret.ControllerInfo.ControllingIdentityId;
                else
                    changingEntity.Controller = getController(turret.CubeGrid);

                changingEntity.ChangingCause = ChangingEntity.Cause.Turret;

                return changingEntity;
            }

            MyShipToolBase shipTool = entity as MyShipToolBase;
            if (shipTool != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = shipTool.OwnerId;
                changingEntity.Controller = getController(shipTool.CubeGrid);
                changingEntity.ChangingCause = ChangingEntity.Cause.ShipTool;
                return changingEntity;
            }

            IMyGunBaseUser gunUser = entity as IMyGunBaseUser;
            if (gunUser != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = gunUser.OwnerId;
                changingEntity.Controller = 0L;
                changingEntity.ChangingCause = ChangingEntity.Cause.CharacterGun;
                return changingEntity;
            }

            MyCubeBlock block = entity as MyCubeBlock;
            if (block != null) {

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = block.OwnerId;
                changingEntity.Controller = getController(block.CubeGrid);
                changingEntity.ChangingCause = ChangingEntity.Cause.Block;
                return changingEntity;
            }

            MyCubeGrid grid = entity as MyCubeGrid;
            if (grid != null) {

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                ChangingEntity changingEntity = new ChangingEntity();
                changingEntity.Owner = gridOwner;
                changingEntity.Controller = getController(grid);
                changingEntity.ChangingCause = ChangingEntity.Cause.Grid;
                return changingEntity;
            }
                
            return null;
        }

        private static long getController(MyCubeGrid cubeGrid) {

            var controlSystem = cubeGrid.GridSystems.ControlSystem;

            if (controlSystem.IsControlled) {

                var controller = controlSystem.GetController();
                if(controller != null) {

                    MyPlayer player = controller.Player;
                    if (player != null)
                        return player.Identity.IdentityId;
                }
            }

            return 0L;
        }
    }
}
