//
// ProfileConfigurationDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Collections.Generic;
using Mono.Unix;

using Gtk;

using Banshee.MediaProfiles;
using Banshee.Base;

namespace Banshee.MediaProfiles.Gui
{
    internal class PipelineVariableComboBox : Gtk.ComboBox
    {
        private PipelineVariable variable;
        private ListStore model;
        
        public PipelineVariableComboBox(PipelineVariable variable, ListStore model) : base()
        {
            this.variable = variable;
            this.model = model;
        }
        
        protected PipelineVariableComboBox(IntPtr ptr) : base(ptr)
        {
        }
        
        public PipelineVariable Variable {
            get { return variable; }
        }
        
        public ListStore Store {
            get { return model; }
        }
    }

    public class ProfileConfigurationDialog : Gtk.Dialog
    {
        private Profile profile;
    
        private Label header_label = new Label();
        private Hyena.Widgets.TextViewLabel description_label = new Hyena.Widgets.TextViewLabel();
        private Table normal_controls_table = new Table(1, 1, false);
        private Table advanced_controls_table = new Table(1, 1, false);
        private Expander advanced_expander = new Expander(Catalog.GetString("Advanced"));
        private TextView sexpr_results = null;
            
        private Dictionary<string, Widget> variable_widgets = new Dictionary<string, Widget>();

        public ProfileConfigurationDialog(Profile profile) : base()
        {
            this.profile = profile;
        
            HasSeparator = false;
            BorderWidth = 5;
                    
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);
            
            Button button = new Button(Stock.Close);
            button.CanDefault = true;
            button.Show();
            
            if(ApplicationContext.Debugging) {
                Button test_button = new Button("Test S-Expr");
                test_button.Show();
                test_button.Clicked += delegate {
                    if(sexpr_results != null) {
                        sexpr_results.Buffer.Text = profile.Pipeline.GetDefaultProcess();
                    }
                };
                ActionArea.PackStart(test_button);
                
                sexpr_results = new TextView();
            }
            
            AddActionWidget(button, ResponseType.Close);
            DefaultResponse = ResponseType.Close;
            button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Return,
                0, AccelFlags.Visible);

            BuildContents();
            
