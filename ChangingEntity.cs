﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALE_Ownership_Logger {


    public class ChangingEntity {

        public long Owner { get; set; }
        public long Controller { get; set; }

        public Cause ChangingCause { get; set; }

        public enum Cause {
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
