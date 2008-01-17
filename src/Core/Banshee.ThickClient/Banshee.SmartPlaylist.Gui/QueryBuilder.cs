/***************************************************************************
 *  QueryBuilder.cs
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
using GLib;
using Gtk;
using System.Collections;

using System.Text.RegularExpressions;

using Mono.Unix;

namespace Banshee.SmartPlaylist
{
    public static class ComboBoxUtil
    {
        public static string GetActiveString(ComboBox box)
        {
            TreeIter iter;
            if(!box.GetActiveIter(out iter))
                return null;
                
            return (string)box.Model.GetValue(iter, 0);
        }

        public static bool SetActiveString(ComboBox box, string val)
        {
            TreeIter iter;
            if (!box.Model.GetIterFirst(out iter))
                return false;

            do {
                if (box.Model.GetValue (iter, 0) as string == val) {
                    box.SetActiveIter(iter);
                    return true;
                }
            } while (box.Model.IterNext (ref iter));

            return false;
        }
    }

    // --- Base QueryMatch Class --- 

    public abstract class QueryMatch
    {
        public string Column;
        public int Op;

        public abstract string Value1 {
            get; set;
        }

        public abstract string Value2 {
            get; set;
        }
        
        public abstract string FilterValues();
        
        public abstract Widget DisplayWidget 
        {
            get;
        }
        
        public abstract QueryFilter [] ValidFilters {
            get;
        }

        public virtual string SqlColumn {
            get { return Column; }
        }

        public QueryFilter Filter {
            get {
                if (Op < 0 || Op > ValidFilters.Length) {
                    Op = 0;
                }
                return ValidFilters [Op];
            }
        }

        protected static HBox BuildRangeBox(Widget a, Widget b)
        {
            HBox box = new HBox();
            box.Spacing = 5;
            
            a.Show();
            box.PackStart(a, false, false, 0);
            
            Label label = new Label(Catalog.GetString("to"));
            label.Show();
            box.PackStart(label, false, false, 0);
            
            b.Show();
            box.PackStart(b, false, false, 0);
            
            box.Show();
            
            return box;
        }

        protected static string EscapeQuotes (string v)
        {
            return v == null ? String.Empty : v.Replace("'", "''");
        }
    }
    
    
    // --- Base QueryBuilderModel Class --- 
    
    public abstract class QueryBuilderModel : IEnumerable
    {
        private Hashtable fieldsMap;
        private Hashtable columnLookup;
        private Hashtable nameLookup;
        private Hashtable orderMap;
        private Hashtable mapOrder;

        private ArrayList fields = new ArrayList();
        private ArrayList orders = new ArrayList();
        
        public QueryBuilderModel()
        {
            fieldsMap = new Hashtable();
            columnLookup = new Hashtable();
            nameLookup = new Hashtable();
            orderMap = new Hashtable();
            mapOrder = new Hashtable();
        }
        
        public Type this [string index] 
        {
            get {
                return (Type)fieldsMap[index];
            }
        }
        
        public IEnumerator GetEnumerator()
        {
            return fields.GetEnumerator();
        }
        
        public void AddField(string name, string column, Type matchType)
        {
            fields.Add (name);
            fieldsMap[name] = matchType;
            columnLookup[name] = column;
            nameLookup[column] = name;
            fields.Sort();
        }
        
        public void AddOrder(string name, string map)
        {
            orders.Add (name);
            orderMap[name] = map;
            mapOrder[map] = name;
            orders.Sort();
        }
        
        public string GetOrder(string name)
        {
            return (string)orderMap[name];
        }

        public string GetOrderName(string map)
        {
            return (string)mapOrder[map];
        }
        
        public string GetColumn(string name)
        {
            return (string)columnLookup[name];
        }

        public string GetName(string col)
        {
            return (string)nameLookup[col];
        }
        
        public abstract string [] LimitCriteria 
        {
            get;
        }
        
        public ICollection OrderCriteria 
        {
            get {
                return orders;
            }
        }
    }
    
    // --- Query Builder Widgets

    public class QueryBuilderMatchRow : HBox
    {
        private VBox widgetBox;
        private ComboBox fieldBox, opBox;
        private QueryBuilderModel model;
        private QueryMatch match;
        private Button buttonAdd;
        private Button buttonRemove;
        
        public event EventHandler AddRequest;
        public event EventHandler RemoveRequest;
        
        public QueryBuilderMatchRow(QueryBuilderModel model) : base()
        {
            this.model = model;
        
            Spacing = 5;
            
            fieldBox = ComboBox.NewText();
            fieldBox.Changed += OnFieldComboBoxChanged;
            PackStart(fieldBox, false, false, 0);
            fieldBox.Show();
            
            opBox = ComboBox.NewText();
            opBox.Changed += OnOpComboBoxChanged;
            PackStart(opBox, false, false, 0);
            opBox.Show();
            
            widgetBox = new VBox();
            widgetBox.Show();
            PackStart(widgetBox, false, false, 0);
            
            foreach(string fieldName in model) {
                fieldBox.AppendText(fieldName);
            }
            
            Select(0);
            
            Image imageRemove = new Image("gtk-remove", IconSize.Button);
            buttonRemove = new Button(imageRemove);
            buttonRemove.Relief = ReliefStyle.None;
            buttonRemove.Show();
            buttonRemove.Clicked += OnButtonRemoveClicked;
            imageRemove.Show();
            PackEnd(buttonRemove, false, false, 0);
            
            Image imageAdd = new Image("gtk-add", IconSize.Button);
            buttonAdd = new Button(imageAdd);
            buttonAdd.Relief = ReliefStyle.None;
            buttonAdd.Show();
            buttonAdd.Clicked += OnButtonAddClicked;
            imageAdd.Show();
            PackEnd(buttonAdd, false, false, 0);
        }
        
        private void Select(int index)
        {
            TreeIter iter;
            
            if(!fieldBox.Model.IterNthChild(out iter, index))
                return;

            fieldBox.SetActiveIter(iter);
        }
        
        private void Select(TreeIter iter)
        {
            string fieldName = (string)fieldBox.Model.GetValue(iter, 0);
            
            Type matchType = model[fieldName];
            match = Activator.CreateInstance(matchType) as QueryMatch;
            
            while(opBox.Model.IterNChildren() > 0)
                opBox.RemoveText(0);

            foreach(QueryFilter filter in match.ValidFilters)
                opBox.AppendText(filter.Name);
            
            TreeIter opIterFirst;
            if(!opBox.Model.IterNthChild(out opIterFirst, 0))
                throw new Exception("Field has no operations");
                
            match.Column = fieldName;
                
            opBox.SetActiveIter(opIterFirst);
        }
        
        private void OnFieldComboBoxChanged(object o, EventArgs args)
        {
            TreeIter iter;
            fieldBox.GetActiveIter(out iter);
            Select(iter);
        }
        
        private void OnOpComboBoxChanged(object o, EventArgs args)
        {
            TreeIter iter;
            opBox.GetActiveIter(out iter);
            //string opName = (string)opBox.Model.GetValue(iter, 0);
            
            match.Op = opBox.Active;
            
            widgetBox.Foreach(WidgetBoxForeachRemoveChild);
            widgetBox.Add(match.DisplayWidget);
        }
        
        private void WidgetBoxForeachRemoveChild(Widget widget)
        {
            widgetBox.Remove(widget);
        }
        
        private void OnButtonAddClicked(object o, EventArgs args)
        {
            EventHandler handler = AddRequest;
            if(handler != null)
                handler(this, new EventArgs());
        }
        
        private void OnButtonRemoveClicked(object o, EventArgs args)
        {
            EventHandler handler = RemoveRequest;
            if(handler != null)
                handler(this, new EventArgs());
        }
        
        public bool CanDelete
        {
            set {
                buttonRemove.Sensitive = value;
            }
        }
        
        public string Query {
            get {
                match.Column = 
                    model.GetColumn(ComboBoxUtil.GetActiveString(fieldBox));
                match.Op = opBox.Active;
                return match.FilterValues();
            }
        }

        public ComboBox FieldBox {
            get {
                return fieldBox;
            }
        }

        public ComboBox FilterBox {
            get {
                return opBox;
            }
        }

        public QueryMatch Match {
            get {
                return match;
            }
        }
    }


    public class QueryBuilderMatches : VBox
    {
        private QueryBuilderModel model;

        private QueryBuilderMatchRow first_row = null;

        public QueryBuilderMatchRow FirstRow {
            get { return first_row; }
        }
        
        public QueryBuilderMatches(QueryBuilderModel model) : base()
        {
            this.model = model;
            CreateRow(false);
        }
        
        public void CreateRow(bool canDelete)
        {
            QueryBuilderMatchRow row = new QueryBuilderMatchRow(model);
            row.Show();
            PackStart(row, false, false, 0);
            row.CanDelete = canDelete;
            row.AddRequest += OnRowAddRequest;
            row.RemoveRequest += OnRowRemoveRequest;

            if (first_row == null) {
                first_row = row;
                row.FieldBox.GrabFocus();
            }
        }
        
        public void OnRowAddRequest(object o, EventArgs args)
        {
            CreateRow(true);
            UpdateCanDelete();
        }
        
        public void OnRowRemoveRequest(object o, EventArgs args)
        {
            Remove(o as Widget);
            UpdateCanDelete();
        }
        
        public void UpdateCanDelete()
        {
            ((QueryBuilderMatchRow)Children[0]).CanDelete = Children.Length > 1;
        }
        
        public string BuildQuery(string join)
        {
            string query = null;
            for(int i = 0, n = Children.Length; i < n; i++) {
                QueryBuilderMatchRow match = Children[i] as QueryBuilderMatchRow;
                query += " (" + match.Query + ") ";
                if(i < n - 1)
                    query += join;
            }
            
            return query;
        }
    }

    public class QueryBuilder : VBox
    {
        private QueryBuilderModel model;
        
        private CheckButton matchCheckBox;
        private ComboBox matchLogicCombo;
        private QueryBuilderMatches matchesBox;
        private Label matchLabelFollowing;
        
        private CheckButton limitCheckBox;
        private Entry limitEntry;
        private ComboBox limitComboBox;
        private ComboBox orderComboBox;

        public QueryBuilderMatches MatchesBox {
            get { return matchesBox; }
        }
    
        public QueryBuilder(QueryBuilderModel model) : base()
        {
            this.model = model;
        
            matchesBox = new QueryBuilderMatches(model);
            matchesBox.Spacing = 5;
            matchesBox.Show();
        
            Alignment matchesAlignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            matchesAlignment.Show();
            matchesAlignment.SetPadding(10, 10, 10, 10);
            matchesAlignment.Add(matchesBox);
        
            Frame matchesFrame = new Frame(null);
            matchesFrame.Show();
            matchesFrame.Add(matchesAlignment);
            
            matchesFrame.LabelWidget = BuildMatchHeader();
            
            PackStart(matchesFrame, true, true, 0);
            PackStart(BuildLimitFooter(), false, false, 0);
        }
        
        private HBox BuildMatchHeader()
        {
            HBox matchHeader = new HBox();
            matchHeader.Show();
            
            matchCheckBox = new CheckButton(Catalog.GetString("_Match"));
            matchCheckBox.Show();
            matchCheckBox.Active = true;
            matchCheckBox.Toggled += OnMatchCheckBoxToggled;
            matchHeader.PackStart(matchCheckBox, false, false, 0);
            
            matchLogicCombo = ComboBox.NewText();
            matchLogicCombo.AppendText(Catalog.GetString("all"));
            matchLogicCombo.AppendText(Catalog.GetString("any"));
            matchLogicCombo.Show();
            matchLogicCombo.Active = 0;
            matchHeader.PackStart(matchLogicCombo, false, false, 0);
            
            matchLabelFollowing = new Label(Catalog.GetString("of the following:"));
            matchLabelFollowing.Show();
            matchLabelFollowing.Xalign = 0.0f;
            matchHeader.PackStart(matchLabelFollowing, true, true, 0);
            
            matchHeader.Spacing = 5;
            
            //matchCheckBox.Active = false;
            //OnMatchCheckBoxToggled(matchCheckBox, null);
            
            return matchHeader;
        }
        
        private HBox BuildLimitFooter()
        {
            HBox limitFooter = new HBox();
            limitFooter.Show();
            limitFooter.Spacing = 5;
            
            limitCheckBox = new CheckButton(Catalog.GetString("_Limit to"));
            limitCheckBox.Show();
            limitCheckBox.Toggled += OnLimitCheckBoxToggled;
            limitFooter.PackStart(limitCheckBox, false, false, 0);
            
            limitEntry = new Entry("25");
            limitEntry.Show();
            limitEntry.SetSizeRequest(50, -1);
            limitFooter.PackStart(limitEntry, false, false, 0);
            
            limitComboBox = ComboBox.NewText();
            limitComboBox.Show();
            foreach(string criteria in model.LimitCriteria)
                limitComboBox.AppendText(criteria);
            limitComboBox.Active = 0;
            limitFooter.PackStart(limitComboBox, false, false, 0);
                
            Label orderLabel = new Label(Catalog.GetString("selected by"));
            orderLabel.Show();
            limitFooter.PackStart(orderLabel, false, false, 0);
            
            orderComboBox = ComboBox.NewText();
            orderComboBox.Show();
            foreach(string order in model.OrderCriteria)
                orderComboBox.AppendText(order);
            orderComboBox.Active = 0;
            limitFooter.PackStart(orderComboBox, false, false, 0);
                
            limitCheckBox.Active = false;
            OnLimitCheckBoxToggled(limitCheckBox, null);
                
            return limitFooter;
        }
        
        private void OnMatchCheckBoxToggled(object o, EventArgs args)
        {
            matchesBox.Sensitive = matchCheckBox.Active;
            matchLogicCombo.Sensitive = matchCheckBox.Active;
            matchLabelFollowing.Sensitive = matchCheckBox.Active;
        }
        
        private void OnLimitCheckBoxToggled(object o, EventArgs args)
        {
            limitEntry.Sensitive = limitCheckBox.Active;
            limitComboBox.Sensitive = limitCheckBox.Active;
            orderComboBox.Sensitive = limitCheckBox.Active;
        }
        
        public bool MatchesEnabled 
        {
            get {
                return matchCheckBox.Active;
            }

            set {
                matchCheckBox.Active = value;
            }
        }
        
        public string MatchQuery
        {
            get {
                return matchesBox.BuildQuery(
                    matchLogicCombo.Active == 0 ?
                    "AND" :
                    "OR"
                );
            }

            set {
                if (value == null || value == String.Empty) {
                    matchCheckBox.Active = false;
                    return;
                }

                // Check for ANDs or ORs and split into conditions as needed
                string [] conditions;
                if (value.IndexOf(") AND (") != -1) {
                    matchLogicCombo.Active = 0;
                    conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) AND \\(");
                    
                } else if (value.IndexOf(") OR (") != -1) {
                    matchLogicCombo.Active = 1;
                    conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) OR \\(");
                } else {
                    conditions = new string [] {value};
                }

                // Remove leading spaces and parens from the first condition
                conditions[0] = conditions[0].Remove(0, 2);

                // Remove trailing spaces and last paren from the last condition
                string tmp = conditions[conditions.Length-1];
                tmp = tmp.TrimEnd(new char[] {' '});
                tmp = tmp.Substring(0, tmp.Length - 1);
                conditions[conditions.Length-1] = tmp;

                matchCheckBox.Active = true;

                int count = 0;
                foreach (string condition in conditions) {
                    // Add a new row for this condition
                    string col, v1, v2;
                    bool found_filter = false;
                    foreach (QueryFilter filter in QueryFilter.Filters) {
                        if (filter.Operator.MatchesCondition (condition, out col, out v1, out v2)) {
                            //Console.WriteLine ("{0} is col: {1} with v1: {2} v2: {3}", condition, col, v1, v2);

                            // The first row is already created
                            if (count > 0)
                                matchesBox.CreateRow(true);

                            // Set the column
                            QueryBuilderMatchRow row = matchesBox.Children[count] as QueryBuilderMatchRow;
                            if (!ComboBoxUtil.SetActiveString (row.FieldBox, model.GetName(col))) {
                                if (col.IndexOf ("current_timestamp") == -1) {
                                    Console.WriteLine ("Found col that can't place");
                                    break;
                                } else {
                                    bool found = false;
                                    foreach (string field in model) {
                                        if (col.IndexOf (model.GetColumn (field)) != -1) {
                                            ComboBoxUtil.SetActiveString (row.FieldBox, field);
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found) {
                                        Console.WriteLine ("Found col that can't place");
                                        break;
                                    }
                                }
                            }

                            // Make sure we're on the right filter (as multiple filters can have the same operator)
                            QueryFilter real_filter = filter;
                            if (System.Array.IndexOf (row.Match.ValidFilters, filter) == -1) {
                                foreach (QueryFilter f in row.Match.ValidFilters) {
                                    if (f.Operator == filter.Operator) {
                                        real_filter = f;
                                        break;
                                    }
                                }
                            }

                            // Set the operator
                            if (!ComboBoxUtil.SetActiveString (row.FilterBox, real_filter.Name)) {
                                Console.WriteLine ("Found filter that can't place");
                                break;
                            }

                            // Set the values
                            row.Match.Value1 = v1;
                            row.Match.Value2 = v2;

                            found_filter = true;
                            break;
                        }
                    }

                    // TODO should push error here instead
                    if (!found_filter)
                        Console.WriteLine ("Couldn't find appropriate filter for condition: {0}", condition);
                    count++;

                    matchesBox.UpdateCanDelete();
                }
            }
        }
        
        public string LimitNumber {
            get {
                try {
                    Convert.ToInt32(limitEntry.Text);
                    return limitEntry.Text;
                } catch(Exception) {
                    return "0";
                }
            }

            set {
                limitEntry.Text = value;
            }
        }
        
        public int LimitCriterion {
            get { return limitComboBox.Active; }

            set { limitComboBox.Active = value; }
        }
        
        public bool Limit {
            get {
                return limitCheckBox.Active;
            }

            set {
                limitCheckBox.Active = value;
            }
        }
        
        public string OrderBy {
            get {
                return model.GetOrder(ComboBoxUtil.GetActiveString(orderComboBox));
            }

            set {
                if (value == null)
                    return;

                ComboBoxUtil.SetActiveString(orderComboBox, model.GetOrderName(value));
            }
        }
    }
}
