using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

internal static class NativeOpenDeadline
{
    public const int DatabaseOpenInAnotherProcess = -16;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    // The budget limits when an attempt may start and when its result may be accepted.
    // A synchronous native call already in progress cannot be interrupted here.
    public static T Open<T>(
        Func<T> open,
        TimeSpan budget,
        TimeProvider clock,
        Action<TimeSpan> delay) where T : IDisposable
    {
        var started = clock.GetTimestamp();
        LatticeException? lastLockError = null;
        while (clock.GetElapsedTime(started) < budget)
        {
            T resource;
            try
            {
                resource = open();
            }
            catch (LatticeException exception) when (exception.NativeErrorCode == DatabaseOpenInAnotherProcess)
            {
                lastLockError = exception;
                var remaining = budget - clock.GetElapsedTime(started);
                if (remaining > TimeSpan.Zero)
                    delay(remaining < RetryDelay ? remaining : RetryDelay);
                continue;
            }

            if (clock.GetElapsedTime(started) < budget)
                return resource;

            resource.Dispose();
            break;
        }

        throw new TimeoutException(
            "The native database did not become openable before the deadline after process termination."
                + (lastLockError is null ? "" : $" Last native error: {lastLockError.Message}"),
            lastLockError);
    }
}
