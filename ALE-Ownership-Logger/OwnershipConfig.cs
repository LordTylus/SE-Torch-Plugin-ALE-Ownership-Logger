using Torch;

namespace ALE_Ownership_Logger {
    
    public class OwnershipConfig : ViewModel {

        private string _loggingFileName = "ownerships-${shortdate}.log";

        public string LoggingFileName { get => _loggingFileName; set => SetValue(ref _loggingFileName, value); }
    }
}
