using System;
using System.Threading;

namespace Semaphores
{
    public enum Status
    {
        Working,
        Waiting,
        Created
    }

    public class SemaphoreThread : IDisposable
    {
        private static SemaphoreSlim _semaphore;
        private Timer _timer;
       
        public SemaphoreThread(int number, SemaphoreSlim semaphore)
        {
            Number = number;
            Counter = 0;
            Status = Status.Created;
            InnerThread = new Thread(new ThreadStart(DoWork)) { IsBackground = true };
            _semaphore = semaphore;
        }

        public event Action ThreadInfoChanged;

        public int Number { get; set; }
        public int Counter { get; set; }
        public Status Status { get; set; }
        public Thread InnerThread { get; set; }

        public string Info
        {
            get
            {
                switch (Status)
                {
                    case Status.Working:
                        return $"Thread {Number} -> {Counter}";
                    case Status.Waiting:
                        return $"Thread {Number} -> waiting";
                    case Status.Created:
                        return $"Thread {Number} -> created";
                    default:
                        return "Something goes wrong! Status not given.";
                }
            }
        }

        private void DoWork()
        {
            _semaphore.Wait();

            Status = Status.Working;
            ThreadInfoChanged?.Invoke();

            _timer = new Timer((e) =>
            {
                Counter++;   
                ThreadInfoChanged?.Invoke();

            }, null, 0, 1000);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
