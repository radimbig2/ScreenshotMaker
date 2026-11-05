namespace ScreenshotMaker.Services
{
    internal sealed class SingleInstance : IDisposable
    {
        private const string MutexName = @"Local\ScreenshotMaker.SingleInstance";
        private const string ActivateEventName = @"Local\ScreenshotMaker.Activate";
        private readonly Mutex _mutex;
        private readonly EventWaitHandle _activateEvent;
        private RegisteredWaitHandle? _registration;

        private SingleInstance(Mutex mutex)
        {
            _mutex = mutex;
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        }

        public static SingleInstance? TryAcquire()
        {
            var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);

            if (createdNew)
            {
                return new(mutex);
            }

            mutex.Dispose();
            return null;
        }

        public static void ActivateRunningInstance()
        {
            if (EventWaitHandle.TryOpenExisting(ActivateEventName, out var handle))
            {
                using (handle)
                {
                    handle.Set();
                }
            }
        }

        public void ListenForActivation(Action onActivate)
        {
            _registration = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                (_, _) => onActivate(),
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }

        public void Dispose()
        {
            _registration?.Unregister(null);
            _activateEvent.Dispose();
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
