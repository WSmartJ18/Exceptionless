﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using System.Diagnostics;
using CodeSmith.Core.Scheduler;
using Exceptionless.Core.Caching;
using NLog.Fluent;

namespace Exceptionless.Core.Jobs {
    public class CacheJobLockProvider : JobLockProvider {
        private readonly ICacheClient _cacheClient;

        public CacheJobLockProvider(ICacheClient cacheClient) {
            _cacheClient = cacheClient;
        }

        public override JobLock Acquire(string lockName) {
            int result = 0;

            try {
                var lockValue = _cacheClient.Get<object>("joblock:" + lockName);
                if (lockValue != null)
                    return new JobLock(this, lockName, false);

                _cacheClient.Set("joblock:" + lockName, DateTime.Now, TimeSpan.FromMinutes(20));

                result = 1;
            } catch (Exception e) {
                Log.Error().Exception(e).Message("Error attempting to acquire job lock '{0}' on {1}.", lockName, Environment.MachineName).Report().Write();
            }

            if (Settings.Current.LogJobLocks) {
                string processName = "{Unknown}";
                int processId = 0;
                try {
                    Process p = Process.GetCurrentProcess();
                    processId = p.Id;
                    processName = p.ProcessName;
                } catch {}
                Log.Debug().Message(result == 1
                    ? "Acquired job lock '{0}' on {1}; process {2}:{3}"
                    : "Could not acquire job lock '{0}' on {1}; process {2}:{3}",
                    lockName, Environment.MachineName, processName, processId).Write();
            }

            return new JobLock(this, lockName, result == 1);
        }

        public override void Release(JobLock jobLock) {
            if (!jobLock.LockAcquired) {
                if (Settings.Current.LogJobLocks) {
                    string processName = "{Unknown}";
                    int processId = 0;
                    try {
                        Process p = Process.GetCurrentProcess();
                        processId = p.Id;
                        processName = p.ProcessName;
                    } catch {}
                    Log.Debug().Message("Tried to release job lock '{0}' that wasn't acquired on {1}; process {2}:{3}",
                        jobLock.LockName, Environment.MachineName, processName, processId).Write();
                }

                return;
            }

            try {
                _cacheClient.Remove("joblock:" + jobLock.LockName);
            } catch (Exception e) {
                Log.Error().Message("Error attempting to release job lock '{0}' on {1}.", jobLock.LockName, Environment.MachineName).Exception(e).Report().Write();
            }

            if (Settings.Current.LogJobLocks) {
                string processName = "{Unknown}";
                int processId = 0;
                try {
                    Process p = Process.GetCurrentProcess();
                    processId = p.Id;
                    processName = p.ProcessName;
                } catch {}
                Log.Debug().Message("Released job lock '{0}' on {1}; process {2}:{3}",
                    jobLock.LockName, Environment.MachineName, processName, processId).Write();
            }

            jobLock.SetReleased();
        }
    }
}