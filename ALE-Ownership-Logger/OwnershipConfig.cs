using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace ALE_Ownership_Logger {
    
    public class OwnershipConfig : ViewModel {

        private string _loggingFileName = "ownerships-${shortdate}.log";

        public string LoggingFileName { get => _loggingFileName; set => SetValue(ref _loggingFileName, value); }
    }
}
