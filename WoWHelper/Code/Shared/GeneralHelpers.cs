using System;
using System.Threading.Tasks;

// Small general-purpose helpers with no WowPlayer state of their own.
public static class GeneralHelpers
{
    public static async Task<TState> ChangeStateBasedOnTaskResult<TState>(Task<bool> task, TState successState, TState failureState) where TState : Enum
    {
        bool taskResult = await task;
        return taskResult ? successState : failureState;
    }

    public static bool CurrentTimeInsideDuration(long startTime, long duration)
    {
        return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime) < duration;
    }
}
