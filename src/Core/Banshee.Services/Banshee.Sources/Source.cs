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
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;

using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.Sources
{
    public abstract class Source : ISource
    {
        private PropertyStore properties = new PropertyStore();
        private List<Source> child_sources = new List<Source>();

        public event EventHandler Updated;
        public event SourceEventHandler ChildSourceAdded;
        public event SourceEventHandler ChildSourceRemoved;
        
        protected Source(string name, int order)
        {
            properties.SetString("Name", name);
            properties.SetInteger("Order", order);
            
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
        
        public virtual bool Unmap()
        {
            return false;
        }
        
        public virtual void AddChildSource(ChildSource source)
        {
            lock(Children) {
                source.SetParentSource(this);
                child_sources.Add(source);
                OnChildSourceAdded(source);
            }
        }

        public virtual void RemoveChildSource(ChildSource source)
        {
            lock(Children) {
                if (source.Children.Count > 0) {
                    source.ClearChildSources();
                }
                
                child_sources.Remove(source);
                
                if(ServiceManager.SourceManager.ActiveSource == source) {
                    ServiceManager.SourceManager.SetActiveSource(ServiceManager.SourceManager.DefaultSource);
                }
                
                OnChildSourceRemoved(source);
            }
        }
        
        public virtual void ClearChildSources()
        {
            lock(Children) {
                while(child_sources.Count > 0) {
                    RemoveChildSource(child_sources[child_sources.Count - 1] as ChildSource);
                }
            }
        }
        
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

        public string Name {
            get { return properties.GetString("Name"); }
            set { properties.SetString("Name", value); }
        }
        
        public int Order {
            get { return properties.GetInteger("Order"); }
        }
        
        public virtual bool ImplementsCustomSearch {
            get { return false; }
        }
        
        public virtual bool CanSearch {
            get { return true; }
        }
                
        public string FilterQuery {
            get { return properties.GetString("FilterQuery"); }
            set { properties.SetString("FilterQuery", value); }
        }
        
        public TrackFilterType FilterType {
            get { return (TrackFilterType)properties.GetInteger("FilterType"); }
            set { properties.SetInteger("FilterType", (int)value); }
        }
        
        public virtual bool Expanded {
            get { return properties.GetBoolean("Expanded"); }
            set {
                if(Expanded == value) {
                    return;
                }
                
                properties.SetBoolean("Expanded", value);
            }
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
        
        string IService.ServiceName {
            get { return DBusServiceManager.MakeDBusSafeString(Name) + "Source"; }
        }
        
        IDBusExportable IDBusExportable.Parent {
            get { 
                if(this is ChildSource) {
                    return ((ChildSource)this).Parent;
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
