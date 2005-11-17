/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  MultiStateToggleButton.cs
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
using Gtk;

namespace Banshee.Widgets
{
    public delegate void ToggleStateChangedHandler(object o, ToggleStateChangedArgs args);

    public class ToggleStateChangedArgs : EventArgs 
    {
        public Type ToggleState;
    }

    public class MultiStateToggleButton : Button
    {
        private ArrayList states = new ArrayList();
        private int active_state_index;
        private bool show_label = true;
        private bool show_icon = true;
        private HBox box = new HBox();
        private Image icon = new Image();
        private Label label = new Label();
        private int ignore_menu_toggle = 0;
        private bool is_boolean = false;
        
        public event ToggleStateChangedHandler Changed;
        
        public MultiStateToggleButton() : base()
        {
            Add(box);
            box.Spacing = 5;
        }
        
        public MultiStateToggleButton(params Type [] states) : this()
        {
            foreach(Type state in states) {
                AddState(state);
            }
            
            ActiveStateIndex = 0;
            UpdateButton(true);
        }
        
        private void UpdateButton(bool rebuild)
        {
            if(rebuild) {
                while(box.Children.Length > 0) {
                    box.Remove(box.Children[0]);
                }
                
                if(ShowIcon) {
                    box.PackStart(icon, false, false, 0);
                }
                
                if(ShowLabel) {
                    box.PackStart(label, true, true, 0);
                }
                
                box.Spacing = ShowLabel && ShowIcon ? 5 : 0;
            }
            
            icon.Pixbuf = this[ActiveStateIndex].Icon;
            label.Text = this[ActiveStateIndex].Label;
        }
        
        private Type FindStateTypeFromMenuItem(CheckMenuItem item)
        {
            foreach(ToggleState state_instance in states) {
                if(state_instance.MenuItem.GetHashCode() == item.GetHashCode()) {
                    return state_instance.GetType();
                }
            }
            
            return null;
        }
        
        private Type FindBooleanFalseState()
        {
            foreach(ToggleState state_instance in states) {
                if(!state_instance.CheckActive) {
                    return state_instance.GetType();
                }
            }
            
            return null;
        }
        
        private Type FindBooleanTrueState()
        {
            foreach(ToggleState state_instance in states) {
                if(state_instance.CheckActive) {
                    return state_instance.GetType();
                }
            }
            
            return null;        
        }
        
        private void EmitChanged()
        {
            ToggleStateChangedHandler handler = Changed;
            if(handler != null) {
                ToggleStateChangedArgs args = new ToggleStateChangedArgs();
                args.ToggleState = ActiveState;
                handler(this, args);
            }
        }
        
        private void OnMenuItemToggled(object o, EventArgs args)
        {
            if(ignore_menu_toggle > 0) {
                return;
            }
            
            CheckMenuItem item = o as CheckMenuItem;
            
            if(!is_boolean) {
                if(!item.Active) {
                    return;
                }
                
                ActiveState = FindStateTypeFromMenuItem(item);
                EmitChanged();
            } else {
                ActiveState = item.Active ? FindBooleanTrueState() : FindBooleanFalseState();
                EmitChanged();
            }
        }
        
        protected override void OnClicked()
        {
            Cycle();
            
            if(is_boolean) {
                ignore_menu_toggle++;
                if(ActiveState == FindBooleanFalseState()) {
                    this[ActiveState].MenuItem.Active = false;
                } else {
                    this[ActiveState].MenuItem.Active = true;
                }
                ignore_menu_toggle--;
                EmitChanged();
            } else if(ContainsState(ActiveState)) {
                this[ActiveState].MenuItem.Active = this[ActiveState].CheckActive;
            } else {
                EmitChanged();
            }
        }
        
        public void NextState()
        {
            if(ActiveStateIndex < StateCount - 1) {
                ActiveStateIndex++;
            }
        }
        
        public void PreviousState()
        {
            if(ActiveStateIndex > 0) {
                ActiveStateIndex--;
            }
        }
        
        public void Cycle()
        {
            if(ActiveStateIndex < StateCount - 1) {
                ActiveStateIndex++;
            } else {
                ActiveStateIndex = 0;
            }
        }

        public bool ContainsState(Type state)
        {
            foreach(ToggleState previous_state in states) {
                if(previous_state.GetType() == state) {
                    return true;
                }
            }
            
            return false;
        } 
        
        public ToggleState AddState(Type state)
        {
            if(ContainsState(state)) {
                throw new ApplicationException(String.Format(
                    "Another ToggleState of type '{0}' has already been added. " + 
                    "Cannot add two of the same ToggleState subclasses.", 
                    state.GetType()));
            }
            
            if(!state.IsSubclassOf(typeof(ToggleState))) { 
                throw new ApplicationException("Can only add types that are subclassed from ToggleState");
            }

            ToggleState state_instance = Activator.CreateInstance(state) as ToggleState;
            states.Add(state_instance);
            
            is_boolean = states.Count == 2;
            
            return state_instance;
        }
        
        public ToggleState AddState(Type state, CheckMenuItem menuItem, bool checkActive)
        {
            ToggleState state_instance = AddState(state);
            state_instance.MenuItem = menuItem;
            state_instance.CheckActive = checkActive;
            menuItem.Toggled += OnMenuItemToggled;
            return state_instance;
        }
        
        public ToggleState AddState(Type state, CheckMenuItem menuItem)
        {
            return AddState(state, menuItem, true);
        }

        public ToggleState this [int index] {
            get {
                return states[index] as ToggleState;
            }
        }
        
        public ToggleState this [Type state] {
            get {
                foreach(ToggleState state_instance in states) {
                    if(state_instance.GetType() == state) {
                        return state_instance;
                    }
                }
                
                return null;
            }
        }
        
        public int StateCount {
            get {
                return states.Count;
            }
        }
        
        public int ActiveStateIndex {
            get {
                return active_state_index;
            }
            
            set {
                if(value < 0 || value >= StateCount) {
                    throw new ApplicationException("Index is out of range");
                }
                
                active_state_index = value;
                UpdateButton(false);
            }
        }
        
        public Type ActiveState {
            get {
                return states[active_state_index].GetType();
            }
            
            set {
                for(int i = 0; i < states.Count; i++) {
                    if(states[i].GetType() == value) {
                        active_state_index = i;
                        UpdateButton(false);
                        ignore_menu_toggle++;
                        this[ActiveState].MenuItem.Active = this[ActiveState].CheckActive;
                        ignore_menu_toggle--;
                        return;
                    }
                }
                
                throw new ApplicationException("ToggleState is not supported by this button");
            }
        }
        
        public string ActiveLabel {
            get {
                return (states[active_state_index] as ToggleState).Label;
            }
        }
        
        public bool ShowLabel {
            set {
                if(show_label != value) {
                    show_label = value;
                    UpdateButton(true);
                }
            }
            
            get {
                return show_label;
            }
        }
        
        public bool ShowIcon {
            set {
                if(show_icon != value) {
                    show_icon = value;
                    UpdateButton(true);
                }
            }
            
            get {
                return show_icon;
            }
        }
    }
}