            LoadProfile();
        }
        
        private void BuildContents()
        {
            VBox box = new VBox();
            box.BorderWidth = 8;
            box.Spacing = 10;
            box.Show();
        
            header_label.Xalign = 0.0f;
            
            header_label.Show();
            description_label.Show();
            normal_controls_table.Show();
            advanced_controls_table.Show();

            advanced_expander.Add(advanced_controls_table);
            advanced_expander.Show();

            box.PackStart(header_label, false, false, 0);
            box.PackStart(description_label, false, false, 0);
            box.PackStart(normal_controls_table, false, false, 5);
            box.PackStart(advanced_expander, false, false, 0);
            
            if(sexpr_results != null) {
                ScrolledWindow scroll = new Gtk.ScrolledWindow();
                scroll.HscrollbarPolicy = PolicyType.Automatic;
                scroll.VscrollbarPolicy = PolicyType.Automatic;
                scroll.ShadowType = ShadowType.In;
                sexpr_results.WrapMode = WrapMode.Word;
                sexpr_results.SetSizeRequest(-1, 100);
                scroll.Add(sexpr_results);
                scroll.ShowAll();
                
                VSeparator sep = new VSeparator();
                sep.Show();
                
                Label label = new Label();
                label.Markup = "<b>S-Expr Results</b>";
                label.Xalign = 0.0f;
                label.Show();
               
                box.PackStart(sep, false, false, 0);
                box.PackStart(label, false, false, 0);
                box.PackStart(scroll, false, false, 0);
            }

            VBox.PackStart(box, false, false, 0);
            
            SetSizeRequest(350, -1);
            
            Gdk.Geometry limits = new Gdk.Geometry();
            limits.MinWidth = SizeRequest().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            SetGeometryHints(this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }

        private void LoadProfile()
        {
            Title = Catalog.GetString(String.Format("Configuring {0}", profile.Name));
            header_label.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(profile.Name));
            description_label.Text = profile.Description;

            LoadControlTable(normal_controls_table, false);
            LoadControlTable(advanced_controls_table, true);
            
            advanced_expander.Visible = advanced_controls_table.Visible;
        }

        private void LoadControlTable(Table table, bool advanced)
        {
            while(table.Children.Length > 0) {
                table.Remove(table.Children[0]);
            }
        
            table.Resize(1, 1);
        
            table.RowSpacing = 5;
            table.ColumnSpacing = 12;
            
            uint y = 0;

            foreach(PipelineVariable variable in profile.Pipeline) {
                if(advanced != variable.Advanced) {
                    continue;
                }
            
                Label label = new Label();
                label.Show();
                label.Markup = String.Format("<b>{0}:</b>", GLib.Markup.EscapeText(variable.Name));
                label.Xalign = 0.0f;

                try {
                    Widget control = BuildControl(variable);
                    if(control == null) {
                        throw new ApplicationException("Control could not be created");
                    }
                    
                    variable_widgets.Add(variable.Id, control);
                    
                    if(variable.ControlType != PipelineVariableControlType.Check) {
                        variable_widgets.Add(".label." + variable.Id, label);
                    }

                    control.Show();
                
                    table.Resize(y + 1, 2);
                
                    if(variable.ControlType != PipelineVariableControlType.Check) {
                        table.Attach(label, 0, 1, y, y + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                    }
                    
                    table.Attach(control, 1, 2, y, y + 1, 
                        control is ComboBox ? AttachOptions.Fill : AttachOptions.Fill | AttachOptions.Expand, 
                        AttachOptions.Fill, 0, 
                        (uint)(variable.ControlType == PipelineVariableControlType.Check ? 2 : 0));

                    y++;
                } catch {
                }
            }
            
            foreach(Widget widget in variable_widgets.Values) {
                if(widget is PipelineVariableComboBox) {
                    OnComboChanged(widget, EventArgs.Empty);
                } else if(widget is CheckButton) {
                    (widget as CheckButton).Toggle();
                }
            }

            table.Visible = y > 0;
        }

        private Widget BuildControl(PipelineVariable variable)
        {
            switch(variable.ControlType) {
                default:
                case PipelineVariableControlType.Text:
                    return new Entry();
                case PipelineVariableControlType.Slider:
                    return BuildSlider(variable);
                case PipelineVariableControlType.Combo:
                    return BuildCombo(variable);
                case PipelineVariableControlType.Check:
                    return BuildCheck(variable);
            }
        }
        
        private Widget BuildCheck(PipelineVariable variable)
        {
            CheckButton check = new CheckButton(variable.Name);

            check.Toggled += delegate {
                variable.CurrentValue = Convert.ToString(check.Active ? 1 : 0);
                
                for(int i = 0; i < variable.Enables.Length; i++) {
                   if(variable_widgets.ContainsKey(variable.Enables[i])) {
                       variable_widgets[variable.Enables[i]].Visible = check.Active;
                       variable_widgets[".label." + variable.Enables[i]].Visible = check.Active;
                   }
                }
                
                for(int i = 0; i < variable.Disables.Length; i++) {
                   if(variable_widgets.ContainsKey(variable.Disables[i])) {
                       variable_widgets[variable.Disables[i]].Visible = !check.Active;
                       variable_widgets[".label." + variable.Disables[i]].Visible = !check.Active;
                   }
                }
            };
            
            check.Active = ((int)variable.CurrentValueNumeric.Value) != 0; 
            check.Show();
            return check;
        }

        private Widget BuildSlider(PipelineVariable variable)
        {
            if(variable.StepValue <= 0.0) {
                return null;
            }
            
            HBox box = new HBox();
        
            HScale slider = new HScale(variable.MinValue, variable.MaxValue, variable.StepValue);
            slider.DrawValue = true;
            slider.Digits = variable.StepPrecision;
            
            if(variable.DefaultValueNumeric != null) {
                slider.Value = (double)variable.DefaultValueNumeric;
            }
            
            if(variable.CurrentValueNumeric != null) {
                slider.Value = (double)variable.CurrentValueNumeric;
            }
            
            slider.ChangeValue += delegate {
                variable.CurrentValue = slider.Value.ToString();
            };
            
            if(variable.MinLabel != null) {
                Label min_label = new Label();
                min_label.Yalign = 0.9f;
                min_label.Markup = String.Format("<small>{0}</small>", GLib.Markup.EscapeText(variable.MinLabel));
                box.PackStart(min_label, false, false, 0);
                box.Spacing = 5;
            }
            
            box.PackStart(slider, true, true, 0);
            
            if(variable.MaxLabel != null) {
                Label max_label = new Label();
                max_label.Yalign = 0.9f;
                max_label.Markup = String.Format("<small>{0}</small>", GLib.Markup.EscapeText(variable.MaxLabel));
                box.PackStart(max_label, false, false, 0);
                box.Spacing = 5;
            }
            
            box.ShowAll();
            
            return box;
        }

        private TreeIter ComboAppend(ListStore model, PipelineVariable variable, string display, string value)
        {
            if(variable.Unit != null) {
                return model.AppendValues(String.Format("{0} {1}", display, variable.Unit), value);
            } else {
                return model.AppendValues(display, value);
            }
        }

        private Widget BuildCombo(PipelineVariable variable)
        {
            ListStore model = new ListStore(typeof(string), typeof(string));
            PipelineVariableComboBox box = new PipelineVariableComboBox(variable, model);
            TreeIter active_iter = TreeIter.Zero;

            box.Changed += OnComboChanged;

            if(variable.PossibleValuesCount > 0) {
                foreach(string key in variable.PossibleValuesKeys) {
                    TreeIter iter = ComboAppend(model, variable, variable.PossibleValues[key].Display, key);
                
                    if(variable.CurrentValue == key || (active_iter.Equals(TreeIter.Zero) && 
                        variable.DefaultValue == key)) {
                        active_iter = iter;
                    }
                }
            } else {
                double min = variable.MinValue;
                double max = variable.MaxValue;
                double step = variable.StepValue;
                double current = min;

                for(; current <= max; current += step) {
                    ComboAppend(model, variable, current.ToString(), current.ToString());
                }
            }
            
            if(active_iter.Equals(TreeIter.Zero)) {
                for(int i = 0, n = model.IterNChildren(); i < n; i++) {
                    TreeIter iter;
                    if(model.IterNthChild(out iter, i)) {
                        string value = (string)model.GetValue(iter, 1);
                        if(value == variable.CurrentValue) {
                            active_iter = iter;
                            break;
                        }
                    }
                }
            }
            
            CellRendererText text_renderer = new CellRendererText();
            box.PackStart(text_renderer, true);
            box.AddAttribute(text_renderer, "text", 0);

            box.Model = model;
            
            if(active_iter.Equals(TreeIter.Zero)) {
                if(model.IterNthChild(out active_iter, 0)) {
                    box.SetActiveIter(active_iter);
                }
            } else {
                box.SetActiveIter(active_iter);
            }
            
            return box;
        }
        
        private void OnComboChanged(object o, EventArgs args)
        {
            if(!(o is PipelineVariableComboBox)) {
                return;
            }
            
            PipelineVariableComboBox box = o as PipelineVariableComboBox;
            PipelineVariable variable = box.Variable;
            ListStore model = box.Store;
            TreeIter selected_iter = TreeIter.Zero;
            
            if(box.GetActiveIter(out selected_iter)) {
                variable.CurrentValue = (string)model.GetValue(selected_iter, 1);
                
                if(variable.PossibleValuesCount > 0 && 
                    variable.PossibleValues.ContainsKey(variable.CurrentValue)) {
                    PipelineVariable.PossibleValue possible_value = variable.PossibleValues[variable.CurrentValue];
                    if(possible_value.Disables != null) {
                        for(int i = 0; i < possible_value.Disables.Length; i++) {
                            if(variable_widgets.ContainsKey(possible_value.Disables[i])) {
                                variable_widgets[possible_value.Disables[i]].Visible = false;
                                variable_widgets[".label." + possible_value.Disables[i]].Visible = false;
                            }
                        }
                    }
                    
                    if(possible_value.Enables != null) {
                        for(int i = 0; i < possible_value.Enables.Length; i++) {
                            if(variable_widgets.ContainsKey(possible_value.Enables[i])) {
                                variable_widgets[possible_value.Enables[i]].Visible = true;
                                variable_widgets[".label." + possible_value.Enables[i]].Visible = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
