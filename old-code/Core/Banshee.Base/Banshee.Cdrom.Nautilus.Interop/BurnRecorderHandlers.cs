using System;
using GLib;

namespace Banshee.Cdrom.Nautilus.Interop 
{
    internal delegate void ActionChangedHandler(object o, ActionChangedArgs args);

    internal class ActionChangedArgs : SignalArgs 
    {
        public BurnRecorderActions Action {
            get { return (BurnRecorderActions)Args[0]; }
        }

        public BurnRecorderMedia Media {
            get { return (BurnRecorderMedia)Args[1]; }
        }
    }
    
    
    internal delegate void AnimationChangedHandler(object o, AnimationChangedArgs args);

    internal class AnimationChangedArgs : SignalArgs {
        public bool Spinning {
            get { return (bool)Args[0]; }
        }
    }
    
    
    internal delegate void WarnDataLossHandler(object o, WarnDataLossArgs args);

    internal class WarnDataLossArgs : SignalArgs 
    {
    }
    
    
    internal delegate void InsertMediaRequestHandler(object o, InsertMediaRequestArgs args);

    internal class InsertMediaRequestArgs : SignalArgs 
    {
        public bool IsReload{
            get { return (bool)Args[0]; }
        }

        public bool CanRewrite{
            get {
                return (bool)Args[1];
            }
        }

        public bool Busy{
            get { return (bool)Args[2]; }
        }
    }
    
    internal delegate void ProgressChangedHandler(object o, ProgressChangedArgs args);

    internal class ProgressChangedArgs : SignalArgs 
    {
        public double Fraction {
            get { return (double)Args[0]; }
        }

        public int Secs{
            get { return (int)Args[1]; }
        }
    }
}
