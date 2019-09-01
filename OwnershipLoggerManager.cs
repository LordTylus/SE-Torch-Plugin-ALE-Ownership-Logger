using ALE_Ownership_Logger.Patch;
using ALE_Ownership_Logger.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Reflection;
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

        private FieldInfo warheadExplodedField = null;

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

            warheadExplodedField = typeof(MyWarhead).GetField("m_isExploded", BindingFlags.NonPublic | BindingFlags.Instance);

            if(warheadExplodedField == null)
                Log.Error("Unable to load Warhead.isExploded Field. If you are the developer of the plugin. This is the moment you have to fix that!");
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

            try {

                MySlimBlock block = target as MySlimBlock;

                if (block == null)
                    return;

                MyCubeBlock cubeBlock = block.FatBlock;

                if (cubeBlock == null)
                    return;

                if (cubeBlock as MyTerminalBlock == null)
                    return;

                if (cubeBlock.EntityId == 0L)
                    return;

                var infoType = info.Type;
                bool isExplosion = infoType != null && infoType.String == "Explosion";

                Cache DamageCache = OwnershipLoggerPlugin.Instance.DamageCache;

                MyWarhead warhead = cubeBlock as MyWarhead;
                if (warhead != null && isExplosion) {

                    try {

                        bool exploded = (bool)warheadExplodedField.GetValue(warhead);

                        if (exploded) {

                            ChangingEntity changingEntity = new ChangingEntity();
                            changingEntity.Owner = warhead.OwnerId;
                            changingEntity.Controller = 0L;
                            changingEntity.ChangingCause = ChangingEntity.Cause.Warhead;

                            MyCubeGrid grid = warhead.CubeGrid;

                            if(grid != null)
                                DamageCache.Store(grid.EntityId, changingEntity, TimeSpan.FromSeconds(3));
                        }

                    } catch (Exception e) {
                        Log.Error(e, "Warhead Detection failed!");
                    }

                } else {

                    ChangingEntity entity = getAttacker(info.AttackerId);
                    if (entity == null)
                        return;

                    bool isGrid = entity.ChangingCause == ChangingEntity.Cause.Grid;

                    if (isGrid && isExplosion) {

                        ChangingEntity gridExplisonEntity = DamageCache.Get(info.AttackerId);

                        if (gridExplisonEntity != null) {

                            entity.Owner = gridExplisonEntity.Owner;
                            entity.Controller = gridExplisonEntity.Controller;
                            entity.ChangingCause = gridExplisonEntity.ChangingCause;
                        }
                    }

                    DamageCache.Store(cubeBlock.EntityId, entity, TimeSpan.FromSeconds(30));
                }

            } catch(Exception e) {
                Log.Error(e, "Error on Checking Damage!");
            }
        }

        public ChangingEntity getAttacker(long attackerId) {

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
