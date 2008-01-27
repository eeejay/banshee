/***************************************************************************
 *  GstMisc.cs
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
 *  Written by Aaron Bockover <abock@gnome.org>
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Banshee.Base;

using SExpEngine;

namespace Banshee.Gstreamer
{
    public static class Utilities
    {
        [DllImport("libbanshee")]
        private static extern bool gstreamer_test_pipeline(IntPtr pipeline);
        
        public static bool TestPipeline(string pipeline)
        {
            if(pipeline == null || pipeline == String.Empty) {
                return false;
            }
        
            IntPtr pipeline_ptr = GLib.Marshaller.StringToPtrGStrdup(pipeline);
            
            if(pipeline_ptr == IntPtr.Zero) {
                return false;
            }
            
            try {
                return gstreamer_test_pipeline(pipeline_ptr);
            } finally {
                GLib.Marshaller.Free(pipeline_ptr);
            }
        }
        
        [DllImport("libbanshee")]
        private static extern void gstreamer_initialize();
        
        public static void Initialize()
        {
            gstreamer_initialize();
        }
        
        public static TreeNode SExprTestElement(EvaluatorBase evaluator, TreeNode [] args)
        {
            if(args.Length != 1) {
                throw new ArgumentException("gst-test-element accepts one argument");
            }
            
            TreeNode arg = evaluator.Evaluate(args[0]);
            if(!(arg is StringLiteral)) {
                throw new ArgumentException("gst-test-element requires a string argument");
            }
            
            StringLiteral element_node = (StringLiteral)arg;
            return new BooleanLiteral(TestPipeline(element_node.Value));
        }
        
        public static TreeNode SExprConstructPipeline(EvaluatorBase evaluator, TreeNode [] args)
        {
            StringBuilder builder = new StringBuilder();
            List<string> elements = new List<string>();
            
            for(int i = 0; i < args.Length; i++) {
                TreeNode node = evaluator.Evaluate(args[i]);
                if(!(node is LiteralNodeBase)) {
                    throw new ArgumentException("node must evaluate to a literal");
                }
                
                string value = node.ToString().Trim();
                
                if(value.Length == 0) {
                    continue;
                }
                
                elements.Add(value);
            }
            
            for(int i = 0; i < elements.Count; i++) {
                builder.Append(elements[i]);
                
                if(i < elements.Count - 1) {
                    builder.Append(" ! ");
                }
            }
            
            return new StringLiteral(builder.ToString());
        }
        
        public static TreeNode SExprConstructElement(EvaluatorBase evaluator, TreeNode [] args)
        {
            return SExprConstructPipelinePart(evaluator, args, true);
        }
        
        public static TreeNode SExprConstructCaps(EvaluatorBase evaluator, TreeNode [] args)
        {
            return SExprConstructPipelinePart(evaluator, args, false);
        }
        
        private static TreeNode SExprConstructPipelinePart(EvaluatorBase evaluator, TreeNode [] args, bool element)
        {
            StringBuilder builder = new StringBuilder();
            
            TreeNode list = new TreeNode();
            foreach(TreeNode arg in args) {
                list.AddChild(evaluator.Evaluate(arg));
            }
            
            list = list.Flatten();
            
            for(int i = 0; i < list.ChildCount; i++) {
                TreeNode node = list.Children[i];
                
                string value = node.ToString().Trim();
                
                builder.Append(value);
                
                if(i == 0) {
                    if(list.ChildCount > 1) {
                        builder.Append(element ? " " : ",");
                    }
                    
                    continue;
                } else if(i % 2 == 1) {
                    builder.Append("=");
                } else if(i < list.ChildCount - 1) {
                    builder.Append(element ? " " : ",");
                }
            }
            
            return new StringLiteral(builder.ToString());
        }
    }
}

