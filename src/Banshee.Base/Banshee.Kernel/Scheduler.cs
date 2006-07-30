/***************************************************************************
 *  Scheduler.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
// TODO: Add Remove(Type), Remove(IJob) to scheduler
 
using System;
using System.Threading;

namespace Banshee.Kernel
{
    public static class Scheduler
    {
        private static object this_mutex = new object();
        private static IntervalHeap<IJob> heap = new IntervalHeap<IJob>();
        private static Thread job_thread;
        private static bool disposed;
        
        public static void Schedule(IJob job)
        {
            Schedule(job, JobPriority.Normal);
        }
        
        public static void Schedule(IJob job, JobPriority priority)
        {
            lock(this_mutex) {
                if(disposed) {
                    Debug("Job not scheduled; disposing scheduler");
                    return;
                }
                
                heap.Push(job, (int)priority);
                Debug("Job scheduled ({0}, {1})", job, priority);
                CheckRun();
            }
        }
        
        public static void Dispose()
        {
            lock(this_mutex) {
                disposed = true;
            }
        }
        
        private static void CheckRun()
        {
            if(heap.Count <= 0) {
                return;
            } else if(job_thread == null) {
                Debug("execution thread created");
                job_thread = new Thread(new ThreadStart(ProcessJobThread));
                job_thread.Priority = ThreadPriority.BelowNormal;
                job_thread.IsBackground = true;
                job_thread.Start();
            }   
        }
        
        private static void ProcessJobThread()
        {
            while(true) {
                IJob job = null;
                lock(this_mutex) {
                    if(disposed) {
                        Console.WriteLine("execution thread destroyed, dispose requested");
                        return;
                    }
                
                    try {
                        job = heap.Pop();
                    } catch(InvalidOperationException) {
                        Debug("execution thread destroyed, no more jobs scheduled");
                        job_thread = null;
                        return;
                    }
                }
                
                try {
                    Debug("Job started ({0})", job);
                    job.Run();
                    Debug("Job ended ({0})", job);
                } catch(Exception e) {
                    Debug("Job threw an unhandled exception: {0}", e);
                }
            }
        }
        
        private static bool print_debug = Banshee.Base.Globals.ArgumentQueue.Contains("debug");
        private static void Debug(string message, params object [] args)
        {
            if(print_debug) {
                Console.Error.WriteLine(String.Format("** Scheduler: {0}", message), args);
            }
        }
    }
}
