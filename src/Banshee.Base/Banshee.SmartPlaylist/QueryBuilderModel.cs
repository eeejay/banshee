/***************************************************************************
 *  QueryBuilderModel.cs
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

using Mono.Unix;

using Banshee.Widgets;
using Banshee.Sources;

namespace Banshee.SmartPlaylist
{
    
    public sealed class QuerySelectedByCriteria
    {
        public static string Random = Catalog.GetString("Random");
        public static string Album = Catalog.GetString("Album");
        public static string Artist = Catalog.GetString("Artist");
        public static string Genre = Catalog.GetString("Genre");
        public static string SongName = Catalog.GetString("Title");
        public static string HighestRating = Catalog.GetString("Highest Rating");
        public static string LowestRating = Catalog.GetString("Lowest Rating");
        public static string LeastOftenPlayed = Catalog.GetString("Least Often Played");
        public static string MostOftenPlayed = Catalog.GetString("Most Often Played");
        public static string MostRecentlyAdded = Catalog.GetString("Most Recently Added");
        public static string LeastRecentlyAdded = Catalog.GetString("Least Recently Added");
        public static string MostRecentlyPlayed = Catalog.GetString("Most Recently Played");
        public static string LeastRecentlyPlayed = Catalog.GetString("Least Recently Played");
    }
    
    public sealed class QueryLimitCriteria
    {
        public static string Songs = Catalog.GetString("songs");
        public static string Minutes = Catalog.GetString("minutes");
        public static string Hours = Catalog.GetString("hours");
        public static string MB = Catalog.GetString("MB");
    }

    // --- Query Match String --- 
    
    public class QueryMatchString : QueryMatch
    {
        private Entry dispEntry;

        public override string FilterValues()
        {
            if (Filter == null)
                return null;
            else
                return Filter.Operator.FormatValues (true, Column, Value1.ToLower(), null);
        }
        
        public override string Value1 {
            get { return dispEntry.Text; }
            set { dispEntry.Text = value; }
        }

        public override string Value2 {
            get { return null; }
            set {}
        }
        
        public override Widget DisplayWidget
        {
            get {
                if(dispEntry == null) {
                    dispEntry = new Entry();
                    dispEntry.Show();
                }
                    
                return dispEntry;
            }
        }
        
        public override QueryFilter [] ValidFilters {
            get {
                return new QueryFilter [] {
                    QueryFilter.TextIs,
                    QueryFilter.TextIsNot,
                    QueryFilter.Contains,
                    QueryFilter.DoesNotContain,
                    QueryFilter.StartsWith,
                    QueryFilter.EndsWith
                };
            }
        }
    }

    // --- Query Match Integers --- 

    public class QueryMatchInteger : QueryMatch
    {
        protected SpinButton spinButton1, spinButton2;
        private HBox rangeBox;

        private int default_value_1;
        private int default_value_2;

        public QueryMatchInteger() : base ()
        {
            DefaultValue1 = DefaultValue2 = 1;
        }

        public override string FilterValues()
        {
            if (Filter == null)
                return null;
            else
                return Filter.Operator.FormatValues (false, Column, Value1, Value2);
        }
        
        public override string Value1 {
            get { return spinButton1.ValueAsInt.ToString (); }
            set { spinButton1.Value = Double.Parse(value); }
        }

        public override string Value2 {
            get { return (spinButton2 == null) ? null : spinButton2.ValueAsInt.ToString (); }
            set {
                if (value == null)
                    return;

                spinButton2.Value = Double.Parse(value);
            }

        }
        
        public override Widget DisplayWidget
        {
            get {
                if(spinButton1 == null) {
                    spinButton1 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton1.Value = DefaultValue1;
                    spinButton1.Digits = 0;
                    spinButton1.WidthChars = 4;
                    spinButton1.Show();
                }
                
                if(Filter.Operator != QueryOperator.Between) {
                    if(rangeBox != null && spinButton2 != null) {
                        rangeBox.Remove(spinButton1);
                        rangeBox.Remove(spinButton2);
                        
                        spinButton2.Destroy();
                        spinButton2 = null;
                        rangeBox.Destroy();
                        rangeBox = null;
                    }
                
                    return spinButton1;
                }
                
                if(spinButton2 == null) {
                    spinButton2 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton2.Value = DefaultValue2;
                    spinButton2.Digits = 0;
                    spinButton2.WidthChars = 4;
                    spinButton2.Show();
                }
                
                rangeBox = BuildRangeBox(spinButton1, spinButton2);
                return rangeBox;
            }
        }
        
        public override QueryFilter [] ValidFilters {
            get {    
                return new QueryFilter [] {
                    QueryFilter.Is,
                    QueryFilter.IsNot,
                    QueryFilter.IsLessThan,
                    QueryFilter.IsGreaterThan,
                    QueryFilter.IsInTheRange
                };
            }
        }

        public int DefaultValue1 {
            get {
                return default_value_1;
            }
            set {
                default_value_1 = value;
            }
        }

        public int DefaultValue2 {
            get {
                return default_value_2;
            }
            set {
                default_value_2 = value;
            }
        }
    }

    public class QueryMatchYear : QueryMatchInteger
    {
        public QueryMatchYear() : base ()
        {
            DefaultValue1 = DefaultValue2 = DateTime.Now.Year;
        }
    }

    /*-- Query Match Rating --*/
    public class QueryMatchRating : QueryMatch
    {
        protected RatingEntry rating1, rating2;
        private HBox rangeBox;

        public override string FilterValues()
        {
            if (Filter == null)
                return null;
            else
                return Filter.Operator.FormatValues (false, Column, Value1, Value2);
        }
        
        public override string Value1 {
            get { return rating1.Value.ToString (); }
            set { rating1.Value = Int32.Parse(value); }
        }

        public override string Value2 {
            get { return (rating2 == null) ? null : rating2.Value.ToString (); }
            set {
                if (value == null)
                    return;

                rating2.Value = Int32.Parse(value);
            }

        }
        
        public override Widget DisplayWidget
        {
            get {
                if(rating1 == null) {
                    rating1 = new RatingEntry();
                    rating1.Show();
                }
                
                if(Filter.Operator != QueryOperator.Between) {
                    if(rangeBox != null && rating2 != null) {
                        rangeBox.Remove(rating1);
                        rangeBox.Remove(rating2);
                        
                        rating2.Destroy();
                        rating2 = null;
                        rangeBox.Destroy();
                        rangeBox = null;
                    }
                
                    return rating1;
                }
                
                if(rating2 == null) {
                    rating2 = new RatingEntry();
                    rating2.Show();
                }
                
                rangeBox = BuildRangeBox(rating1, rating2);
                return rangeBox;
            }
        }
        
        public override QueryFilter [] ValidFilters {
            get {    
                return new QueryFilter [] {
                    QueryFilter.Is,
                    QueryFilter.IsNot,
                    QueryFilter.IsLessThan,
                    QueryFilter.IsGreaterThan,
                    QueryFilter.IsInTheRange
                };
            }
        }
    }
  
    // --- Query Match Time --- 
    // Used to match things like [duration] [less|greater] than [2] [minutes]
        
    public class QueryMatchTime : QueryMatchInteger
    {
        // Multiplied by the spinButton inputs to determine the equivalent number of seconds the user
        // has entered.
        private static int [] time_multipliers = {1, 60, 60*60 };
        private ComboBox comboBox1, comboBox2;
        private HBox hBox1, hBox2;
        private HBox rangeBox;

        private static ComboBox GetComboBox ()
        {
            ComboBox box = ComboBox.NewText();

            box.AppendText(Catalog.GetString("Seconds"));
            box.AppendText(Catalog.GetString("Minutes"));
            box.AppendText(Catalog.GetString("Hours"));

            box.Active = 1;

            return box;
        }

        public override string Value1 {
            get { return (comboBox1 == null) ? null : (time_multipliers [comboBox1.Active] * spinButton1.ValueAsInt).ToString(); }
            set {

                if (value == null)
                    return;

                int val = Int32.Parse (value);

                int i = 1;
                for (i = (time_multipliers.Length - 1); i >= 0; i--) {
                    if (val % time_multipliers[i] == 0) {
                        comboBox1.Active = i;
                        break;
                    }

                }

                spinButton1.Value = (double) (val / time_multipliers[comboBox1.Active]);
            }
        }

        public override string Value2 {
            get { return (comboBox2 == null) ? null : (time_multipliers [comboBox2.Active] * spinButton2.ValueAsInt).ToString(); }
            set {
                if (value == null)
                    return;

                int val = Int32.Parse (value);

                int i = 1;
                for (i = (time_multipliers.Length - 1); i >= 0; i--) {
                    if (val % time_multipliers[i] == 0) {
                        comboBox2.Active = i;
                        break;
                    }

                }

                spinButton2.Value = (double) (val / time_multipliers[comboBox2.Active]);
            }
        }
        
        public override Widget DisplayWidget
        {
            get {
                if(spinButton1 == null) {
                    spinButton1 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton1.Value = 2.0;
                    spinButton1.Digits = 0;
                    spinButton1.WidthChars = 4;
                    spinButton1.Show();

                    comboBox1 = GetComboBox();

                    hBox1 = new HBox();
                    hBox1.Spacing = 5;
                    hBox1.PackStart(spinButton1, false, false, 0);
                    hBox1.PackStart(comboBox1, false, false, 0);

                    hBox1.ShowAll();
                }

                if(Filter.Operator != QueryOperator.Between) {
                    if(rangeBox != null && spinButton2 != null) {
                        rangeBox.Remove(hBox1);
                        rangeBox.Remove(hBox2);

                        spinButton2.Destroy();
                        spinButton2 = null;

                        comboBox2.Destroy();
                        comboBox2 = null;

                        hBox2.Destroy();
                        hBox2 = null;

                        rangeBox.Destroy();
                        rangeBox = null;

                    }

                    return hBox1;
                }

                if(spinButton2 == null) {
                    spinButton2 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton2.Value = 4.0;
                    spinButton2.Digits = 0;
                    spinButton2.WidthChars = 4;
                    spinButton2.Show();

                    comboBox2 = GetComboBox();
                    comboBox2.Active = comboBox1.Active;

                    hBox2 = new HBox();
                    hBox2.Spacing = 5;
                    hBox2.PackStart(spinButton2, false, false, 0);
                    hBox2.PackStart(comboBox2, false, false, 0);
                    hBox2.ShowAll();
                }

                rangeBox = BuildRangeBox(hBox1, hBox2);
                return rangeBox;
            }
        }
    }
    
    // --- Query Match Date --- 
    // Used to match things like [Added|Last Played] [less|greater] than [2] [weeks] ago
        
    public class QueryMatchDate : QueryMatch
    {
        // Multiplied by the spinButton inputs to determine the equivalent number of seconds the user
        // has entered.
        private static int [] date_multipliers = {60, 3600, 24*3600, 7*24*3600, 30*24*3600, 365*24*3600};
        private SpinButton spinButton1, spinButton2;
        private ComboBox comboBox1, comboBox2;
        private HBox hBox1, hBox2;
        private Label ago1;
        private HBox rangeBox;

        private static ComboBox GetComboBox ()
        {
            ComboBox box = ComboBox.NewText();

            box.AppendText(Catalog.GetString("Minutes"));
            box.AppendText(Catalog.GetString("Hours"));
            box.AppendText(Catalog.GetString("Days"));
            box.AppendText(Catalog.GetString("Weeks"));
            box.AppendText(Catalog.GetString("Months"));
            box.AppendText(Catalog.GetString("Years"));

            box.Active = 1;

            return box;
        }

        public override string FilterValues()
        {
            string pv = Value1;
            string pv2 = (spinButton2 == null) ? null : Value2;

            if (Filter == null)
                return null;
            else
                return Filter.Operator.FormatValues (false, SqlColumn, pv, pv2);
        }

        public override string SqlColumn {
            get { return String.Format("(strftime(\"%s\", current_timestamp) - {0} + 3600)", Column); }
        }
        
        public override string Value1 {
            get { return (comboBox1 == null) ? null : (date_multipliers [comboBox1.Active] * spinButton1.ValueAsInt).ToString(); }
            set {
                if (value == null)
                    return;

                int val = Int32.Parse (value);

                int i = 1;
                for (i = 1; i < date_multipliers.Length; i++) {
                    if (val < date_multipliers[i]) {
                        comboBox1.Active = i - 1;
                        break;
                    }
                }

                if (i == date_multipliers.Length) {
                    comboBox1.Active = i;
                }

                spinButton1.Value = (double) (val / date_multipliers[comboBox1.Active]);
            }
        }

        public override string Value2 {
            get { return (comboBox2 == null) ? null : (date_multipliers [comboBox2.Active] * spinButton2.ValueAsInt).ToString(); }
            set {
                if (value == null)
                    return;

                int val = Int32.Parse (value);

                int i = 1;
                for (i = 1; i < date_multipliers.Length; i++) {
                    if (val < date_multipliers[i]) {
                        comboBox2.Active = i - 1;
                        break;
                    }

                }

                if (i == date_multipliers.Length) {
                    comboBox2.Active = i;
                }

                spinButton2.Value = (double) (val / date_multipliers[comboBox2.Active]);
            }
        }
        
        public override Widget DisplayWidget
        {
            get {
                if(spinButton1 == null) {
                    spinButton1 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton1.Value = 2.0;
                    spinButton1.Digits = 0;
                    spinButton1.WidthChars = 2;
                    spinButton1.Show();

                    comboBox1 = GetComboBox();

                    hBox1 = new HBox();
                    hBox1.Spacing = 5;
                    hBox1.PackStart(spinButton1, false, false, 0);
                    hBox1.PackStart(comboBox1, false, false, 0);

                    hBox1.ShowAll();
                }
                
                if(Filter.Operator != QueryOperator.Between) {
                    if(rangeBox != null && spinButton2 != null) {
                        rangeBox.Remove(hBox1);
                        rangeBox.Remove(hBox2);
                        
                        spinButton2.Destroy();
                        spinButton2 = null;

                        comboBox2.Destroy();
                        comboBox2 = null;

                        hBox2.Destroy();
                        hBox2 = null;

                        rangeBox.Destroy();
                        rangeBox = null;

                    }

                    if (ago1 == null) {
                        ago1 = new Label(Catalog.GetString("ago"));
                        hBox1.PackStart(ago1, false, false, 0);
                        ago1.Show();
                    }
                
                    return hBox1;
                }
                
                if(spinButton2 == null) {
                    spinButton2 = new SpinButton(Int32.MinValue, Int32.MaxValue, 1.0);
                    spinButton2.Value = 4.0;
                    spinButton2.Digits = 0;
                    spinButton2.WidthChars = 2;
                    spinButton2.Show();
                    hBox1.Remove(ago1);
                    ago1.Destroy();
                    ago1 = null;

                    comboBox2 = GetComboBox();
                    comboBox2.Active = comboBox1.Active;

                    hBox2 = new HBox();
                    hBox2.Spacing = 5;
                    hBox2.PackStart(spinButton2, false, false, 0);
                    hBox2.PackStart(comboBox2, false, false, 0);
                    hBox2.PackStart(new Label(Catalog.GetString ("ago")), false, false, 0);
                    hBox2.ShowAll();
                }
                
                rangeBox = BuildRangeBox(hBox1, hBox2);
                return rangeBox;
            }
        }
        
        public override QueryFilter [] ValidFilters {
            get {    
                return new QueryFilter [] {
                    // To get these two working need to not check against the exact second but +/- 12*3600 seconds
                    //QueryFilter.Is,
                    //QueryFilter.IsNot,
                    QueryFilter.MoreThan,
                    QueryFilter.LessThan,
                    QueryFilter.Between
                };
            }
        }
    }

    public class QueryMatchPlaylist : QueryMatch
    {
        private ComboBox comboBox1;

        private static ComboBox GetComboBox ()
        {
            ComboBox box = ComboBox.NewText();

            foreach (ChildSource child in LibrarySource.Instance.Children) {
                if (child is PlaylistSource) {
                    box.AppendText(child.Name);
                }
            }

            box.Active = 0;

            return box;
        }

        public override string FilterValues()
        {
            int playlist_id = -1;

            foreach (ChildSource child in LibrarySource.Instance.Children) {
                if (child is PlaylistSource && child.Name == Value1) {
                    playlist_id = (child as PlaylistSource).Id;
                    break;
                }
            }

            return Filter.Operator.FormatValues (false, "PlaylistID", playlist_id.ToString(), null);
        }

        public override string Value1 {
            get { return (comboBox1 == null) ? null : comboBox1.ActiveText; }
            set {
                if (value == null)
                    return;

                int val = Int32.Parse (value);

                int i = 1;
                foreach (ChildSource child in LibrarySource.Instance.Children) {
                    if (child is PlaylistSource && (child as PlaylistSource).Id == val) {
                        comboBox1.Active = i - 1;
                        break;
                    }
                    i++;
                }
            }
        }

        public override string Value2 {
            get { return null; }
            set { }
        }

        public override Widget DisplayWidget
        {
            get {
                if(comboBox1 == null) {
                    comboBox1 = GetComboBox();
                    comboBox1.ShowAll();
                }
                
                return comboBox1;
            }
        }
        
        public override QueryFilter [] ValidFilters {
            get {    
                return new QueryFilter [] {
                    QueryFilter.InPlaylist,
                    QueryFilter.NotInPlaylist
                };
            }
        }
    }
    
    public class TracksQueryModel : QueryBuilderModel
    {
        public TracksQueryModel() : base()
        {
            AddField(Catalog.GetString("Artist"), "Artist", typeof(QueryMatchString));
            AddField(Catalog.GetString("Title"), "Title", typeof(QueryMatchString));
            AddField(Catalog.GetString("Album"), "AlbumTitle", typeof(QueryMatchString));
            AddField(Catalog.GetString("Genre"), "Genre", typeof(QueryMatchString));
            AddField(Catalog.GetString("Date Added"), "DateAddedStamp", typeof(QueryMatchDate));
            AddField(Catalog.GetString("Last Played"), "LastPlayedStamp", typeof(QueryMatchDate));
            AddField(Catalog.GetString("Duration"), "Duration", typeof(QueryMatchTime));
            AddField(Catalog.GetString("Play Count"), "NumberOfPlays", typeof(QueryMatchInteger));
            AddField(Catalog.GetString("Playlist"), "PlaylistID", typeof(QueryMatchPlaylist));
            AddField(Catalog.GetString("Rating"), "Rating", typeof(QueryMatchRating));
            AddField(Catalog.GetString("Path"), "Uri", typeof(QueryMatchString));
            AddField(Catalog.GetString("Year"), "Year", typeof(QueryMatchYear));
            
            AddOrder(QuerySelectedByCriteria.Random, "RANDOM()");
            AddOrder(QuerySelectedByCriteria.Album, "AlbumTitle");
            AddOrder(QuerySelectedByCriteria.Artist, "Artist");
            AddOrder(QuerySelectedByCriteria.Genre, "Genre");
            AddOrder(QuerySelectedByCriteria.SongName, "Title");
            AddOrder(QuerySelectedByCriteria.HighestRating, "Rating DESC");
            AddOrder(QuerySelectedByCriteria.LowestRating, "Rating ASC");
            AddOrder(QuerySelectedByCriteria.MostOftenPlayed, "NumberOfPlays DESC");
            AddOrder(QuerySelectedByCriteria.LeastOftenPlayed, "NumberOfPlays ASC");
            AddOrder(QuerySelectedByCriteria.MostRecentlyAdded, "DateAddedStamp DESC");
            AddOrder(QuerySelectedByCriteria.LeastRecentlyAdded, "DateAddedStamp ASC");
            AddOrder(QuerySelectedByCriteria.MostRecentlyPlayed, "LastPlayedStamp DESC");
            AddOrder(QuerySelectedByCriteria.LeastRecentlyPlayed, "LastPlayedStamp ASC");
        }

        public override string [] LimitCriteria 
        {
            get {
                string [] criteria = {
                    QueryLimitCriteria.Songs,
                    QueryLimitCriteria.Minutes,
                    QueryLimitCriteria.Hours,
                    QueryLimitCriteria.MB
                };
                
                return criteria;
            }
        }
    }

    public class SqlBuilderUI
    {
        private QueryBuilder builder;
        private TracksQueryModel model;
    
        public SqlBuilderUI()
        {
            Window win = new Window("SQL Builder");
            win.Show();
            win.BorderWidth = 10;
            win.Resizable = false;
            
            VBox box = new VBox();
            box.Show();
            win.Add(box);
            box.Spacing = 10;
            
            model = new TracksQueryModel();
            builder = new QueryBuilder(model);
            builder.Show();
            builder.Spacing = 4;
            
            box.PackStart(builder, true, true, 0);
            
            Button btn = new Button("Generate Query");
            btn.Show();
            box.PackStart(btn, false, false, 0);
            btn.Clicked += OnButtonClicked;    
        }
        
        private void OnButtonClicked(object o, EventArgs args)
        {
            string query = "SELECT * FROM Tracks";
            
            query += builder.MatchesEnabled ?
                " WHERE" + builder.MatchQuery : " ";
            
            query += "ORDER BY " + builder.OrderBy;
            
            if(builder.Limit && builder.LimitNumber != "0")
                query += " LIMIT " + builder.LimitNumber;
            
            Console.WriteLine(query);
        }
    }
}
