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

using Hyena;
using Hyena.Data;
using Hyena.Query;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;

namespace Banshee.Sources
{
    public abstract class Source : ISource
    {
        private Source parent;
        private PropertyStore properties = new PropertyStore ();
        protected SourceMessage status_message;
        private List<SourceMessage> messages = new List<SourceMessage> ();
        private List<Source> child_sources = new List<Source> ();
        private ReadOnlyCollection<Source> read_only_children;

        private SourceSortType child_sort;
        private bool sort_children = true;
        private SchemaEntry<string> child_sort_schema;
        private SchemaEntry<bool> separate_by_type_schema;

        public event EventHandler Updated;
        public event EventHandler UserNotifyUpdated;
        public event EventHandler MessageNotify;
        public event SourceEventHandler ChildSourceAdded;
        public event SourceEventHandler ChildSourceRemoved;

        public delegate void OpenPropertiesDelegate ();

        protected Source (string generic_name, string name, int order) : this (generic_name, name, order, null)
        {
        }

        protected Source (string generic_name, string name, int order, string type_unique_id) : this ()
        {
            GenericName = generic_name;
            Name = name;
            Order = order;
            TypeUniqueId = type_unique_id;

            SourceInitialize ();
        }

        protected Source ()
        {
            child_sort = DefaultChildSort;
        }

        // This method is chained to subclasses intialize methods,
        // allowing at any state for delayed intialization by using the empty ctor.
        protected virtual void Initialize ()
        {
            SourceInitialize ();
        }

        private void SourceInitialize ()
        {
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

            LoadSortSchema ();
        }
        
