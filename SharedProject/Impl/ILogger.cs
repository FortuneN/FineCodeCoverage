using System.Collections.Generic;

public interface ILogger
{
    void Log(IEnumerable<object> message);
    void Log(IEnumerable<string> message);
    void Log(params object[] message);
    void Log(params string[] message);
    void LogWithoutTitle(IEnumerable<object> message);
    void LogWithoutTitle(IEnumerable<string> message);
    void LogWithoutTitle(params object[] message);
    void LogWithoutTitle(params string[] message);
}