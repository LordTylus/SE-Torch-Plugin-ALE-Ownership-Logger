using NLog;
using Torch;
using Torch.API;

namespace ALE_Ownership_Logger
{
    public class OwnershipLoggerPlugin : TorchPluginBase {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static OwnershipLoggerPlugin Instance { get; private set; }

        public Cache DamageCache { get; } = new Cache();

        public override void Init(ITorchBase torch) {

            base.Init(torch);

            Instance = this;

            var pgmr = new OwnershipLoggerManager(torch);
            torch.Managers.AddManager(pgmr);
        }
    }
}
