/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  BurnCore.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Threading;
using System.IO;
using Mono.Unix;
using Gtk;
using Nautilus;

namespace Banshee
{
    public class BurnCore
    {
        public enum DiskType : uint {
            Audio,
            Mp3,
            Data
        };
    
        private Queue encodeQueue = new Queue();
        private Queue burnQueue = new Queue();
        private DiskType diskType;
        private bool canceled;
        
        public BurnCore(DiskType diskType)
        {
            this.diskType = diskType;
        }
        
        public void AddTrack(TrackInfo track)
        {
            encodeQueue.Enqueue(track);
        }
        
        public void Burn()
        {
            PipelineProfile profile = null;
            
            canceled = false;
            
            switch(diskType) {
                case DiskType.Audio:
                    foreach(PipelineProfile cp in PipelineProfile.Profiles) {
                       if(cp.Extension == "wav") {
                           profile = new PipelineProfile(cp);
                           break;
                       }
                    }
                    break;
                case DiskType.Mp3:
                case DiskType.Data:
                default:
                    foreach(TrackInfo ti in encodeQueue)
                        burnQueue.Enqueue(ti.Uri);
                    DoBurn();
                    return;
            }
            
            if(profile == null) {
                 Core.Log.PushError(
                     Catalog.GetString("Could Not Write CD"),
                     Catalog.GetString("No suitable wave encoder could be found to convert selected songs to CD audio format."));
                 return;
            }
        
            FileEncodeAction fet = new FileEncodeAction(profile);
            fet.FileEncodeComplete += OnFileEncodeComplete;
            fet.Finished += OnFileEncodeTransactionFinished;
            fet.Canceled += OnFileEncodeTransactionCanceled;
            
            while(encodeQueue.Count > 0) {
                TrackInfo ti = encodeQueue.Dequeue() as TrackInfo;
                string outputFile = Paths.TempDir + "/"  + 
                    Path.GetFileNameWithoutExtension(ti.Uri.LocalPath) + "." + 
                    profile.Extension;
                
                fet.AddTrack(ti, new Uri(outputFile));
            }
                
            fet.Run();
        }
        
        private void OnFileEncodeComplete(object o, FileEncodeCompleteArgs args)
        {
            burnQueue.Enqueue(args.EncodedFileUri);
        }
        
        private void OnFileEncodeTransactionFinished(object o, EventArgs args)
        {
            if(!canceled)
                DoBurn();
        }
        
        private void OnFileEncodeTransactionCanceled(object o, EventArgs args)
        {
            canceled = true;
        }
        
        private void DoBurn()
        {
            Core.ProxyToMainThread(delegate {
                new Burner(diskType, burnQueue);
            });
        }
    }
    
    public class Burner
    {
        private Queue burnQueue;
        private BurnCore.DiskType diskType;
        private BurnDrive drive;
        private BurnRecorder recorder;
        private BurnRecorderActions currentAction;
        private long TotalDuration;
        private ActiveUserEvent user_event;
        
        public Burner(BurnCore.DiskType diskType, Queue burnQueue)
        {
            this.diskType = diskType;
            this.burnQueue = burnQueue;
            
            user_event = new ActiveUserEvent(Catalog.GetString("Writing CD"));
            user_event.Icon = Gdk.Pixbuf.LoadFromResource("cd-action-burn-24.png");
            user_event.CancelRequested += OnUserEventCancelRequested;
            
            Thread burnThread = new Thread(new ThreadStart(BurnThread));
            burnThread.Start();
        }
        
        private bool GetBoolPref(string key, bool def)
        {
            try {
                return (bool)Core.GconfClient.Get(key);
            } catch(Exception) {
                return def;
            }
        }

        private void OnUserEventCancelRequested(object o, EventArgs args) 
        {
            if(recorder != null) {
                recorder.Cancel(false);
            }
        }
        
        private bool HaveRequiredSpace() 
        {
            long available = (long)(((drive.MediaSize  / 1024 / 1024) - 1) * 48 / 7);
            long remaining = (long)(available - TotalDuration);

            if(remaining < 0) {
                int minutes = (int)(-remaining / 60);
                string msg =
                    Catalog.GetString("The inserted media is not large enough to hold your selected music.") + " " +
                    Catalog.GetPluralString(
                        "{0} more minute is needed on the media.",
                        "{0} more minutes are needed on the media.",
                        minutes);
            
                Core.Log.PushWarning(Catalog.GetString("Not Enough Space on Disc"), msg);
                return false;
            }

            return true;
        }
        
