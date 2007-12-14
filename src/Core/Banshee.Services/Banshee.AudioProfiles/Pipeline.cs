/***************************************************************************
 *  Pipeline.cs
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
using System.Text.RegularExpressions;

using SExpEngine;
using Banshee.Base;

namespace Banshee.AudioProfiles
{
    public class Pipeline : IEnumerable<PipelineVariable>
    {
        public struct Process : IComparable<Process>
        {
            public static readonly Process Zero;
        
            private string id;
            private int order;
            private string pipeline;
            
            public Process(string id, int order, string pipeline)
            {
                this.id = id;
                this.order = order;
                this.pipeline = pipeline;
            }
            
            public int CompareTo(Process process)
            {
                int id_cmp = id.CompareTo(process.ID);
                return id_cmp != 0 ? id_cmp : order.CompareTo(process.Order);
            }
            
            public string ID {
                get { return id; }
            }
            
            public int Order { 
                get { return order; }
            }
            
            public string Pipeline {
                get { return pipeline; }
            }
        }
    
        private static Dictionary<string, SExpFunctionHandler> sexpr_functions = 
            new Dictionary<string, SExpFunctionHandler>();
            
        private List<PipelineVariable> variables = new List<PipelineVariable>();
        private Dictionary<string, string> processes = new Dictionary<string, string>();
        private List<Process> processes_pending = new List<Process>();
        
        internal Pipeline(ProfileManager manager, XmlNode node)
        {
            foreach(XmlNode process_node in node.SelectNodes("process")) {
                string process_id = process_node.Attributes["id"].Value.Trim();
                string process = process_node.InnerText.Trim();
                int order = 0;
                try {
                    XmlAttribute order_attr = process_node.Attributes["order"];
                    if(order_attr != null) {
                        order = Convert.ToInt32(order_attr.Value);
                    }
                } catch {
                }
                
                processes_pending.Add(new Process(process_id, order, process));
            }

            foreach(XmlNode variable_node in node.SelectNodes("variable")) {
                try {
                    AddVariable(new PipelineVariable(variable_node));
                } catch {
                }
            }

            foreach(XmlNode preset_variable_node in node.SelectNodes("preset-variable")) {
                try {
                    string preset_id = preset_variable_node.Attributes["id"].Value.Trim();
                    AddVariable(manager.GetPresetPipelineVariableById(preset_id));
                } catch {
                }
            }
        }
        
        public static void AddSExprFunction(string name, SExpFunctionHandler handler)
        {
            if(!sexpr_functions.ContainsKey(name)) { 
                sexpr_functions.Add(name, handler);
            }
        }
        
        public string CompileProcess(Process process)
        {
            return CompileProcess(process.Pipeline, process.ID);
        }
        
        public string CompileProcess(string process, string id)
        {
            Evaluator eval = new Evaluator();
            
            foreach(KeyValuePair<string, SExpFunctionHandler> function in sexpr_functions) {
                eval.RegisterFunction(function.Value, function.Key);
            }
            
            foreach(PipelineVariable variable in this) {
                double ?numeric = variable.CurrentValueNumeric;
                if(numeric != null) {
                    if(variable.CurrentValue.Contains(".")) {
                        eval.RegisterVariable(variable.ID, numeric.Value);
                    } else {
                        eval.RegisterVariable(variable.ID, (int)numeric.Value);
                    }
                } else {
                    eval.RegisterVariable(variable.ID, variable.CurrentValue);
                }
            }
            
            TreeNode result = eval.EvaluateString(process);
            if(eval.Success && result is StringLiteral) {
                return (result.Flatten() as StringLiteral).Value;
            } else if(!eval.Success && ApplicationContext.Debugging) {
                Console.WriteLine("Could not compile pipeline S-Expression for pipeline:");
                Console.WriteLine(process);
                Console.WriteLine("-----");
                Console.WriteLine(eval.ErrorMessage);
                Console.WriteLine("-----");
                Console.WriteLine("Stack Trace:");
            
                foreach(Exception e in eval.Exceptions) {
                    Console.WriteLine(e.Message);
                }
                
                Console.WriteLine("-----");
                Console.WriteLine("Expression Tree:");
                SExpEngine.TreeNode.DumpTree(eval.ExpressionTree);
            }
            
            return null;
        }
        
        public void AddProcess(Process process)
        {
            AddProcess(process.ID, process.Pipeline);
        }

        public void AddProcess(string id, string process)
        {
            if(processes.ContainsKey(id)) {
                throw new ApplicationException(String.Format("A pipeline process with ID '{0}' already exists in this profile", id));
            }

            processes.Add(id, process);
        }

        public void RemoveProcess(string id)
        {
            processes.Remove(id);
        }
        
        public string GetProcessById(string id)
        {
            if(processes.ContainsKey(id)) {
                return CompileProcess(processes[id], id);
            }
            
            throw new ApplicationException("No processes in pipeline");
        }
        
        public string GetProcessByIdOrDefault(string id)
        {
            if(processes.ContainsKey(id)) {
                return GetProcessById(id);
            } 
            
            return GetDefaultProcess();
        }
        
        public string GetDefaultProcess()
        {
            foreach(KeyValuePair<string, string> process in processes) {
                return CompileProcess(process.Value, process.Key);
            }
            
            throw new ApplicationException("No processes in pipeline");
        }
        
        public IList<Process> GetPendingProcessesById(string id)
        {
            List<Process> processes = new List<Process>();
            foreach(Process process in processes_pending) {
                if(process.ID == id) {
                    processes.Add(process);
                }
            }
            processes.Sort();
            return processes;
        }

        public void AddVariable(PipelineVariable variable)
        {
            if(variables.Contains(variable)) {
                throw new ApplicationException(String.Format("A variable with ID '{0}' already exists in this profile", variable.ID));
            }
            
            variables.Add(variable);
        }

        public void RemoveVariable(PipelineVariable variable)
        {
            variables.Remove(variable);
        }

        public IEnumerator<PipelineVariable> GetEnumerator()
        {
            return variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return variables.GetEnumerator();
        }

        public int ProcessCount {
            get { return processes.Count; }
        }

        public int VariableCount {
            get { return variables.Count; }
        }

        public IDictionary<string, string> Processes {
            get { return processes; }
        }
        
        public IList<PipelineVariable> Variables {
            get { return variables; }
        }

        public IEnumerable<KeyValuePair<string, string>> Configuration {
            get {
                foreach(PipelineVariable variable in Variables) {
                    yield return new KeyValuePair<string, string>(variable.ID, variable.CurrentValue);
                }
            }
        }
        
        public string this[string variableName] {
            set { 
                foreach(PipelineVariable variable in this) {
                    if(variable.ID == variableName) {
                        variable.CurrentValue = value;
                        break;
                    }
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
                
            builder.Append("\tProcesses:\n");
            foreach(KeyValuePair<string, string> process in processes) {
                builder.Append(String.Format("\t{0} = {1}\n", process.Key, process.Value));
            }

            builder.Append("\n\tVariables:\n");
            foreach(PipelineVariable variable in variables) {
                builder.Append(variable);
                builder.Append("\n");
            }

            return builder.ToString();
        }
    }
}
