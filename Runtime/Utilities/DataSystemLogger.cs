using GAOS.Logger;
using UnityEngine;

namespace GAOS.DataStructure
{
    public class DataSystemLogger : ILogSystem
    {
        public string LogPrefix => "[DataSystem]";
        public string LogPrefixColor => "#FFD600"; // Yellow-ish color for both editor and runtime 
        public LogLevel DefaultLogLevel => LogLevel.Info;

        private static DataSystemLogger _instance;
        public static DataSystemLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataSystemLogger();
                    GLog.RegisterSystem(_instance);
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterLogger()
        {
            var _ = Instance;
        }
    }
} 