        private void BurnThread()
        {
            ArrayList tracks = new ArrayList();
    
            try {
                if(recorder == null) {
                    recorder = new BurnRecorder();
                    recorder.ProgressChanged += OnProgressChanged;
                    recorder.ActionChanged += OnActionChanged;
                    recorder.InsertMediaRequest += OnInsertMediaRequest;
                    //recorder.WarnDataLoss += OnWarnDataLoss;
                }
                
                string selectedBurnerId;
                
                try { 
                    selectedBurnerId = (string)Core.GconfClient.Get(GConfKeys.CDBurnerId);
                } catch(Exception) { 
                    selectedBurnerId = null;
                }
                
                drive = BurnUtil.GetDriveByIdOrDefault(selectedBurnerId);
                    
                if(drive == null) {
                    throw new ApplicationException(Catalog.GetString("No CD writers were found on your system."));
                }
                
                selectedBurnerId = BurnUtil.GetDriveUniqueId(drive);
                
                string burnKeyParent = GConfKeys.CDBurnerRoot + selectedBurnerId + "/";
                
                foreach(Uri uri in burnQueue) {
                    tracks.Add(new BurnRecorderTrack(uri.LocalPath, 
                        diskType == BurnCore.DiskType.Audio ?
                            BurnRecorderTrackType.Audio :
                            BurnRecorderTrackType.Data));
                }
                
                BurnRecorderWriteFlags flags = BurnRecorderWriteFlags.Debug;
                
                if(GetBoolPref(burnKeyParent + "Eject", true))
                    flags |= BurnRecorderWriteFlags.Eject;
                
                if(GetBoolPref(burnKeyParent + "DAO", true))
                    flags |= BurnRecorderWriteFlags.DiscAtOnce;
                
                if(GetBoolPref(burnKeyParent + "Overburn", false)) 
                    flags |= BurnRecorderWriteFlags.Overburn;
                
                if(GetBoolPref(burnKeyParent + "Simulate", false))
                    flags |= BurnRecorderWriteFlags.DummyWrite;
                
                if(GetBoolPref(burnKeyParent + "Burnproof", true))
                    flags |= BurnRecorderWriteFlags.Burnproof;

                BurnRecorderResult result = (BurnRecorderResult)recorder.WriteTracks(drive,
                    tracks.ToArray(typeof(BurnRecorderTrack)) as BurnRecorderTrack [],
                    drive.MaxWriteSpeed, flags);
                
                if(result == BurnRecorderResult.Error) {
                    string header = null;
                    string message = null;
                    
                    if(header == null || header == String.Empty) {
                        header = Catalog.GetString("Error Burning CD");
                    }
                    
                    if(message == null || message ==String.Empty) {
                        message = Catalog.GetString("An unknown error occurred when attempting to write the CD");
                    }
                    
                    Core.Log.PushError(header, message);
                } else if(result != BurnRecorderResult.Cancel) {
                    Core.Log.PushInformation(
                        Catalog.GetString("CD Writing Complete"),
                        Catalog.GetString("The selected audio was successfully written to the CD.")
                    );
                }
            } catch(Exception e) {
                Core.Log.PushError(Catalog.GetString("Error Writing CD"), e.Message);    
            } finally {
                foreach(string file in Directory.GetFiles(Paths.TempDir)) {
                    File.Delete(file);
                }
                
                user_event.Dispose();
            }
        }

        private void OnProgressChanged(object o, ProgressChangedArgs args) 
        {
            if(currentAction == BurnRecorderActions.Writing) {
                user_event.Progress = args.Fraction;
            }
        }
        
        private void OnActionChanged(object o, ActionChangedArgs args) 
        {
            currentAction = args.Action;

            switch(currentAction) {
                case BurnRecorderActions.PreparingWrite:
                    user_event.Message = Catalog.GetString("Preparing to write...");
                    user_event.CanCancel = true;
                    break;
                case BurnRecorderActions.Writing:
                    user_event.Message = Catalog.GetString("Writing disk...");
                    user_event.CanCancel = true;
                    break;
                case BurnRecorderActions.Fixating:
                    user_event.Message = Catalog.GetString("Fixating disk...");
                    user_event.CanCancel = false;
                    break;
            }
        }
       
        private bool media_present;
        
        private void OnInsertMediaRequest(object o, InsertMediaRequestArgs args)
        {
            media_present = false;
            
            if(user_event.IsCancelRequested) {
                recorder.Cancel(false);
                return;
            }
            
            user_event.Message = Catalog.GetString("Waiting for Media");
            
            Core.ProxyToMainThread(delegate {
                HigMessageDialog dialog = new HigMessageDialog(null, DialogFlags.Modal, MessageType.Info,
                    ButtonsType.OkCancel, Catalog.GetString("Insert Blank CD"),
                    Catalog.GetString("Please insert a blank CD disk for the write process."));
                
                dialog.Title = Catalog.GetString("Insert Blank CD");
                dialog.Icon = ThemeIcons.WindowManager;
                dialog.DefaultResponse = ResponseType.Ok;
                dialog.Response += OnMediaRequestResponse;
                dialog.ShowAll();
            });
            
            while(!media_present && !user_event.IsCancelRequested) {
                Thread.Sleep(250);
            }
        }

        private void OnMediaRequestResponse(object o, ResponseArgs args)
        {
            if(args.ResponseId == ResponseType.Ok) {
                media_present = drive.MediaSize > 0;
                if(!media_present) {
                    return;
                }
            }
            
            (o as Dialog).Response -= OnMediaRequestResponse;
            (o as Dialog).Destroy();
            
            if(args.ResponseId != ResponseType.Ok) {
                user_event.Cancel();
            }
        }
    }
}

