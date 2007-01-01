/***************************************************************************
 *  SchedulerMetadataProvider.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Banshee.Kernel;
using Banshee.Base;

namespace Banshee.Metadata
{
    public abstract class SchedulerMetadataProvider : IMetadataProvider
    {
        private Dictionary<IBasicTrackInfo, SchedulerMetadataLookupJob> queries 
            = new Dictionary<IBasicTrackInfo, SchedulerMetadataLookupJob>();
    
        public event MetadataLookupResultHandler HaveResult;
        
        protected abstract SchedulerMetadataLookupJob CreateJob(IBasicTrackInfo track);
        
        protected SchedulerMetadataProvider()
        {
            Scheduler.JobFinished += OnSchedulerJobFinished;
        }
        
        public void Lookup(IBasicTrackInfo track)
        {
            if(track == null || queries == null) {
                return;
            }
            
            lock(((ICollection)queries).SyncRoot) {
                if(!queries.ContainsKey(track)) {
                    SchedulerMetadataLookupJob job = CreateJob(track);
                    if(job == null) {
                        return;
                    }
                    
                    queries.Add(track, job);
                    Scheduler.Schedule(job, JobPriority.Highest);
                }
            }
        }
        
        public void Cancel(IBasicTrackInfo lookupId)
        {
        }
        
        public void Cancel()
        {
        }
        
        private bool RemoveJob(SchedulerMetadataLookupJob job)
        {
            lock(((ICollection)queries).SyncRoot) {
                if(queries.ContainsKey(job.Track)) {
                    queries.Remove(job.Track);
                    return true;
                }
                
                return false;
            }
        }
        
        private void OnSchedulerJobFinished(IJob job)
        {
            if(!(job is SchedulerMetadataLookupJob)) {
                return;
            }
            
            SchedulerMetadataLookupJob lookup_job = (SchedulerMetadataLookupJob)job;
            if(RemoveJob(lookup_job)) {
                ThreadAssist.ProxyToMain(delegate { 
                    OnHaveResult(lookup_job.Track, lookup_job.ResultTags); 
                });
            }
        }
        
        protected virtual void OnHaveResult(IBasicTrackInfo track, IList<StreamTag> tags)
        {
            if(tags == null) {
                return;
            }
            
            MetadataLookupResultHandler handler = HaveResult;
            if(handler != null) {
                handler(this, new MetadataLookupResultArgs(track, 
                    new ReadOnlyCollection<StreamTag>(tags)));
            }
        }
    }
}
