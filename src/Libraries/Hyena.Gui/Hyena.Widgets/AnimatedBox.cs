//
// AnimatedBox.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Gdk;
using Gtk;

using Hyena.Gui.Theatrics;

namespace Hyena.Gui
{
    public abstract class AnimatedBox : Container, IEnumerable<Widget>
    {
        private readonly Stage<AnimatedWidget> stage = new Stage<AnimatedWidget> ();
        private readonly Queue<AnimatedWidget> expired = new Queue<AnimatedWidget> ();
        private readonly LinkedList<AnimatedWidget> children = new LinkedList<AnimatedWidget> ();
        private readonly object childrenMutex = new object ();
        
        protected int spacing;
        protected int startSpacing;
        protected int endSpacing;
        
        private uint duration = 500;
        private Easing easing = Easing.Linear;
        private Blocking blocking = Blocking.Upstage;
        
        protected AnimatedBox ()
        {
            stage.ActorStep += OnActorStep;
            stage.Iteration += OnIteration;
        }
        
#region Private Methods
        
        private bool OnActorStep (Actor<AnimatedWidget> actor)
        {
            lock (actor.Target) {
                switch (actor.Target.AnimationState) {
                case AnimationState.Coming:
                    actor.Target.Percent = actor.Percent;
                    if (actor.Expired) {
                        actor.Target.AnimationState = AnimationState.Idle;
                        return false;
                    }
                    break;
                case AnimationState.IntendingToGo:
                    actor.Target.AnimationState = AnimationState.Going;
                    actor.Target.Bias = actor.Percent;
                    actor.Reset ((uint)(actor.Target.Duration * actor.Percent));
                    break;
                case AnimationState.Going:
                    if (actor.Expired) {
                        lock (childrenMutex) {
                            children.Remove (actor.Target.Node);
                        }
                        expired.Enqueue (actor.Target);
                        return false;
                    } else {
                        actor.Target.Percent = 1.0 - actor.Percent;
                    }
                    break;
                }
            }
            
            return true;
        }
        
        private void OnIteration (object sender, EventArgs args)
        {
            // When widgets are disposed, their hash code changes (zee uber lame).
            // We would otherwise do this up in OnActorStep, but the has code needs
            // to remain the same so that the actor can be removed from the stage.
            while (expired.Count > 0) {
                AnimatedWidget widget = expired.Dequeue ();
                widget.Unparent ();
                widget.Dispose ();
            }
            
            QueueResizeNoRedraw ();
        }
        
        private void OnWidgetDestroyed (object sender, EventArgs args)
        {
            RemoveCore ((AnimatedWidget)sender);
        }
        
#endregion
    
#region Pack Methods
        
        public void PackStart (Widget widget)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, uint duration)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, Easing easing)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, uint duration, Easing easing)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, Blocking blocking)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, uint duration, Blocking blocking)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, Easing easing, Blocking blocking)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        public void PackStart (Widget widget, uint duration, Easing easing, Blocking blocking)
        {
            AnimatedWidget animatedWidget = Pack (widget, duration, easing, blocking);
            lock (childrenMutex) {
                animatedWidget.Node = children.AddFirst (animatedWidget);
            }
        }
        
        public void PackEnd (Widget widget)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, uint duration)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, Easing easing)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, uint duration, Easing easing)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, Blocking blocking)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, uint duration, Blocking blocking)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, Easing easing, Blocking blocking)
        {
            PackEnd (widget, duration, easing, blocking);
        }
        
        public void PackEnd (Widget widget, uint duration, Easing easing, Blocking blocking)
        {
            AnimatedWidget animatedWidget = Pack (widget, duration, easing, blocking);
            lock (childrenMutex) {
                animatedWidget.Node = children.AddLast (animatedWidget);
            }
        }
        
        private AnimatedWidget Pack (Widget widget, uint duration, Easing easing, Blocking blocking)
        {
            if (widget == null) {
                throw new ArgumentNullException ("widget");
            }
            
            AnimatedWidget animatedWidget = new AnimatedWidget (widget, duration, easing, blocking);
            animatedWidget.Parent = this;
            animatedWidget.WidgetDestroyed += OnWidgetDestroyed;
            stage.Add (animatedWidget, duration);
            return animatedWidget;
        }
        
#endregion
        
