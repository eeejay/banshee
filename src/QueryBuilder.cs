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
using Sql;
using System.Collections;

namespace Sonance
{
	// --- Query Filter Operations --- 
	
	public class ComboBoxUtil
	{
		public static string GetActiveString(ComboBox box)
		{
			TreeIter iter;
			if(!box.GetActiveIter(out iter))
				return null;
				
			return (string)box.Model.GetValue(iter, 0);
		}
	}
	
	// --- Base QueryMatch Class --- 

	public abstract class QueryMatch
	{
		public string Column, Filter, Value1, Value2;
		
		public abstract string FilterValues();
		public abstract void UpdateValues();
		
		public abstract Widget DisplayWidget 
		{
			get;
		}
		
		public abstract string [] ValidOperations
		{
			get;
		}
		
		protected static HBox BuildRangeBox(Widget a, Widget b)
		{
			HBox box = new HBox();
			box.Spacing = 5;
			a.Show();
			box.PackStart(a, true, true, 0);
			
			Label label = new Label(" to ");
			label.Show();
			box.PackStart(label, false, false, 0);
			
			b.Show();
			box.PackStart(b, true, true, 0);
			
			box.Show();
			
			return box;
		}
	}
	
	// --- Base QueryBuilderModel Class --- 
	
	public abstract class QueryBuilderModel : IEnumerable
	{
		private Hashtable fields;
		private Hashtable columnLookup;
		private Hashtable orderMap;
		
		public QueryBuilderModel()
		{
			fields = new Hashtable();
			columnLookup = new Hashtable();
			orderMap = new Hashtable();
		}
		
		public Type this [string index] 
		{
			get {
				return (Type)fields[index];
			}
		}
		
		public IEnumerator GetEnumerator()
		{
			return fields.Keys.GetEnumerator();
		}
		
		public void AddField(string name, string column, Type matchType)
		{
			fields[name] = matchType;
			columnLookup[name] = column;
		}
		
		public void AddOrder(string name, string map)
		{
			orderMap[name] = map;
		}
		
		public string GetOrder(string name)
		{
			return (string)orderMap[name];
		}
		
		public string GetColumn(string name)
		{
			return (string)columnLookup[name];
		}
		
		public abstract string [] LimitCriteria 
		{
			get;
		}
		
		public ICollection OrderCriteria 
		{
			get {
				return orderMap.Keys;
			}
		}
	}
	
	// --- Query Builder Widgets

	public class QueryBuilderMatchRow : HBox
	{
		private VBox widgetBox;
		private ComboBox fieldBox, opBox;
		private Widget valueBox;
		private QueryBuilderModel model;
		private QueryMatch match;
		private Button buttonAdd;
		private Button buttonRemove;
		
		public event EventHandler AddRequest;
		public event EventHandler RemoveRequest;
		
		private bool canDelete;
		
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(QueryBuilderMatchRow));
				return gtype;
			}
		}
		
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
			PackStart(widgetBox, true, true, 0);
			
			foreach(string fieldName in model) {
				fieldBox.AppendText(fieldName);
			}
			
			Select(0);
			
			Image imageRemove = new Image("gtk-remove", IconSize.Button);
			buttonRemove = new Button(imageRemove);
			buttonRemove.Show();
			buttonRemove.Clicked += OnButtonRemoveClicked;
			imageRemove.Show();
			PackStart(buttonRemove, false, false, 0);
			
			Image imageAdd = new Image("gtk-add", IconSize.Button);
			buttonAdd = new Button(imageAdd);
			buttonAdd.Show();
			buttonAdd.Clicked += OnButtonAddClicked;
			imageAdd.Show();
			PackStart(buttonAdd, false, false, 0);
			
			canDelete = true;
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

			foreach(string op in match.ValidOperations)
				opBox.AppendText(op);
			
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
			string opName = (string)opBox.Model.GetValue(iter, 0);
			
			match.Filter = opName;
			
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
				canDelete = value;
				buttonRemove.Sensitive = value;
			}
		}
		
		public string Query
		{
			get {
				match.Column = 
					model.GetColumn(ComboBoxUtil.GetActiveString(fieldBox));
				match.Filter = ComboBoxUtil.GetActiveString(opBox);
				return match.FilterValues();
			}
		}
	}


	public class QueryBuilderMatches : VBox
	{
		private QueryBuilderModel model;
		
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(QueryBuilderMatches));
				return gtype;
			}
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
				query += " " + match.Query + " ";
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
	
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(QueryBuilder));
				return gtype;
			}
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
			
			matchCheckBox = new CheckButton("Match");
			matchCheckBox.Show();
			matchCheckBox.Toggled += OnMatchCheckBoxToggled;
			matchHeader.PackStart(matchCheckBox, false, false, 0);
			
			matchLogicCombo = ComboBox.NewText();
			matchLogicCombo.AppendText("all");
			matchLogicCombo.AppendText("any");
			matchLogicCombo.Show();
			matchLogicCombo.Active = 0;
			matchHeader.PackStart(matchLogicCombo, false, false, 0);
			
			matchLabelFollowing = new Label("of the following:");
			matchLabelFollowing.Show();
			matchLabelFollowing.Xalign = 0.0f;
			matchHeader.PackStart(matchLabelFollowing, true, true, 0);
			
			matchHeader.Spacing = 5;
			
			matchCheckBox.Active = false;
			OnMatchCheckBoxToggled(matchCheckBox, null);
			
			return matchHeader;
		}
		
		private HBox BuildLimitFooter()
		{
			HBox limitFooter = new HBox();
			limitFooter.Show();
			limitFooter.Spacing = 5;
			
			limitCheckBox = new CheckButton("Limit to");
			limitCheckBox.Show();
			limitCheckBox.Toggled += OnLimitCheckBoxToggled;
			limitFooter.PackStart(limitCheckBox, false, false, 0);
			
			limitEntry = new Entry();
			limitEntry.Show();
			limitEntry.SetSizeRequest(50, -1);
			limitFooter.PackStart(limitEntry, false, false, 0);
			
			limitComboBox = ComboBox.NewText();
			limitComboBox.Show();
			foreach(string criteria in model.LimitCriteria)
				limitComboBox.AppendText(criteria);
			limitComboBox.Active = 0;
			limitFooter.PackStart(limitComboBox, false, false, 0);
				
			Label orderLabel = new Label("selected by");
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
		}
		
		public bool MatchesEnabled 
		{
			get {
				return matchCheckBox.Active;
			}
		}
		
		public string MatchQuery
		{
			get {
				return matchesBox.BuildQuery(
					ComboBoxUtil.GetActiveString(matchLogicCombo) == "any" ?
					"OR" :
					"AND"
				);
			}
		}
		
		public int LimitNumber
		{
			get {
				try {
					return Convert.ToInt32(limitEntry.Text);
				} catch(Exception) {
					return 0;
				}
			}
		}
		
		public string LimitCriteria
		{
			get {
				return ComboBoxUtil.GetActiveString(limitComboBox);
			}
		}
		
		public bool Limit
		{
			get {
				return limitCheckBox.Active;
			}
		}
		
		public string OrderBy
		{
			get {
				return 
				model.GetOrder(ComboBoxUtil.GetActiveString(orderComboBox));
			}
		}
	}
}
