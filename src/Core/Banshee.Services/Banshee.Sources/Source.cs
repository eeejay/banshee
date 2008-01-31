//
// Source.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Query;

using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.Sources
{
    public abstract class Source : ISource
    {
        private Source parent;        
        private PropertyStore properties = new PropertyStore();
        private List<Source> child_sources = new List<Source>();

        public event EventHandler Updated;
        public event EventHandler UserNotifyUpdated;
        public event SourceEventHandler ChildSourceAdded;
        public event SourceEventHandler ChildSourceRemoved;
        
        protected Source(string generic_name, string name, int order)
        {
            GenericName = generic_name;
            Name = name;
            Order = order;
            
            properties.PropertyChanged += OnPropertyChanged;
        }
        
        protected void OnSetupComplete()
        {
            if(this is ITrackModelSource) {
                ITrackModelSource tm_source = (ITrackModelSource)this;
                
                tm_source.TrackModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject(tm_source.TrackModel);
                
                tm_source.ArtistModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject(tm_source.ArtistModel);
                
                tm_source.AlbumModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject(tm_source.AlbumModel);
            }
        }

        protected void Remove ()
        {
            if (ServiceManager.SourceManager.ContainsSource (this)) {
                if (this.Parent != null) {
                    this.Parent.RemoveChildSource (this);
                } else {
                    ServiceManager.SourceManager.RemoveSource (this);
                }
            }
        }

#region Public Methods
        
        public virtual void Activate()
        {
        }

        public virtual void Deactivate()
        {
        }

        public virtual void Rename(string newName)
        {
            properties.SetString("Name", newName);
        }
        
        public void SetParentSource (Source parent)
        {
            this.parent = parent;
        }
        
        public virtual void AddChildSource(Source child)
        {
            lock(Children) {
                child.SetParentSource (this);
                child_sources.Add (child);
                OnChildSourceAdded (child);
            }
        }

        public virtual void RemoveChildSource (Source child)
        {
            lock (Children) {
                if (child.Children.Count > 0) {
                    child.ClearChildSources ();
                }
                
                child_sources.Remove (child);
                
                if (ServiceManager.SourceManager.ActiveSource == child) {
                    ServiceManager.SourceManager.SetActiveSource(ServiceManager.SourceManager.DefaultSource);
                }
                
                OnChildSourceRemoved (child);
            }
        }
        
        public virtual void ClearChildSources ()
        {
            lock(Children) {
                while(child_sources.Count > 0) {
                    RemoveChildSource (child_sources[child_sources.Count - 1]);
                }
            }
        }

        public class NameComparer : IComparer<Source>
        {
            public int Compare (Source a, Source b)
            {
                return a.Name.CompareTo (b.Name);
            }
        }

        public class SizeComparer : IComparer<Source>
        {
            public int Compare (Source a, Source b)
            {
                return a.Count.CompareTo (b.Count);
            }
        }

        /*public virtual void SortChildSources (IComparer<Source> comparer, bool asc)
        {
            lock(Children) {
                child_sources.Sort (comparer);
                if (!asc) {
                    child_sources.Reverse ();
                }

                int i = 0;
                foreach (Source child in child_sources) {
                    child.Order = i++;
                }
            }
        }*/
        
#endregion
        
#region Protected Methods
    
        protected virtual void OnChildSourceAdded(Source source)
        {
            SourceEventHandler handler = ChildSourceAdded;
            if(handler != null) {
                SourceEventArgs args = new SourceEventArgs();
                args.Source = source;
                handler(args);
            }
        }
        
        protected virtual void OnChildSourceRemoved(Source source)
        {
            SourceEventHandler handler = ChildSourceRemoved;
            if(handler != null) {
                SourceEventArgs args = new SourceEventArgs();
                args.Source = source;
                handler(args);
            }
        }
        
        protected virtual void OnUpdated()
        {
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnUserNotifyUpdated()
        {
            EventHandler handler = UserNotifyUpdated;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
#endregion
        
#region Private Methods
        
        private void OnPropertyChanged(object o, PropertyChangeEventArgs args)
        {
            OnUpdated();
        }
        
#endregion
        
#region Public Properties
        
        public ICollection<Source> Children {
            get { return child_sources; }
        }
        
        string [] ISource.Children {
            get {
                return null;
            }
        }

        public Source Parent {
            get { return parent; }
        }

        public virtual bool CanRename {
            get { return true; }
        }

        public virtual bool HasProperties {
            get { return false; }
        }

        public string Name {
            get { return properties.GetString("Name"); }
            set { properties.SetString("Name", value); }
        }

        public string GenericName {
            get { return properties.GetString("GenericName"); }
            set { properties.SetString("GenericName", value); }
        }
        
        public int Order {
            get { return properties.GetInteger("Order"); }
            set { properties.SetInteger("Order", value); }
        }

        public virtual bool ImplementsCustomSearch {
            get { return false; }
        }
        
        public virtual bool CanSearch {
            get { return true; }
        }
                
        public virtual string FilterQuery {
            get { return properties.GetString("FilterQuery"); }
            set { properties.SetString("FilterQuery", value); }
        }
        
        public TrackFilterType FilterType {
            get { return (TrackFilterType)properties.GetInteger("FilterType"); }
            set { properties.SetInteger("FilterType", (int)value); }
        }
        
        public virtual bool Expanded {
            get { return properties.GetBoolean("Expanded"); }
            set { properties.SetBoolean("Expanded", value); }
        }
        
        public virtual bool? AutoExpand {
            get { return true; }
        }
        
        public virtual PropertyStore Properties {
            get { return properties; }
        }
        
        public virtual bool CanActivate {
            get { return true; }
        }
        
        public abstract int Count { get; }
        public virtual int FilteredCount { get { return Count; } }

        public virtual string GetStatusText ()
        {
            StringBuilder builder = new StringBuilder ();

            int count = FilteredCount;
            
            if (count == 0) {
                return String.Empty;
            }
            
            builder.AppendFormat (Catalog.GetPluralString ("{0} song", "{0} songs", count), count);
            
            if (this is IDurationAggregator) {
                builder.Append (", ");

                TimeSpan span = (this as IDurationAggregator).FilteredDuration; 
                if (span.Days > 0) {
                    double days = span.Days + (span.Hours / 24.0);
                    builder.AppendFormat (Catalog.GetPluralString ("{0} day", "{0:0.0} days", 
                        (int)Math.Ceiling (days)), days);
                } else if (span.Hours > 0) {
                    double hours = span.Hours + (span.Minutes / 60.0);
                    builder.AppendFormat (Catalog.GetPluralString ("{0} hour", "{0:0.0} hours", 
                        (int)Math.Ceiling (hours)), hours);
                } else {
                    double minutes = span.Minutes + (span.Seconds / 60.0);
                    builder.AppendFormat (Catalog.GetPluralString ("{0} minute", "{0:0.0} minutes", 
                        (int)Math.Ceiling (minutes)), minutes);
                }
            }

            if (this is IFileSizeAggregator) {
                long bytes = (this as IFileSizeAggregator).FileSize;
                if (bytes > 0) {
                    builder.Append (", ");
                    builder.AppendFormat (new FileSizeQueryValue (bytes).ToUserQuery ());
                }
            }
            
            return builder.ToString ();
        }
        
        string IService.ServiceName {
            get { return DBusServiceManager.MakeDBusSafeString(Name) + "Source"; }
        }
        
        IDBusExportable IDBusExportable.Parent {
            get {
                if (Parent != null) {
                    return ((Source)this).Parent;
                } else {
                    return ServiceManager.SourceManager;
                }
            }
        }
        
        public virtual string TrackModelPath {
            get { return null; }
        }
        
#endregion
        
    }
}