#region Remove Methods
        
        public new void Remove (Widget widget)
        {
            RemoveCore (widget, null, null, null);
        }
        
        public void Remove (Widget widget, uint duration)
        {
            RemoveCore (widget, duration, null, null);
        }
        
        public void Remove (Widget widget, Easing easing)
        {
            RemoveCore (widget, null, easing, null);
        }
        
        public void Remove (Widget widget, uint duration, Easing easing)
        {
            RemoveCore (widget, duration, easing, null);
        }
        
        public void Remove (Widget widget, Blocking blocking)
        {
            RemoveCore (widget, null, null, blocking);
        }
        
        public void Remove (Widget widget, uint duration, Blocking blocking)
        {
            RemoveCore (widget, duration, null, blocking);
        }
        
        public void Remove (Widget widget, Easing easing, Blocking blocking)
        {
            RemoveCore (widget, null, easing, blocking);
        }
        
        public void Remove (Widget widget, uint duration, Easing easing, Blocking blocking)
        {
            RemoveCore (widget, duration, easing, blocking);
        }
        
        private void RemoveCore (Widget widget, uint? duration, Easing? easing, Blocking? blocking)
        {
            if (widget == null) {
                throw new ArgumentNullException ("widget");
            }
            
            AnimatedWidget animatedWidget = null;
            foreach (AnimatedWidget child in this) {
                if (child.Widget == widget) {
                    animatedWidget = child;
                    break;
                }
            }
            if (animatedWidget == null) {
                throw new ArgumentException ("Cannot remove the specified widget because it has not been added to this container or it has already been removed.", "widget");
            }
            RemoveCore (animatedWidget, duration, easing, blocking);
        }
        
        private void RemoveCore (AnimatedWidget widget)
        {
            RemoveCore (widget, widget.Duration, widget.Easing, widget.Blocking);
        }
        
        private void RemoveCore (AnimatedWidget widget, uint? duration, Easing? easing, Blocking? blocking)
        {
            lock (widget) {
                if (duration != null) {
                    widget.Duration = duration.Value;
                }
                if (easing != null) {
                    widget.Easing = easing.Value;
                }
                if (blocking != null) {
                    widget.Blocking = blocking.Value;
                }
            
                if (widget.AnimationState == AnimationState.Coming) {
                    widget.AnimationState = AnimationState.IntendingToGo;
                } else {
                    if (widget.Easing == Easing.QuadraticIn) {
                        widget.Easing = Easing.QuadraticOut;
                    } else if (widget.Easing == Easing.QuadraticOut) {
                        widget.Easing = Easing.QuadraticIn;
                    } else if (widget.Easing == Easing.ExponentialIn) {
                        widget.Easing = Easing.ExponentialOut;
                    } else if (widget.Easing == Easing.ExponentialOut) {
                        widget.Easing = Easing.ExponentialIn;
                    }
                    widget.AnimationState = AnimationState.Going;
                    stage.Add (widget, widget.Duration);
                }
            }
        }
        
#endregion
        
#region Other Public Methods
        
        public bool Contains (Widget widget)
        {
            foreach (AnimatedWidget child in this) {
                if (child.AnimationState != AnimationState.Going && child.Widget == widget) {
                    return true;
                }
            }
            return false;
        }
        
        public new IEnumerator<Widget> GetEnumerator ()
        {
            lock (childrenMutex) {
                foreach (AnimatedWidget child in children) {
                    yield return child;
                }
            }
        }
        
        IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
        
#endregion
        
#region Overrides
        
        protected override void OnAdded (Widget widget)
        {
            PackStart (widget, duration, easing, blocking);
        }
        
        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized | WidgetFlags.NoWindow;
            GdkWindow = Parent.GdkWindow;
        }
        
        protected override void ForAll (bool include_internals, Callback callback)
        {
            foreach (AnimatedWidget child in this) {
                callback (child);
            }
        }
        
#endregion
        
#region Properties
        
        public int Spacing {
            get { return spacing; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException ("value", "Spacing cannot be less than 0.");
                }
                spacing = value;
                double half = (double)spacing / 2.0;
                startSpacing = (int)Math.Floor (half);
                endSpacing = (int)Math.Ceiling (half);
            }
        }
        
        public uint Duration {
            get { return duration; }
            set { duration = value; }
        }
        
        public Easing Easing {
            get { return easing; }
            set { easing = value; }
        }
        
        public Blocking Blocking {
            get { return blocking; }
            set { blocking = value; }
        }
        
#endregion
        
    }
}