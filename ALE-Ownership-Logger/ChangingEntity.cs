using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_Ownership_Logger {


    public class ChangingEntity {

        public long Owner { get; set; }
        public long Controller { get; set; }

        public string AdditionalInfo { get; set; }

        public Cause ChangingCause { get; set; }

        public enum Cause {
            Warhead,
            Character,
            CharacterTool,
            CharacterGun,
            ShipTool,
            Turret,
            Block,
            Grid,
        }
    }
}
