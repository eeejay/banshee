/***************************************************************************
 *  PipelineVariable.cs
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
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Banshee.MediaProfiles
{
    public enum PipelineVariableControlType
    {
        Text,
        Slider,
        Combo,
        Check
    }

    public class PipelineVariable
    {
        public struct PossibleValue
        {
            public string Value;
            public string Display;
            public string [] Enables;
            public string [] Disables;

            public PossibleValue(string value, string display)
            {
                Value = value;
                Display = display;
                Enables = null;
                Disables = null;
            }

            public override string ToString()
            {
                return Display;
            }
        }

        private PipelineVariableControlType control_type;
        private string id;
        private string name;
        private string unit;
        private string default_value;
        private string current_value;
        private string min_label;
        private string max_label;
        private double min_value;
        private double max_value;
        private double step_value;
        private int step_precision;
        private string [] enables = new string[0];
        private string [] disables = new string[0];

        private Dictionary<string, PossibleValue> possible_values = new Dictionary<string, PossibleValue>();
        private List<string> possible_values_keys = new List<string>();
        private bool advanced;

        internal PipelineVariable(XmlNode node)
        {
            id = node.Attributes["id"].Value.Trim();
            name = Banshee.Base.Localization.SelectSingleNode(node, "name").InnerText.Trim();
            control_type = StringToControlType(node.SelectSingleNode("control-type").InnerText.Trim());

            XmlAttribute enables_attr = node.Attributes["enables"];
            if(enables_attr != null && enables_attr.Value != null) {
                string [] vars = enables_attr.Value.Split(',');
                if(vars != null && vars.Length > 0) {
                    enables = new string[vars.Length];
                    for(int i = 0; i < vars.Length; i++) {
                        enables[i] = vars[i].Trim();
                    }
                }
            }

            XmlAttribute disables_attr = node.Attributes["disables"];
            if(disables_attr != null && disables_attr.Value != null) {
                string [] vars = disables_attr.Value.Split(',');
                if(vars != null && vars.Length > 0) {
                    disables = new string[vars.Length];
                    for(int i = 0; i < vars.Length; i++) {
                        disables[i] = vars[i].Trim();
                    }
                }
            }

            try {
                XmlNode unit_node = node.SelectSingleNode("unit");
                if(unit_node != null) {
                    unit = node.SelectSingleNode("unit").InnerText.Trim();
                }
            } catch {
            }

            try {
                XmlNode advanced_node = node.SelectSingleNode("advanced");
                if(advanced_node != null) {
                    advanced = ParseAdvanced(advanced_node.InnerText);
                }
            } catch {
            }

            default_value = ReadValue(node, "default-value");
            min_value = ToDouble(ReadValue(node, "min-value"));
            max_value = ToDouble(ReadValue(node, "max-value"));
            min_label = ReadValue(node, "min-label", true);
            max_label = ReadValue(node, "max-label", true);

            string step_value_str = ReadValue(node, "step-value");
            if(step_value_str != null) {
                bool zeros = true;
                step_precision = step_value_str.IndexOf(".") + 1;

                for(int i = step_precision; i > 0 && i < step_value_str.Length; i++) {
                    if(step_value_str[i] != '0') {
                        zeros = false;
                        break;
                    }
                }

                step_precision = zeros ? 0 : step_value_str.Length - step_precision;
                step_value = ToDouble(step_value_str);
            }

            if(default_value != null && default_value != String.Empty && (current_value == null ||
                current_value == String.Empty)) {
                current_value = default_value;
            }

            foreach(XmlNode possible_value_node in Banshee.Base.Localization.SelectNodes(node, "possible-values/value")) {
                try {
                    string value = possible_value_node.Attributes["value"].Value.Trim();
                    string display = possible_value_node.InnerText.Trim();

                    PossibleValue possible_value = new PossibleValue(value, display);

                    XmlAttribute attr = possible_value_node.Attributes["enables"];
                    if(attr != null && attr.Value != null) {
                        string [] vars = attr.Value.Split(',');
                        if(vars != null && vars.Length > 0) {
                            possible_value.Enables = new string[vars.Length];
                            for(int i = 0; i < vars.Length; i++) {
                                possible_value.Enables[i] = vars[i].Trim();
                            }
                        }
                    }

                    attr = possible_value_node.Attributes["disables"];
                    if(attr != null && attr.Value != null) {
                        string [] vars = attr.Value.Split(',');
                        if(vars != null && vars.Length > 0) {
                            possible_value.Disables = new string[vars.Length];
                            for(int i = 0; i < vars.Length; i++) {
                                possible_value.Disables[i] = vars[i].Trim();
                            }
                        }
                    }

                    if(!possible_values.ContainsKey(value)) {
                        possible_values.Add(value, possible_value);
                        possible_values_keys.Add(value);
                    }
                } catch {
                }
            }
        }

        private static string ReadValue(XmlNode node, string name)
        {
            return ReadValue(node, name, false);
        }

        private static string ReadValue(XmlNode node, string name, bool localize)
        {
            try {
                XmlNode str_node = localize ?
                    Banshee.Base.Localization.SelectSingleNode(node, name) :
                    node.SelectSingleNode(name);

                if(str_node == null) {
                    return null;
                }

                string str = str_node.InnerText.Trim();
                return str == String.Empty ? null : str;
            } catch {
            }

            return null;
        }

        private static double ToDouble(string str)
        {
            try {
                return Convert.ToDouble(str, MediaProfileManager.CultureInfo);
            } catch {
            }

            return 0.0;
        }

        private static PipelineVariableControlType StringToControlType(string str)
        {
            switch(str.ToLower()) {
                case "combo": return PipelineVariableControlType.Combo;
                case "slider": return PipelineVariableControlType.Slider;
                case "check": return PipelineVariableControlType.Check;
                case "text":
                default:
                    return PipelineVariableControlType.Text;
            }
        }

        internal static bool ParseAdvanced(string advanced)
        {
            if(advanced == null || advanced.Trim() == String.Empty) {
                return true;
            }

            switch(advanced.Trim().ToLower()) {
                case "true":
                case "yes":
                case "1":
                case "advanced":
                    return true;
                default:
                    return false;
            }
        }

        public string Id {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Unit {
            get { return unit; }
            set { unit = value; }
        }

        public PipelineVariableControlType ControlType {
            get { return control_type; }
            set { control_type = value; }
        }

        public bool Advanced {
            get { return advanced; }
            set { advanced = value; }
        }

        public string DefaultValue {
            get { return default_value; }
            set { default_value = value; }
        }

        public string CurrentValue {
            get { return current_value; }
            set { current_value = value; }
        }

        public string MinLabel {
            get { return min_label; }
            set { min_label = value; }
        }

        public string MaxLabel {
            get { return max_label; }
            set { max_label = value; }
        }

        public int StepPrecision {
            get { return step_precision; }
        }

        public string [] Enables {
            get { return enables; }
        }

        public string [] Disables {
            get { return disables; }
        }

        public double? DefaultValueNumeric {
            get {
                try {
                    return DefaultValue == null || DefaultValue == String.Empty ?
                        (double?)null :
                        Convert.ToDouble(DefaultValue, MediaProfileManager.CultureInfo);
                } catch {
                    return null;
                }
            }

            set { DefaultValue = Convert.ToString(value, MediaProfileManager.CultureInfo); }
        }

        public double? CurrentValueNumeric {
            get {
                try {
                    return CurrentValue == null || CurrentValue == String.Empty ?
                        (double?)null :
                        Convert.ToDouble(CurrentValue, MediaProfileManager.CultureInfo);
                } catch {
                    return null;
                }
            }

            set { CurrentValue = Convert.ToString(value, MediaProfileManager.CultureInfo); }
        }

        public double MinValue {
            get { return min_value; }
            set { min_value = value; }
        }

        public double MaxValue {
            get { return max_value; }
            set { max_value = value; }
        }

        public double StepValue {
            get { return step_value; }
            set { step_value = value; }
        }

        public IDictionary<string, PossibleValue> PossibleValues {
            get { return possible_values; }
        }

        public ICollection<string> PossibleValuesKeys {
            get { return possible_values_keys; }
        }

        public int PossibleValuesCount {
            get { return possible_values.Count; }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(String.Format("\tID            = {0}\n", Id));
            builder.Append(String.Format("\tName          = {0}\n", Name));
            builder.Append(String.Format("\tControl Type  = {0}\n", ControlType));
            builder.Append(String.Format("\tAdvanced      = {0}\n", Advanced));
            builder.Append(String.Format("\tDefault Value = {0}\n", DefaultValue));
            builder.Append(String.Format("\tCurrent Value = {0}\n", CurrentValue));
            builder.Append(String.Format("\tMin Value     = {0}\n", MinValue));
            builder.Append(String.Format("\tMax Value     = {0}\n", MaxValue));
            builder.Append(String.Format("\tStep Value    = {0}\n", StepValue));
            builder.Append(String.Format("\tPossible Values:\n"));

            foreach(KeyValuePair<string, PossibleValue> value in PossibleValues) {
                builder.Append(String.Format("\t\t{0} => {1}\n", value.Value, value.Key));
            }

            builder.Append("\n");

            return builder.ToString();
        }
    }
}
