using GAOS.Logger;
using UnityEditor;

namespace GAOS.DataStructure.Editor
{
    public class DataSystemEditorLogger : ILogSystem
    {
        public string LogPrefix => "[DataSystem.Editor]";
        public string LogPrefixColor => "#FFD600"; // Yellow-ish color for both editor and runtime
        public LogLevel DefaultLogLevel => LogLevel.Info;

        private static DataSystemEditorLogger _instance;
        public static DataSystemEditorLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataSystemEditorLogger();
                }
                return _instance;
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new DataSystemEditorLogger();
                GLog.RegisterSystem(_instance);
            }
        }
    }
} 