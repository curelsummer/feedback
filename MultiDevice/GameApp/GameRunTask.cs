using System;

namespace MultiDevice
{
    public enum GameEndCondition
    {
        GameEndSignal,
        ProcessExit,
        Timeout
    }

    public enum GameBdfMode
    {
        SingleFileWithMarkers,
        PerTaskFile
    }

    public class GameRunTask
    {
        public string ExePath { get; set; }
        public string ProcessName { get; set; }
        public string Args { get; set; }
        public string GameCode { get; set; }
        public TimeSpan MinDuration { get; set; }
        public GameEndCondition EndCondition { get; set; }
        public TimeSpan Timeout { get; set; }
        public GameBdfMode BdfMode { get; set; }
        public string MarkerName { get; set; }

        public override string ToString()
        {
            return $"{ProcessName} {Args}";
        }
    }
}

