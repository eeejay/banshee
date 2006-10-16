/***************************************************************************
 *  ProfileConfigurationDialog.cs
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
using System.Collections.Generic;
using Mono.Unix;

using Gtk;

using Banshee.AudioProfiles;

namespace Banshee.AudioProfiles.Gui
{
    public class ProfileConfigurationDialog : Gtk.Dialog
    {
        private Profile profile;
    
        private Label header_label = new Label();
        private Label description_label = new Label();
        private Table normal_controls_table = new Table(1, 1, false);
        private Table advanced_controls_table = new Table(1, 1, false);
        private Expander advanced_expander = new Expander(Catalog.GetString("Advanced"));

        public ProfileConfigurationDialog(Profile profile) : base()
        {
            this.profile = profile;
        
            HasSeparator = false;
            BorderWidth = 5;
            Resizable = false;
            
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);
            
            Button button = new Button(Stock.Close);
            button.CanDefault = true;
            button.Show();

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
            description_label.Wrap = true;
            description_label.Xalign = 0.0f;
            
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

            VBox.PackStart(box, false, false, 0);
        }

        private void LoadProfile()
        {
            Title = Catalog.GetString(String.Format("Configuring {0}", profile.Name));
            header_label.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(profile.Name));
            description_label.Text = profile.Description;

            LoadControlTable(normal_controls_table, false);
            LoadControlTable(advanced_controls_table, true);
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
                label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(variable.Name));
                label.Xalign = 0.0f;

                try {
                    Widget control = BuildControl(variable);
                    if(control == null) {
                        throw new ApplicationException("Control could not be created");
                    }

                    control.Show();
                
                    table.Resize(y + 1, 2);
                
                    table.Attach(label, 0, 1, y, y + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
                    table.Attach(control, 1, 2, y, y + 1, 
                        control is ComboBox ? AttachOptions.Fill : AttachOptions.Fill | AttachOptions.Expand, 
                        AttachOptions.Fill, 0, 0);

                    y++;
                } catch {
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
            }
        }

        private Widget BuildSlider(PipelineVariable variable)
        {
            if(variable.StepValue <= 0.0) {
                return null;
            }
        
            HScale slider = new HScale(variable.MinValue, variable.MaxValue, variable.StepValue);
            if(variable.DefaultValueNumeric != null) {
                slider.Value = (double)variable.DefaultValueNumeric;
            }
            
            if(variable.CurrentValueNumeric != null) {
                slider.Value = (double)variable.CurrentValueNumeric;
            }
            
            slider.ChangeValue += delegate {
                variable.CurrentValue = slider.Value.ToString();
            };
            return slider;
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
            ComboBox box = new ComboBox();
            ListStore model = new ListStore(typeof(string), typeof(string));
            TreeIter active_iter = TreeIter.Zero;

            if(variable.PossibleValuesCount > 0) {
                foreach(KeyValuePair<string, string> value in variable.PossibleValues) {
                    TreeIter iter = ComboAppend(model, variable, value.Value, value.Key);
                
                    if(variable.CurrentValue == value.Key || (active_iter.Equals(TreeIter.Zero) && 
                        variable.DefaultValue == value.Key)) {
                        active_iter = iter;
                    }
                }
            } else {
                double min = variable.MinValue;
                double max = variable.MaxValue;
                double step = variable.StepValue;
                double current = min;

                TreeIter iter;
                
                if(variable.HasStepExpression) {
                    for(;;) {
                        current = variable.EvaluateStepExpression(current);
                        iter = ComboAppend(model, variable, current.ToString(), current.ToString());
                        if(current >= max) {
                            break;
                        }
                    }
                } else {
                    for(; current <= max; current += step) {
                        iter = ComboAppend(model, variable, current.ToString(), current.ToString());
                    }
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
            
            box.Changed += delegate {
                TreeIter selected_iter = TreeIter.Zero;
                if(box.GetActiveIter(out selected_iter)) {
                    variable.CurrentValue = (string)model.GetValue(selected_iter, 1);
                }
            };

            return box;
        }
    }
}
