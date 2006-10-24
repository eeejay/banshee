/***************************************************************************
 *  BooBuddyShell.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 * 
 *  Based on ShellTextView.boo in the MonoDevelop Boo Binding
 *  originally authored by  Peter Johanson <latexer@gentoo.org>
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
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Gtk;

using Boo.Lang.Interpreter;
using Boo.Lang.Compiler;
using Boo.Lang.Compiler.TypeSystem;

namespace BooBuddy
{
    public delegate void ProcessInputHandler(object o, ProcessInputArgs args);
    
    public class ProcessInputArgs : EventArgs
    {
        private string input;
        
        public ProcessInputArgs(string input)
        {
            this.input = input;
        }
        
        public string Input {
            get { return input; }
        }
    }

    public class BooBuddyShell : TextView
    {
        private string script_lines = String.Empty;
        private string block_text = String.Empty;
        
        private bool in_block;
        private bool auto_indent = true;
        
        private Stack<string> command_history_past = new Stack<string>();
        private Stack<string> command_history_future = new Stack<string>();
        
        private TextMark end_of_last_processing;
        
        private InteractiveInterpreter interpreter;
        
        public event ProcessInputHandler ProcessInput;
        
        public BooBuddyShell() : base()
        {
            WrapMode = WrapMode.Word;
            
            TextTag freeze_tag = new TextTag("Freezer");
            freeze_tag.Editable = false;
            Buffer.TagTable.Add(freeze_tag);
            
            TextTag prompt_tag = new TextTag("Prompt");
            prompt_tag.Foreground = "blue";
            prompt_tag.Background = "#f8f8f8";
            prompt_tag.Weight = Pango.Weight.Bold;
            Buffer.TagTable.Add(prompt_tag);
            
            TextTag prompt_ml_tag = new TextTag("PromptML");
            prompt_ml_tag.Foreground = "orange";
            prompt_ml_tag.Background = "#f8f8f8";
            prompt_ml_tag.Weight = Pango.Weight.Bold;
            Buffer.TagTable.Add(prompt_ml_tag);
            
            TextTag error_tag = new TextTag("Error");
            error_tag.Foreground = "red";
            Buffer.TagTable.Add(error_tag);
            
            TextTag stdout_tag = new TextTag("Stdout");
            stdout_tag.Foreground = "#006600";
            Buffer.TagTable.Add(stdout_tag);
            
            Pango.FontDescription font_description = new Pango.FontDescription();
            font_description.Family = "Monospace";
            ModifyFont(font_description);
            
            Prompt(false);
        }
        
        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            if(Cursor.Compare(InputLineBegin) < 0) {
                Buffer.MoveMark(Buffer.SelectionBound, InputLineEnd);
                Buffer.MoveMark(Buffer.InsertMark, InputLineEnd);
            }
        
            if(evnt.Key == Gdk.Key.Return) {
                if(in_block) {
                    if(InputLine == String.Empty) {
                        OnProcessInput(block_text);
                        block_text = String.Empty;
                        in_block = false;
                    } else {
                        block_text += "\n" + InputLine;
                        string whitespace = String.Empty;
                        if(auto_indent) {
                            whitespace = Regex.Replace(InputLine, @"^(\s+).*", "$1");
                            if(InputLineStartsBlock) {
                                whitespace += "\t";
                            }
                        }
                        
                        Prompt(true, true);
                        
                        if(auto_indent) {
                            InputLine += whitespace;
                        }
                    }
                } else {
                    if(InputLineStartsBlock) {
                        in_block = true;
                        block_text = InputLine;
                        Prompt(true, true);
                        
                        if(auto_indent) {
                            InputLine += "\t";
                        }
                        
                        return true;
                    } else if(InputLine != String.Empty) {
                        while(command_history_future.Count > 1) {
                            command_history_past.Push(command_history_future.Pop());
                        }
                        
                        command_history_future.Clear();
                        command_history_past.Push(InputLine);
                        
                        if(script_lines == String.Empty) {
                            script_lines += InputLine;
                        } else {
                            script_lines += "\n" + InputLine;
                        }
                        
                        OnProcessInput(InputLine);
                    }
                }
                
                return true;
            } else if(evnt.Key == Gdk.Key.Up) {
                if(!in_block && command_history_past.Count > 0) {
                    if(command_history_future.Count == 0) {
                        command_history_future.Push(InputLine);
                    } else {
                        if(command_history_past.Count == 1) {
                            return true;
                        }
                        command_history_future.Push(command_history_past.Pop());
                    }
                    
                    InputLine = command_history_past.Peek();
                }
                
                return true;
            } else if(evnt.Key == Gdk.Key.Down) {
                if(!in_block && command_history_future.Count > 0) {
                    if(command_history_future.Count == 1) {
                        InputLine = command_history_future.Pop();
                    } else {
                        command_history_past.Push(command_history_future.Pop());
                        InputLine = command_history_past.Peek();
                    }
                }
                
                return true;
            } else if(evnt.Key == Gdk.Key.Left) {
                if(Cursor.Compare(InputLineBegin) <= 0) {
                    return true;
                }
            } else if(evnt.Key == Gdk.Key.Home) {
                Buffer.MoveMark(Buffer.InsertMark, InputLineBegin);
                if((evnt.State & Gdk.ModifierType.ShiftMask) == evnt.State) {
                    Buffer.MoveMark(Buffer.SelectionBound, InputLineBegin);
                }
                return true;
            } /*else if(evnt.Key == Gdk.Key.period) {
                string lookup_fragment = InputLine.Substring(0, Cursor.LineIndex - 4);
                string [] fragment_parts = Regex.Split(lookup_fragment, @"[ \t]+");
                lookup_fragment = fragment_parts[fragment_parts.Length - 1];
                
                Console.WriteLine(interpreter.Lookup(lookup_fragment));
                foreach(IEntity entity in interpreter.SuggestCodeCompletion(lookup_fragment)) {
                    Console.WriteLine(entity.FullName);
                }
            }*/
            
            return base.OnKeyPressEvent(evnt);
        }
        
        private void Prompt(bool newline)
        {
            Prompt(newline, false);
        }
        
        private void Prompt(bool newline, bool multiline)
        {
            TextIter end_iter = Buffer.EndIter;
            
            if(newline) {
                Buffer.Insert(ref end_iter, "\n");
            }
            
            Buffer.Insert(ref end_iter, multiline ? "... " : ">>> ");
            
            Buffer.PlaceCursor(Buffer.EndIter);
            ScrollMarkOnscreen(Buffer.InsertMark);
            
            end_of_last_processing = Buffer.CreateMark(null, Buffer.EndIter, true);
            Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
            
            TextIter prompt_start_iter = InputLineBegin;
            prompt_start_iter.LineIndex -= 4;
            
            TextIter prompt_end_iter = InputLineBegin;
            prompt_end_iter.LineIndex -= 1;
            
            Buffer.ApplyTag(Buffer.TagTable.Lookup(multiline ? "PromptML" : "Prompt"), 
                prompt_start_iter, prompt_end_iter);
        }

        protected virtual void OnProcessInput(string input)
        {
            ProcessInputHandler handler = ProcessInput;
            if(handler != null) {
                handler(this, new ProcessInputArgs(input));
            }
        }

        public void SetResult(InterpreterResult result)
        {
            TextIter end_iter = Buffer.EndIter;
            
            StringBuilder builder = new StringBuilder();
            if(result.Errors.Count > 0) {
                foreach(string error in result.Errors) {
                    builder.Append(error + "\n");
                }
            } else if(result.Message == null) {
                Prompt(true);
                return;
            } else {
                builder.Append(result.Message);
            }
            
            string str_result = builder.ToString().Trim();
            Buffer.Insert(ref end_iter, "\n" + str_result);
            
            TextIter start_iter = end_iter;
            start_iter.Offset -= str_result.Length;
            Buffer.ApplyTag(Buffer.TagTable.Lookup(result.Errors.Count > 0 ? "Error" : "Stdout"), 
                start_iter, end_iter);
            
            Prompt(true);
        }
        
        private TextIter InputLineBegin {
            get { return Buffer.GetIterAtMark(end_of_last_processing); }
        }
        
        private TextIter InputLineEnd {
            get { return Buffer.EndIter; }
        }
        
        private TextIter Cursor {
            get { return Buffer.GetIterAtMark(Buffer.InsertMark); }
        }

        private string InputLine {
            get { return Buffer.GetText(InputLineBegin, InputLineEnd, false); }
            set {
                TextIter start = InputLineBegin;
                TextIter end = InputLineEnd;
                Buffer.Delete(ref start, ref end);
                start = InputLineBegin;
                Buffer.Insert(ref start, value);
            }
        }
        
        private bool InputLineStartsBlock {
            get {
                string line = InputLine.Trim();
                
                if(line.Length > 0) {
                    return line.Substring(line.Length - 1) == ":";
                } 
                
                return false;
            }
        }
        
        public string Script {
            get { return script_lines; }
        }
        
        public InteractiveInterpreter Interpreter {
            get { return interpreter; }
            set { interpreter = value; }
        }
            
    }
}
