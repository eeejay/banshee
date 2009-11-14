/***************************************************************************
 *  TaskScheduler.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
 *  THE SOFTWare IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWare OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

// Does not work, must be completed!!!!!!!!!!!!!!
/*
using System;
using System.Threading;
using System.Collections.Generic;

using C5;

namespace Migo.TaskCore
{
    class ScheduledCommandWrapperComparer : IComparer<ScheduledCommandWrapper>
    {
        public int Compare (ScheduledCommandWrapper left,
                            ScheduledCommandWrapper right)
        {
            return DateTime.Compare (left.ScheduledFor, right.ScheduledFor);
        }
    }

    public class ScheduledCommandWrapper : CommandWrapper
    {
        private readonly DateTime scheduledFor;

        public DateTime ScheduledFor {
            get { return scheduledFor; }
        }

        public ScheduledCommandWrapper (DateTime t, CommandDelegate d) : base (d)
        {
            scheduledFor = t;
        }
    }

    public static class TaskScheduler
    {
        private static Timer nextEventTimer;
        private static ScheduledCommandWrapper nextTask;
        private static IntervalHeap<ScheduledCommandWrapper> commands;
        private static AutoResetEvent timerHandle = new AutoResetEvent (false);

        private static bool disposed;
        private static readonly object sync = new object ();

        static TaskScheduler ()
        {
            commands = new IntervalHeap<ScheduledCommandWrapper> (
                new ScheduledCommandWrapperComparer ()
            );

            nextEventTimer = new Timer (TimerCallbackHandler);
            ThreadPool.QueueUserWorkItem (BlahSignaledHandler);
        }

        public static void Dispose ()
        {
            lock (sync) {
                Console.WriteLine ("Dispose - start");
                if (!disposed) {
                    disposed = true;

                    if (nextEventTimer != null) {
                        nextEventTimer.Dispose ();
                        nextEventTimer = null;
                    }

                    if (timerHandle != null) {
                        timerHandle.Set ();
                    }

                    for (int i = commands.Count; i > 0; --i) {
                        commands.DeleteMin ();
                    }
                }
                Console.WriteLine ("Dispose - end");
            }
        }

        private static int count = 0;
        public static void Main ()
        {

            ScheduledCommandWrapper[] commands = new ScheduledCommandWrapper[10];
            DateTime time = DateTime.Now.AddMilliseconds (1000);

            for (int i = 0; i < 1000; ++i) {
                TaskScheduler.Schedule (new ScheduledCommandWrapper (
                    time, delegate { lock (sync) { Console.WriteLine ("HI:  {0}", count++); } }
                ));

                time = time.AddMilliseconds (100);
                Console.WriteLine (time);
            }

            Thread.Sleep (100000);
        }

        public static bool Cancel (IPriorityQueueHandle<ScheduledCommandWrapper> handle)
        {
			bool ret = false;
			
            if (handle == null) {
                throw new ArgumentNullException ("handle");
            }

            lock (sync) {
                if (!disposed) {
					ScheduledCommandWrapper command = commands.Delete (handle);
					if (command != null) {
						ret = true;
						
						if (nextTask == command) {
							nextTask = commands.FindMin ();
							ModifyTimer (nextTask.ScheduledFor);
						}
					}
                }
            }
			
			return ret;
        }

        public static IPriorityQueueHandle<ScheduledCommandWrapper> Schedule (ScheduledCommandWrapper scw)
        {
            if (scw == null) {
                throw new ArgumentNullException ("scw");
            }

            IPriorityQueueHandle<ScheduledCommandWrapper> handle = null;

            lock (sync) {
                if (!disposed) {
                    Console.WriteLine ("Scheduled");
                    commands.Add (ref handle, scw);

                    if (nextTask == null ||
                        nextTask.ScheduledFor > scw.ScheduledFor) {
                        nextTask = scw;
						ModifyTimer (nextTask.ScheduledFor);
                    }
                }
            }

            return handle;
        }

        private static void ModifyTimer (DateTime newTime)
        {
            if (!disposed) {
                TimeSpan span = newTime - DateTime.Now;
                long time = span.TotalMilliseconds < 0 ? 0 :
                    Convert.ToInt64 (span.TotalMilliseconds);
                nextEventTimer.Change (time, Timeout.Infinite);
            }
        }

        private static void TimerCallbackHandler (object state)
        {
            Console.WriteLine ("TimerCallbackHandler");

            lock (sync) {
                if (timerHandle != null) {
                    timerHandle.Set ();
                }
            }
        }

        private static void BlahSignaledHandler (object state)
        {
            try {
                BlahSignaledHandlerImpl ();
            } catch (Exception e){
                Console.WriteLine ("EXCEPTION:  {0}", e.Message);
            } finally {
                lock (sync) {
                    if (timerHandle != null) {
                        timerHandle.Close ();
                        timerHandle = null;
                    }
                }
            }
        }

        private static void BlahSignaledHandlerImpl ()
        {
            bool cont = false;			
            ScheduledCommandWrapper command;
			
			while (true) {
                Console.WriteLine ("Waiting...");
                timerHandle.WaitOne ();
                Console.WriteLine ("Executing...");
				do {
				    cont = false;
					command = null;
					
	                lock (sync) {
	                    if (disposed) {
                            Console.WriteLine ("Returned");
	                        return;
	                     } else {
                            command = commands.DeleteMin ();
                            if (commands.Count > 0) {								
                                nextTask = commands.FindMin ();
                            } else {
                                nextTask = null;
                            }
                        }

                        if (nextTask != null) {
                            if (nextTask.ScheduledFor <= DateTime.Now) {
    					        cont = true;
                            } else {									
                                ModifyTimer (nextTask.ScheduledFor);
                            }
	                    }
	                }
	
	                if (command != null) {
                        try {	
                            command.Execute ();
                        } catch (Exception e) {
                            Console.WriteLine (
                                "ATS COMMAND EXCEPTION:  {0}", e.Message
                            );
                        }
	                }
				} while (cont);
			}
        }
    }
}
*/