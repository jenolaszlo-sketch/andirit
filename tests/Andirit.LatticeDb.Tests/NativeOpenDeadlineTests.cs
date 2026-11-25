using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

public sealed class NativeOpenDeadlineTests
{
    [Fact]
    public void ImmediateSuccessTransfersOwnership()
    {
        var clock = new ControlledClock();
        var resource = new OpenResource();
        Assert.Same(resource, Run(clock, () => resource, 200));
        Assert.Empty(clock.Delays);
        Assert.Equal(0, resource.DisposeCount);
        resource.Dispose();
    }

    [Fact]
    public void NativeLockFailureRetriesAndAcceptsTimelySuccess()
    {
        var clock = new ControlledClock();
        var native = CaptureLockError();
        using var resource = new OpenResource();
        var attempts = 0;
        Assert.Same(resource, Run(clock, () => ++attempts == 1 ? throw native : resource, 250));
        Assert.Equal(2, attempts);
        Assert.Equal([100], clock.Delays);
    }

    [Fact]
    public void PersistentNativeLockStopsWithOriginalDiagnostic()
    {
        var clock = new ControlledClock();
        var native = CaptureLockError();
        var attempts = 0;
        var failure = Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            attempts++;
            throw native;
        }, 250));
        Assert.Same(native, failure.InnerException);
        Assert.Contains(native.Message, failure.Message, StringComparison.Ordinal);
        Assert.Equal(3, attempts);
        Assert.Equal([100, 100, 50], clock.Delays);
    }

    [Fact]
    public void NonRetryableNativeErrorPropagatesUnchanged()
    {
        var clock = new ControlledClock();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lattice");
        var native = Assert.Throws<LatticeException>(() => LatticeDatabase.Open(path, new() { Create = false }));
        Assert.NotEqual(NativeOpenDeadline.DatabaseOpenInAnotherProcess, native.NativeErrorCode);
        Assert.Same(native, Assert.Throws<LatticeException>(() => Run(clock, () => throw native, 200)));
        Assert.Empty(clock.Delays);
    }

    [Fact]
    public void UnrelatedManagedErrorPropagatesUnchanged()
    {
        var clock = new ControlledClock();
        var error = new InvalidOperationException("injected managed error");
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => Run(clock, () => throw error, 200)));
        Assert.Empty(clock.Delays);
    }

    [Fact]
    public void LateSuccessAfterNativeLockFailurePreservesDiagnostic()
    {
        var clock = new ControlledClock();
        var native = CaptureLockError();
        var resource = new OpenResource();
        var attempts = 0;
        var failure = Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            if (++attempts == 1)
                throw native;
            clock.Advance(101);
            return resource;
        }, 200));
        Assert.Same(native, failure.InnerException);
        Assert.Equal(2, attempts);
        Assert.Equal(1, resource.DisposeCount);
    }

    [Fact]
    public void ExactDeadlineRejectsBothAttemptAndCompletion()
    {
        var clock = new ControlledClock();
        var resource = new OpenResource();
        var attempts = 0;
        Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            attempts++;
            clock.Advance(100);
            return resource;
        }, 100));
        Assert.Equal(1, resource.DisposeCount);
        Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            attempts++;
            return resource;
        }, 0));
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void RetryDelayIsClampedToTheRemainingBudget()
    {
        var clock = new ControlledClock();
        var native = CaptureLockError();
        var attempts = 0;
        Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            attempts++;
            clock.Advance(10);
            throw native;
        }, 25));
        Assert.Equal([15], clock.Delays);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void LateFirstSuccessDisposesTheResourceExactlyOnce()
    {
        var clock = new ControlledClock();
        var resource = new OpenResource();
        var error = Assert.Throws<TimeoutException>(() => Run(clock, () =>
        {
            clock.Advance(26);
            return resource;
        }, 25));
        Assert.Null(error.InnerException);
        Assert.Equal(1, resource.DisposeCount);
    }

    private static OpenResource Run(ControlledClock clock, Func<OpenResource> open, int milliseconds) =>
        NativeOpenDeadline.Open(open, TimeSpan.FromMilliseconds(milliseconds), clock, clock.Delay);

    private static LatticeException CaptureLockError()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lattice");
        try
        {
            using var owner = new LatticeCodeGraphStore(path);
            var error = Assert.Throws<LatticeException>(() => LatticeDatabase.Open(path, new() { Create = false }));
            Assert.Equal(NativeOpenDeadline.DatabaseOpenInAnotherProcess, error.NativeErrorCode);
            return error;
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class ControlledClock : TimeProvider
    {
        private long timestamp;
        public List<int> Delays { get; } = [];
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => timestamp;
        public void Advance(long milliseconds) => timestamp += milliseconds;

        public void Delay(TimeSpan duration)
        {
            var milliseconds = (int)duration.TotalMilliseconds;
            Delays.Add(milliseconds);
            Advance(milliseconds);
        }
    }

    private sealed class OpenResource : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }
}
