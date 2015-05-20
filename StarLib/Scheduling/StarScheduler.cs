// SharpStar. A Starbound wrapper.
// Copyright (C) 2015 Mitchell Kutchuk
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarLib.Scheduling
{
    public class StarScheduler : IScheduler, IDisposable
    {
        private long _running;

        private readonly object _jobLocker = new object();

        private readonly List<SchedulerJob> _jobs;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        private CancellationTokenSource _cts;

        public List<SchedulerJob> Jobs
        {
            get
            {
                lock (_jobLocker)
                {
                    return _jobs.ToList();
                }
            }
        }

        public bool Running
        {
            get
            {
                return Interlocked.Read(ref _running) == 1;
            }
        }

        public StarScheduler()
        {
            _running = 0;
            _jobs = new List<SchedulerJob>();
            _cts = new CancellationTokenSource();
        }

        public virtual void Start()
        {
            if (Running)
                throw new Exception("The scheduler is already running!");

            Interlocked.CompareExchange(ref _running, 1, 0);

            Run();
        }

        public virtual void Stop()
        {
            if (!Running)
                throw new Exception("The scheduler is not running!");

            Interlocked.CompareExchange(ref _running, 0, 1);

            lock (_jobLocker)
            {
                _cts.Cancel();
                _jobs.Clear();
            }
        }

        protected virtual void Run()
        {
            Task.Run(async () =>
            {
                while (Running)
                {
                    try
                    {
                        IEnumerable<SchedulerJob> jobs;
                        lock (_jobLocker)
                        {
                            jobs = _jobs.Where(p => p.NextExecuteDate >= DateTime.UtcNow);
                        }

                        foreach (SchedulerJob job in jobs)
                        {
                            job.Execute();

                            if (job.ScheduleType == ScheduleType.Once)
                            {
                                lock (_jobLocker)
                                {
                                    _jobs.Remove(job);
                                }
                            }
                        }

                        SchedulerJob sJob = null;
                        lock (_jobLocker)
                        {
                            _jobs.Sort();

                            if (_jobs.Count > 0)
                            {
                                sJob = _jobs.First();
                            }

                            _cts = new CancellationTokenSource();
                        }

                        var tasks = new List<Task> { _semaphore.WaitAsync(_cts.Token) };

                        if (sJob != null)
                        {
                            TimeSpan ts = (sJob.NextExecuteDate - DateTime.UtcNow).Duration();

                            tasks.Add(Task.Delay(ts, _cts.Token));
                        }

                        await Task.WhenAny(tasks);

                        _cts.Cancel();
                    }
                    catch
                    {
                    }
                }
            });
        }

        public virtual void ScheduleAsync(TimeSpan ts, ISchedulerJob toExecute, bool recurring)
        {
            lock (_jobLocker)
            {
                _jobs.Add(new SchedulerJob(toExecute, ts, recurring ? ScheduleType.Recurring : ScheduleType.Once, true));

                if (_semaphore.CurrentCount > 0)
                    _semaphore.Release();
            }
        }

        public virtual void ScheduleAsync(DateTime time, ISchedulerJob toExecute)
        {
            lock (_jobLocker)
            {
                _jobs.Add(new SchedulerJob(toExecute, time.ToUniversalTime(), ScheduleType.Once, true));

                if (_semaphore.CurrentCount > 0)
                    _semaphore.Release();
            }
        }

        public void Dispose()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore.Dispose();
            }

            _semaphore = null;
        }
    }
}
