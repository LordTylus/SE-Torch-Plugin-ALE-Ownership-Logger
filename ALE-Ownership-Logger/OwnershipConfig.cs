using Torch;

namespace ALE_Ownership_Logger {
    
    public class OwnershipConfig : ViewModel {

        private string _loggingFileName = "ownerships-${shortdate}.log";
        private bool _logCoords = false;

        public string LoggingFileName { get => _loggingFileName; set => SetValue(ref _loggingFileName, value); }

        public bool LogCoords { get => _logCoords; set => SetValue(ref _logCoords, value); }
    }
}
