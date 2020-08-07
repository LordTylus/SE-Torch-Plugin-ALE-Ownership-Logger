using ALE_Ownership_Logger.Patch;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VRage.Game.ModAPI;

namespace ALE_Ownership_Logger
{
    public class OwnershipLoggerPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static OwnershipLoggerPlugin Instance { get; private set; }

        public Cache DamageCache { get; } = new Cache();
        
        private FieldInfo warheadExplodedField = null;

        private UserControl _control;
        public UserControl GetControl() => _control ?? (_control = new OwnershipControl(this));

        private Persistent<OwnershipConfig> _config;
        public OwnershipConfig Config => _config?.Data;

        public override void Init(ITorchBase torch) {

            base.Init(torch);

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            warheadExplodedField = typeof(MyWarhead).GetField("m_isExploded", BindingFlags.NonPublic | BindingFlags.Instance);

            if (warheadExplodedField == null)
                Log.Error("Unable to load Warhead.isExploded Field. If you are the developer of the plugin. This is the moment you have to fix that!");

            Instance = this;

            SetUpConfig();
            MyCubeGridPatch.ApplyLogging();
        }

        private void SetUpConfig() {

            var configFile = Path.Combine(StoragePath, "OwnershipLogger.cfg");

            try {

                _config = Persistent<OwnershipConfig>.Load(configFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<OwnershipConfig>(configFile, new OwnershipConfig());
                _config.Save();
            }
        }

        public void Save() {
            _config.Save();
            MyCubeGridPatch.ApplyLogging();
        }

        private void DamageCheck(object target, ref MyDamageInformation info) {

            try {

                if (!(target is MySlimBlock block))
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

                Cache DamageCache = Instance.DamageCache;

                if (cubeBlock is MyWarhead warhead && isExplosion) {

                    try {

                        bool exploded = (bool)warheadExplodedField.GetValue(warhead);

                        if (exploded) {

                            ChangingEntity changingEntity = new ChangingEntity {
                                Owner = warhead.OwnerId,
                                Controller = 0L,
                                ChangingCause = ChangingEntity.Cause.Warhead
                            };

                            MyCubeGrid grid = warhead.CubeGrid;

                            if (grid != null)
                                DamageCache.Store(grid.EntityId, changingEntity, TimeSpan.FromSeconds(3));
                        }

                    } catch (Exception e) {
                        Log.Error(e, "Warhead Detection failed!");
                    }

                } else {

                    ChangingEntity entity = GetAttacker(info.AttackerId);
                    
                    if (entity == null) {

                        if (info.IsDeformation) {

                            entity = new ChangingEntity {
                                Owner = 0L,
                                Controller = 0L,
                                ChangingCause = ChangingEntity.Cause.Deformation,
                            };
                        }

                        if (entity == null)
                            return;

                    } else {

                        if (entity.IsPlanet) 
                            entity.ChangingCause = ChangingEntity.Cause.Lightning;

                        if (cubeBlock is IMySafeZoneBlock safezone) {

                            var safezoneComponent = safezone.Components.Get<MySafeZoneComponent>();

                            bool enabled = false;
                            if (safezoneComponent != null)
                                enabled = safezoneComponent.IsSafeZoneEnabled();

                            entity.AdditionalInfo = enabled ? "on" : "off";
                        }

                        bool isGrid = entity.ChangingCause == ChangingEntity.Cause.Grid;

                        if (isGrid && isExplosion) {

                            ChangingEntity gridExplisonEntity = DamageCache.Get(info.AttackerId);

                            if (gridExplisonEntity != null) {

                                entity.Owner = gridExplisonEntity.Owner;
                                entity.Controller = gridExplisonEntity.Controller;
                                entity.ChangingCause = gridExplisonEntity.ChangingCause;
                            }
                        }
                    }

                    DamageCache.Store(cubeBlock.EntityId, entity, TimeSpan.FromSeconds(30));
                }

            } catch (Exception e) {
                Log.Error(e, "Error on Checking Damage!");
            }
        }

        public ChangingEntity GetAttacker(long attackerId) {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return null;

            if (entity is MyPlanet) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = 0L,
                    Controller = 0L,
                    IsPlanet = true
                };

                return changingEntity;
            }

            if (entity is MyCharacter character) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = character.GetPlayerIdentityId(),
                    Controller = 0L,
                    ChangingCause = ChangingEntity.Cause.Character
                };

                return changingEntity;
            }

            if (entity is IMyEngineerToolBase toolbase) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = toolbase.OwnerIdentityId,
                    Controller = 0L,
                    ChangingCause = ChangingEntity.Cause.CharacterTool
                };
                return changingEntity;
            }

            if (entity is MyLargeTurretBase turret) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = turret.OwnerId
                };

                if (turret.IsPlayerControlled)
                    changingEntity.Controller = turret.ControllerInfo.ControllingIdentityId;
                else
                    changingEntity.Controller = GetController(turret.CubeGrid);

                changingEntity.ChangingCause = ChangingEntity.Cause.Turret;

                return changingEntity;
            }

            if (entity is MyShipToolBase shipTool) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = shipTool.OwnerId,
                    Controller = GetController(shipTool.CubeGrid),
                    ChangingCause = ChangingEntity.Cause.ShipTool
                };
                return changingEntity;
            }

            if (entity is IMyGunBaseUser gunUser) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = gunUser.OwnerId,
                    Controller = 0L,
                    ChangingCause = ChangingEntity.Cause.CharacterGun
                };
                return changingEntity;
            }

            if (entity is MyCubeBlock block) {

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = block.OwnerId,
                    Controller = GetController(block.CubeGrid),
                    ChangingCause = ChangingEntity.Cause.Block
                };
                return changingEntity;
            }

            if (entity is MyCubeGrid grid) {

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                ChangingEntity changingEntity = new ChangingEntity {
                    Owner = gridOwner,
                    Controller = GetController(grid),
                    ChangingCause = ChangingEntity.Cause.Grid
                };

                return changingEntity;
            }

            return null;
        }

        private static long GetController(MyCubeGrid cubeGrid) {

            var controlSystem = cubeGrid.GridSystems.ControlSystem;

            if (controlSystem.IsControlled) {

                var controller = controlSystem.GetController();
                if (controller != null) {

                    MyPlayer player = controller.Player;
                    if (player != null)
                        return player.Identity.IdentityId;
                }
            }

            return 0L;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state) {
            switch (state) {
                case TorchSessionState.Loaded:
                    MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageCheck);
                    break;
            }
        }
    }
}
