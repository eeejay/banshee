//
// ListViewTestModule.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

using Hyena.Data;
using Hyena.Collections;
using Hyena.Gui;

using Selection = Hyena.Collections.Selection;

namespace Hyena.Data.Gui
{
    [TestModule ("List View")]
    public class ListViewTestModule : Window
    {
        private View view;
        private Model model;

        public ListViewTestModule () : base ("ListView")
        {
            WindowPosition = WindowPosition.Center;
            SetDefaultSize (800, 600);

            ScrolledWindow scroll = new ScrolledWindow ();
            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            view = new View ();
            model = new Model ();

            scroll.Add (view);
            Add (scroll);
            ShowAll ();

            view.SetModel (model);
        }

        private class View : ListView<ModelItem>
        {
            public View ()
            {
                ColumnController = new ColumnController ();
                ColumnController.AddRange (
                    new Column (String.Empty, new ColumnCellCheckBox ("F", true), 1),
                    new Column ("Apples", new ColumnCellText ("B", true), 1),
                    new Column ("Pears", new ColumnCellText ("C", true), 1),
                    new Column ("How Hot", new ColumnCellRating ("G", true), 1),
                    new Column ("Peaches", new ColumnCellText ("D", true), 1),
                    new Column ("Doodle", new ColumnCellDoodle ("E", true), 1),
                    new Column ("GUIDs!OMG", new ColumnCellText ("A", true), 1)
                );
            }
        }

        private class Model : IListModel<ModelItem>
        {
            private List<ModelItem> store = new List<ModelItem> ();
            private Selection selection = new Selection ();

            public event EventHandler Cleared;
            public event EventHandler Reloaded;

            public Model ()
            {
                Random random = new Random (0);
                for (int i = 0; i < 1000; i++) {
                    store.Add (new ModelItem (i, random));
                }
            }

            public void Clear ()
            {
            }

            public void Reload ()
            {
            }

            public int Count {
                get { return store.Count; }
            }

            public bool CanReorder {
                get { return false; }
            }

            public ModelItem this[int index] {
                get { return store[index]; }
            }

            public Selection Selection {
                get { return selection; }
            }
        }

        private class ModelItem
        {
            public ModelItem (int i, Random rand)
            {
                a = Guid.NewGuid ().ToString ();
                b = rand.Next (0, 255);
                c = rand.NextDouble ();
                d = String.Format ("Item {0}", i);
                e = new List<Gdk.Point> ();
                f = rand.Next (0, 1) == 1;
                g = rand.Next (0, 5);
            }

            string a; public string A { get { return a; } }
            int b;    public int    B { get { return b; } }
            double c; public double C { get { return c; } }
            string d; public string D { get { return d; } }
            List<Gdk.Point> e; public List<Gdk.Point> E { get { return e; } }
            bool f; public bool F { get { return f; } set { f = value; } }
            int g; public int G { get { return g; } set { g = value; } }
        }

        private class ColumnCellDoodle : ColumnCell, IInteractiveCell
        {
            private Random random = new Random ();
            private bool red = false;

            public ColumnCellDoodle (string property, bool expand) : base (property, expand)
            {
            }

            public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
            {
                red = !red;
                Cairo.Context cr = context.Context;
                cr.Rectangle (0, 0, cellWidth, cellHeight);
                cr.Color = CairoExtensions.RgbaToColor (red ? 0xff000099 : 0x00000099);
                cr.Fill ();

                List<Gdk.Point> points = Points;
                for (int i = 0, n = points.Count; i < n; i++) {
                    if (i == 0) {
                        cr.MoveTo (points[i].X, points[i].Y);
                    } else {
                        cr.LineTo (points[i].X, points[i].Y);
                    }
                }

                cr.Color = CairoExtensions.RgbToColor ((uint)random.Next (0xffffff));
                cr.LineWidth = 1;
                cr.Stroke ();
            }

            private object last_pressed_bound;

            public bool ButtonEvent (int x, int y, bool pressed, Gdk.EventButton evnt)
            {
                if (!pressed) {
                    last_pressed_bound = null;
                    return false;
                }

                last_pressed_bound = BoundObject;
                Points.Add (new Gdk.Point (x, y));
                return true;
            }

            public bool MotionEvent (int x, int y, Gdk.EventMotion evnt)
            {
                if (last_pressed_bound == BoundObject) {
                    Points.Add (new Gdk.Point (x, y));
                    return true;
                }

                return false;
            }

            public bool PointerLeaveEvent ()
            {
                last_pressed_bound = null;
                return true;
            }

            private List<Gdk.Point> Points {
                get { return (List<Gdk.Point>)BoundObject; }
            }
        }
    }
}