        protected void OnSetupComplete ()
        {
            /*ITrackModelSource tm_source = this as ITrackModelSource;
            if (tm_source != null) {
                tm_source.TrackModel.Parent = this;
                ServiceManager.DBusServiceManager.RegisterObject (tm_source.TrackModel);
                
                // TODO if/when browsable models can be added/removed on the fly, this would need to change to reflect that
                foreach (IListModel model in tm_source.FilterModels) {
                    Banshee.Collection.ExportableModel exportable = model as Banshee.Collection.ExportableModel;
                    if (exportable != null) {
                        exportable.Parent = this;
                        ServiceManager.DBusServiceManager.RegisterObject (exportable);
                    }
                }
            }*/
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
        
        protected void PauseSorting ()
        {
            sort_children = false;
        }
        
        protected void ResumeSorting ()
        {
            sort_children = true;
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

        public virtual bool AcceptsUserInputFromSource (Source source)
        {
            return AcceptsInputFromSource (source);
        }
        
        public virtual void MergeSourceInput (Source source, SourceMergeType mergeType)
        {
            Log.ErrorFormat ("MergeSourceInput not implemented by {0}", this);
        }
        
        public virtual SourceMergeType SupportedMergeTypes {
            get { return SourceMergeType.None; }
        }
        
        public virtual void SetParentSource (Source parent)
        {
            this.parent = parent;
        }
        
        public virtual bool ContainsChildSource (Source child)
        {
            lock (Children) {
                return child_sources.Contains (child);
            }
        }
        
        public virtual void AddChildSource (Source child)
        {
            lock (Children) {
                if (!child_sources.Contains (child)) {
                    child.SetParentSource (this);
                    child_sources.Add (child);
                    OnChildSourceAdded (child);
                }
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
                    if (CanActivate) {
                        ServiceManager.SourceManager.SetActiveSource (this);
                    }
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

        private class SizeComparer : IComparer<Source>
        {
            public int Compare (Source a, Source b)
            {
                return a.Count.CompareTo (b.Count);
            }
        }

        public virtual void SortChildSources (SourceSortType sort_type)
        {
            child_sort = sort_type;
            child_sort_schema.Set (child_sort.Id);
            SortChildSources ();
        }

        public virtual void SortChildSources ()
        {
            lock (this) {
                if (!sort_children) {
                    return;
                }
                sort_children = false;
            }
            
            if (child_sort != null && child_sort.SortType != SortType.None) {
                lock (Children) {
                    child_sort.Sort (child_sources, SeparateChildrenByType);

                    int i = 0;
                    foreach (Source child in child_sources) {
                        child.Order = i++;
                    }
                }
            }
            sort_children = true;
        }
        
        private void LoadSortSchema ()
        {
            if (ChildSortTypes.Length == 0) {
                return;
            }

            if (unique_id == null && type_unique_id == null) {
                Hyena.Log.WarningFormat ("Trying to LoadSortSchema, but source's id not set! {0}", UniqueId);
                return;
            }
            
            child_sort_schema = CreateSchema<string> ("child_sort_id", DefaultChildSort.Id, "", "");
            string child_sort_id = child_sort_schema.Get ();
            foreach (SourceSortType sort_type in ChildSortTypes) {
                if (sort_type.Id == child_sort_id) {
                    child_sort = sort_type;
                    break;
                }
            }

            separate_by_type_schema = CreateSchema<bool> ("separate_by_type", false, "", "");
            SortChildSources ();
        }

        public T GetProperty<T> (string name, bool propagate)
        {
            return propagate ? GetInheritedProperty<T> (name) : Properties.Get<T> (name);
        }
        
        public T GetInheritedProperty<T> (string name)
        {
            return Properties.Contains (name)
                ? Properties.Get<T> (name)
                : Parent != null 
                    ? Parent.GetInheritedProperty<T> (name)
                    : default (T);
        }
        
#endregion
        
#region Protected Methods
        
        public virtual void SetStatus (string message, bool error)
        {
            SetStatus (message, !error, !error, error ? "dialog-error" : null);
        }

        public virtual void SetStatus (string message, bool can_close, bool is_spinning, string icon_name)
        {
            lock (this) {
                if (status_message == null) {
                    status_message = new SourceMessage (this);
                    PushMessage (status_message);
                }
            
                string status_name = String.Format ("<i>{0}</i>", GLib.Markup.EscapeText (Name));
                
                status_message.FreezeNotify ();
                status_message.Text = String.Format (GLib.Markup.EscapeText (message), status_name);
                status_message.CanClose = can_close;
                status_message.IsSpinning = is_spinning;
                status_message.SetIconName (icon_name);
                status_message.ClearActions ();
            }
                
            status_message.ThawNotify ();
        }

        public virtual void HideStatus ()
        {
            lock (this) {
                if (status_message != null) {
                    RemoveMessage (status_message);
                    status_message = null;
                }
            }
        }


        protected virtual void PushMessage (SourceMessage message)
        {
            lock (this) {
                messages.Insert (0, message);
                message.Updated += HandleMessageUpdated;
            }
            
            OnMessageNotify ();
        }
        
        protected virtual SourceMessage PopMessage ()
        {
            try {
                lock (this) {
                    if (messages.Count > 0) {
                        SourceMessage message = messages[0];
                        message.Updated -= HandleMessageUpdated;
                        messages.RemoveAt (0);
                        return message;
                    }
                    
                    return null;
                }
            } finally {
                OnMessageNotify ();
            }
        }
        
        protected virtual void ClearMessages ()
        {
            lock (this) {
                if (messages.Count > 0) {
                    foreach (SourceMessage message in messages) {
                        message.Updated -= HandleMessageUpdated;
                    }
                    messages.Clear ();
                    OnMessageNotify ();
                }
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
                    message.Updated -= HandleMessageUpdated;
                    OnMessageNotify ();
                }
            }   
        }

        private void HandleMessageUpdated (object o, EventArgs args)
        {
            OnMessageNotify ();
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
            SortChildSources ();
            source.Updated += OnChildSourceUpdated;
            ThreadAssist.ProxyToMain (delegate {
                SourceEventHandler handler = ChildSourceAdded;
                if (handler != null) {
                    SourceEventArgs args = new SourceEventArgs ();
                    args.Source = source;
                    handler (args);
                }
            });
        }
        
        protected virtual void OnChildSourceRemoved (Source source)
        {
            source.Updated -= OnChildSourceUpdated;
            ThreadAssist.ProxyToMain (delegate {
                SourceEventHandler handler = ChildSourceRemoved;
                if (handler != null) {
                    SourceEventArgs args = new SourceEventArgs ();
                    args.Source = source;
                    handler (args);
                }
            });
        }
        
        protected virtual void OnUpdated ()
        {
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnChildSourceUpdated (object o, EventArgs args)
        {
            SortChildSources ();
        }

        public void NotifyUser ()
        {
            OnUserNotifyUpdated ();
        }

        protected void OnUserNotifyUpdated ()
        {
            if (this != ServiceManager.SourceManager.ActiveSource) {
                EventHandler handler = UserNotifyUpdated;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
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

        public virtual string TypeName {
            get { return GetType ().Name; }
        }
        
        private string unique_id;
        public string UniqueId {
            get {
                if (unique_id == null && type_unique_id == null) {
                    Log.ErrorFormat ("Creating Source.UniqueId for {0}, but TypeUniqueId is null; trace is {1}", this.Name, System.Environment.StackTrace);
                }
                return unique_id ?? (unique_id = String.Format ("{0}-{1}", this.GetType ().Name, TypeUniqueId));
            }
        }
        
        private string type_unique_id;
        protected string TypeUniqueId {
            get { return type_unique_id; }
            set { type_unique_id = value; }
        }

        public virtual bool CanRename {
            get { return false; }
        }

        public virtual bool HasProperties {
            get { return false; }
        }
        
        public virtual bool HasViewableTrackProperties {
            get { return false; }
        }
        
        public virtual bool HasEditableTrackProperties {
            get { return false; }
        }

        public virtual string Name {
            get { return properties.Get<string> ("Name"); }
            set { properties.SetString ("Name", value); }
        }

        public virtual string GenericName {
            get { return properties.Get<string> ("GenericName"); }
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
            get { return false; }
        }
                
        public virtual string FilterQuery {
            get { return properties.Get<string> ("FilterQuery"); }
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
        
        public virtual int Count {
            get { return 0; }
        }

        private string parent_conf_id;
        public string ParentConfigurationId {
            get {
                if (parent_conf_id == null) {
                    parent_conf_id = (Parent ?? this).UniqueId.Replace ('.', '_');
                }
                return parent_conf_id;
            }
        }

        private string conf_id;
        public string ConfigurationId {
            get { return conf_id ?? (conf_id = UniqueId.Replace ('.', '_')); }
        }

        public virtual int FilteredCount { get { return Count; } }
                
        public virtual string TrackModelPath {
            get { return null; }
        }
        
        public static readonly SourceSortType SortNameAscending = new SourceSortType (
            "NameAsc",
            Catalog.GetString ("Name"),
            SortType.Ascending, null); // null comparer b/c we already fall back to sorting by name
        
        public static readonly SourceSortType SortSizeAscending = new SourceSortType (
            "SizeAsc",
            Catalog.GetString ("Size Ascending"),
            SortType.Ascending, new SizeComparer ());
        
        public static readonly SourceSortType SortSizeDescending = new SourceSortType (
            "SizeDesc",
            Catalog.GetString ("Size Descending"),
            SortType.Descending, new SizeComparer ());
        
        private static SourceSortType[] sort_types = new SourceSortType[] {};
        public virtual SourceSortType[] ChildSortTypes {
            get { return sort_types; }
        }
        
        public SourceSortType ActiveChildSort {
            get { return child_sort; }
        }
        
        public virtual SourceSortType DefaultChildSort {
            get { return null; }
        }

        public bool SeparateChildrenByType {
            get { return separate_by_type_schema.Get (); }
            set {
                separate_by_type_schema.Set (value);
                SortChildSources ();
            }
        }
        
#endregion

#region Status Message Stuff        
        
        private static DurationStatusFormatters duration_status_formatters = new DurationStatusFormatters ();
        public static DurationStatusFormatters DurationStatusFormatters {
            get { return duration_status_formatters; }
        }
        
        protected virtual int StatusFormatsCount {
            get { return duration_status_formatters.Count; }
        }
        
        public virtual int CurrentStatusFormat {
            get { return ConfigurationClient.Get<int> (String.Format ("sources.{0}", ParentConfigurationId), "status_format", 0); }
            set { ConfigurationClient.Set<int> (String.Format ("sources.{0}", ParentConfigurationId), "status_format", value); }
        }
        
        public SchemaEntry<T> CreateSchema<T> (string name)
        {
            return CreateSchema<T> (name, default(T), null, null);
        }
        
        public SchemaEntry<T> CreateSchema<T> (string name, T defaultValue, string shortDescription, string longDescription)
        {
            return new SchemaEntry<T> (String.Format ("sources.{0}", ParentConfigurationId), name, defaultValue, shortDescription, longDescription); 
        }
        
        public SchemaEntry<T> CreateSchema<T> (string ns, string name, T defaultValue, string shortDescription, string longDescription)
        {
            return new SchemaEntry<T> (String.Format ("sources.{0}.{1}", ParentConfigurationId, ns), name, defaultValue, shortDescription, longDescription); 
        }
        
        public void CycleStatusFormat ()
        {
            int new_status_format = CurrentStatusFormat + 1;
            if (new_status_format >= StatusFormatsCount) {
                new_status_format = 0;
            }
            
            CurrentStatusFormat = new_status_format;
        }

        private const string STATUS_BAR_SEPARATOR = " \u2013 ";
        public virtual string GetStatusText ()
        {
            StringBuilder builder = new StringBuilder ();

            int count = FilteredCount;
            
            if (count == 0) {
                return String.Empty;
            }
            
            builder.AppendFormat (Catalog.GetPluralString ("{0} item", "{0} items", count), count);
            
            if (this is IDurationAggregator && StatusFormatsCount > 0) {
                builder.Append (STATUS_BAR_SEPARATOR);
                duration_status_formatters[CurrentStatusFormat] (builder, ((IDurationAggregator)this).Duration);
            }

            if (this is IFileSizeAggregator) {
                long bytes = (this as IFileSizeAggregator).FileSize;
                if (bytes > 0) {
                    builder.Append (STATUS_BAR_SEPARATOR);
                    builder.AppendFormat (new FileSizeQueryValue (bytes).ToUserQuery ());
                }
            }
            
            return builder.ToString ();
        }
        
#endregion

        public override string ToString ()
        {
            return Name;
        }
        
        /*string IService.ServiceName {
            get { return String.Format ("{0}{1}", DBusServiceManager.MakeDBusSafeString (Name), "Source"); }
        }*/
        
        // FIXME: Replace ISource with IDBusExportable when it's enabled again
        ISource ISource.Parent {
            get {
                if (Parent != null) {
                    return ((Source)this).Parent;
                } else {
                    return null /*ServiceManager.SourceManager*/;
                }
            }
        }
    }
}
