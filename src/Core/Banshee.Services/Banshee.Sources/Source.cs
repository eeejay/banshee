//
// Source.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Unix;

using Hyena.Data;
using Hyena.Query;

using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.Sources
{
    public abstract class Source : ISource
    {
        private Source parent;
        private PropertyStore properties = new PropertyStore ();
        private List<SourceMessage> messages = new List<SourceMessage> ();
        private List<Source> child_sources = new List<Source> ();
        private ReadOnlyCollection<Source> read_only_children;

        public event EventHandler Updated;
        public event EventHandler UserNotifyUpdated;
        public event EventHandler MessageNotify;
        public event SourceEventHandler ChildSourceAdded;
        public event SourceEventHandler ChildSourceRemoved;
        
        protected Source (string generic_name, string name, int order)
        {
            GenericName = generic_name;
            Name = name;
            Order = order;

            // If this source is not defined in Banshee.Services, set its
            // ResourceAssembly to the assembly where it is defined.
            Assembly asm = Assembly.GetAssembly (this.GetType ());//Assembly.GetCallingAssembly ();
            if (asm != Assembly.GetExecutingAssembly ()) {
                Properties.Set<Assembly> ("ResourceAssembly", asm);
            }

            properties.PropertyChanged += OnPropertyChanged;
            read_only_children = new ReadOnlyCollection<Source> (child_sources);
            
            if (ApplicationContext.Debugging && ApplicationContext.CommandLine.Contains ("test-source-messages")) {
                TestMessages ();
            }
        }
        
        protected void OnSetupComplete ()
        {
            if (this is ITrackModelSource) {
                ITrackModelSource tm_source = (ITrackModelSource)this;
                
                tm_source.TrackModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject (tm_source.TrackModel);
                
                tm_source.ArtistModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject (tm_source.ArtistModel);
                
                tm_source.AlbumModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject (tm_source.AlbumModel);
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
        
        public virtual void Activate ()
        {
        }

        public virtual void Deactivate ()
        {
        }

        public virtual void Rename (string newName)
        {
            properties.SetString ("Name", newName);
        }
        
        public virtual bool AcceptsInputFromSource (Source source)
        {
            return false;
        }
        
        public virtual void MergeSourceInput (Source source, SourceMergeType mergeType)
        {
            throw new NotImplementedException ();
        }
        
        public virtual SourceMergeType SupportedMergeTypes {
            get { return SourceMergeType.None; }
        }
        
        public virtual void SetParentSource (Source parent)
        {
            this.parent = parent;
        }
        
        public virtual void AddChildSource (Source child)
        {
            lock (Children) {
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
                    ServiceManager.SourceManager.SetActiveSource (ServiceManager.SourceManager.DefaultSource);
                }
                
                OnChildSourceRemoved (child);
            }
        }
        
        public virtual void ClearChildSources ()
        {
            lock (Children) {
                while (child_sources.Count > 0) {
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

        public virtual void SortChildSources (IComparer<Source> comparer, bool asc)
        {
            lock (Children) {
                child_sources.Sort (comparer);
                if (!asc) {
                    child_sources.Reverse ();
                }

                int i = 0;
                foreach (Source child in child_sources) {
                    child.Order = i++;
                }
            }
        }
        
#endregion
        
#region Protected Methods

        protected virtual void PushMessage (SourceMessage message)
        {
            lock (this) {
                messages.Insert (0, message);
            }
            
            OnMessageNotify ();
        }
        
        protected virtual SourceMessage PopMessage ()
        {
            try {
                lock (this) {
                    if (messages.Count > 0) {
                        SourceMessage message = messages[0];
                        messages.RemoveAt (0);
                        return message;
                    }
                    
                    return null;
                }
            } finally {
                OnMessageNotify ();
            }
        }
        
        private void TestMessages ()
        {
            int count = 0;
            SourceMessage message_3 = null;
            
            Application.RunTimeout (5000, delegate {
                if (count++ > 5) {
                    if (count == 7) {
                        RemoveMessage (message_3);
                    }
                    PopMessage ();
                    return true;
                } else if (count > 10) {
                    return false;
                }
                
                SourceMessage message = new SourceMessage (this);
                message.FreezeNotify ();
                message.Text = String.Format ("Testing message {0}", count);
                message.IsSpinning = count % 2 == 0;
                message.CanClose = count % 2 == 1;
                if (count % 3 == 0) {
                    for (int i = 2; i < count; i++) {
                        message.AddAction (new MessageAction (String.Format ("Button {0}", i)));
                    }
                }
                    
                message.ThawNotify ();
                PushMessage (message);
                
                if (count == 3) {
                    message_3 = message;
                }
                
                return true;
            });
        }
        
        protected virtual void RemoveMessage (SourceMessage message)
        {
            lock (this) {
                if (messages.Remove (message)) {
                    OnMessageNotify ();
                }
            }   
        }
        
        protected virtual void OnMessageNotify ()
        {
            EventHandler handler = MessageNotify;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    
        protected virtual void OnChildSourceAdded (Source source)
        {
            SourceEventHandler handler = ChildSourceAdded;
            if (handler != null) {
                SourceEventArgs args = new SourceEventArgs ();
                args.Source = source;
                handler (args);
            }
        }
        
        protected virtual void OnChildSourceRemoved (Source source)
        {
            SourceEventHandler handler = ChildSourceRemoved;
            if (handler != null) {
                SourceEventArgs args = new SourceEventArgs ();
                args.Source = source;
                handler (args);
            }
        }
        
        protected virtual void OnUpdated ()
        {
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnUserNotifyUpdated ()
        {
            EventHandler handler = UserNotifyUpdated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
#endregion
        
#region Private Methods
        
        private void OnPropertyChanged (object o, PropertyChangeEventArgs args)
        {
            OnUpdated ();
        }
        
#endregion
        
#region Public Properties
        
        public ReadOnlyCollection<Source> Children {
            get { return read_only_children; }
        }
        
        string [] ISource.Children {
            get { return null; }
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
            get { return properties.GetString ("Name"); }
            set { properties.SetString ("Name", value); }
        }

        public string GenericName {
            get { return properties.GetString ("GenericName"); }
            set { properties.SetString ("GenericName", value); }
        }
        
        public int Order {
            get { return properties.GetInteger ("Order"); }
            set { properties.SetInteger ("Order", value); }
        }
        
        public SourceMessage CurrentMessage {
            get { lock (this) { return messages.Count > 0 ? messages[0] : null; } }
        }

        public virtual bool ImplementsCustomSearch {
            get { return false; }
        }
        
        public virtual bool CanSearch {
            get { return true; }
        }
                
        public virtual string FilterQuery {
            get { return properties.GetString ("FilterQuery"); }
            set { properties.SetString ("FilterQuery", value); }
        }
        
        public TrackFilterType FilterType {
            get { return (TrackFilterType)properties.GetInteger ("FilterType"); }
            set { properties.SetInteger ("FilterType", (int)value); }
        }
        
        public virtual bool Expanded {
            get { return properties.GetBoolean ("Expanded"); }
            set { properties.SetBoolean ("Expanded", value); }
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
            
            builder.AppendFormat (Catalog.GetPluralString ("{0} item", "{0} items", count), count);
            
            if (this is IDurationAggregator) {
                TimeSpan span = (this as IDurationAggregator).Duration;
                
                if (span.Days > 0) {
                    builder.Append (", ");
                    builder.AppendFormat (Catalog.GetPluralString ("{0} day", "{0} days", 
                        (int) span.Days), span.Days);
                }
                
                if (span.Hours > 0) {
                    builder.Append (", ");
                    builder.AppendFormat (Catalog.GetPluralString ("{0} hour", "{0} hours", 
                        (int) span.Hours), span.Hours);
                }
                
                builder.Append (", ");
                
                builder.AppendFormat (Catalog.GetPluralString ("{0} minute", "{0} minutes", 
                    (int) span.Minutes), span.Minutes);
                
                builder.Append (", ");
                builder.AppendFormat (Catalog.GetPluralString ("{0} second", "{0} seconds",
                    (int) span.Seconds), span.Seconds);
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
            get { return DBusServiceManager.MakeDBusSafeString (Name) + "Source"; }
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
