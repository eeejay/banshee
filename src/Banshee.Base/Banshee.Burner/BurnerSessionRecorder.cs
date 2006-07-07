/***************************************************************************
 *  BurnerSessionRecorder.cs
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

using System;
using System.Threading;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Cdrom;

namespace Banshee.Burner
{
    internal class BurnerSessionRecorder
    {
        private IRecorder recorder;
        private ActiveUserEvent user_event;
        private RecorderAction currentAction;
        private BurnerSession session;
        
        private readonly string BURN_INHIBIT_REASON = Catalog.GetString("Writing a disc");
        
        public BurnerSessionRecorder(BurnerSession session)
        {
            this.recorder = session.Recorder;
            this.session = session;
            
            user_event = new ActiveUserEvent(Catalog.GetString("Writing Disc"));
            user_event.Header = Catalog.GetString("Writing Disc");
            user_event.Icon = new Gdk.Pixbuf(System.Reflection.Assembly.GetEntryAssembly(), "cd-action-burn-24.png");
            user_event.CancelRequested += OnUserEventCancelRequested;
        }
        
        public void AddTrack(RecorderTrack track)
        {
            recorder.AddTrack(track);
        }
        
        public void Record()
        {
            ThreadAssist.Spawn(BurnThread);
        }
        
        private void OnUserEventCancelRequested(object o, EventArgs args) 
        {
            if(recorder != null) {
                recorder.CancelWrite(false);
            }
        }
        
        private void BurnThread()
        {
            try {
                recorder.ProgressChanged += OnProgressChanged;
                recorder.ActionChanged += OnActionChanged;
                recorder.InsertMediaRequest += OnInsertMediaRequest;
                
                PowerManagement.Inhibit(BURN_INHIBIT_REASON);

                RecorderResult result = recorder.WriteTracks(session.WriteSpeed, session.EjectWhenFinished);
                
                if(result == RecorderResult.Error) {
                    string header = null;
                    string message = null;
                    
                    if(header == null || header == String.Empty) {
                        header = Catalog.GetString("Error writing disc");
                    }
                    
                    if(message == null || message == String.Empty) {
                        message = Catalog.GetString("An unknown error occurred when attempting to write the disc.");
                    }
                    
                    LogCore.Instance.PushError(header, message);
                } else if(result != RecorderResult.Canceled) {
                    LogCore.Instance.PushInformation(
                        Catalog.GetString("Disc writing complete"),
                        Catalog.GetString("The selected audio was successfully written to the disc.")
                    );
                }
            } catch(Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Error writing disc"), e.Message);    
            } finally {
                foreach(string file in System.IO.Directory.GetFiles(Paths.TempDir)) {
                    try {
                        System.IO.File.Delete(file);
                    } catch {
                    }
                }
                
                user_event.Dispose();
                
                recorder.ProgressChanged -= OnProgressChanged;
                recorder.ActionChanged -= OnActionChanged;
                recorder.InsertMediaRequest -= OnInsertMediaRequest;

                PowerManagement.UnInhibit(BURN_INHIBIT_REASON);
            }
        }
        
        private void OnProgressChanged(object o, ProgressChangedArgs args) 
        {
            if(currentAction == RecorderAction.Writing) {
                user_event.Progress = args.Fraction;
            }
        }
        
        private void OnActionChanged(object o, ActionChangedArgs args) 
        {
            currentAction = args.Action;

            switch(currentAction) {
                case RecorderAction.PreparingWrite:
                    user_event.Message = Catalog.GetString("Preparing to record");
                    user_event.CanCancel = true;
                    break;
                case RecorderAction.Writing:
                    user_event.Message = Catalog.GetString("Recording contents");
                    user_event.CanCancel = true;
                    break;
                case RecorderAction.Fixating:
                    user_event.Message = Catalog.GetString("Fixating disc");
                    user_event.Progress = 0.0;
                    user_event.CanCancel = false;
                    break;
            }
        }
       
        private bool media_present;
        
        private void OnInsertMediaRequest(object o, InsertMediaRequestArgs args)
        {
            media_present = false;
            
            if(user_event.IsCancelRequested) {
                recorder.CancelWrite(false);
                return;
            }
            
            user_event.Message = Catalog.GetString("Waiting for Media");
            
            ThreadAssist.ProxyToMain(delegate {
                HigMessageDialog dialog = new HigMessageDialog(null, DialogFlags.Modal, MessageType.Info,
                    ButtonsType.OkCancel, Catalog.GetString("Insert blank disc"),
                    Catalog.GetString("Please insert a blank disc for the write process."));
                
                dialog.Title = Catalog.GetString("Insert blank disc");
                IconThemeUtils.SetWindowIcon(dialog);
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
                media_present = recorder.MediaSize > 0;
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
