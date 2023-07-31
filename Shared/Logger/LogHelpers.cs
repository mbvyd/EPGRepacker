using System;
using Serilog;

namespace Shared.Logger;

public static class LogHelpers
{
    public static void LogMessage(Exception exception, LogKind logKind)
    {
        switch (logKind)
        {
            case LogKind.Information:
                Log.Information("{0}", exception.Message);
                break;
            case LogKind.Warning:
                Log.Warning("{0}", exception.Message);
                break;
            case LogKind.Error:
                Log.Error("{0}", exception.Message);
                break;
            case LogKind.Fatal:
                Log.Fatal("{0}", exception.Message);
                break;
            default:
                break;
        }
    }

    public static void LogMessageIfException(Exception? exception, LogKind logKind)
    {
        if (exception != null)
        {
            LogMessage(exception, logKind);
        }
    }
}
