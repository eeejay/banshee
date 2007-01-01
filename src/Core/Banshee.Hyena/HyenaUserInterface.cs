using System;
using Gtk;

using Banshee.Base;
using Banshee.Gui;
using Banshee.Sources;

namespace Banshee.Hyena
{
    public class HyenaUserInterface : Window
    {
        private HPaned source_splitter;
        private VBox source_view_container;
        private Alignment playlist_container;
        private SourceView source_view;
        private Widget playlist_view;
    
        public HyenaUserInterface() : base("Hyena")
        {
            Globals.ShutdownRequested += OnApplicationShutdownRequested;
            
            BuildInterface();
        }
        
        public void Init()
        {
            Show();
            Gtk.Application.Run();
        }
        
        private void BuildInterface()
        {
            // source view
            source_view_container = new VBox();
            
            ScrolledWindow source_view_scroll = new ScrolledWindow();
            source_view_scroll.VscrollbarPolicy = PolicyType.Automatic;
            source_view_scroll.HscrollbarPolicy = PolicyType.Never;
            source_view_scroll.ShadowType = ShadowType.In;
            
            source_view = new SourceView();
            
            source_view_scroll.Add(source_view);
            source_view_container.PackStart(source_view_scroll, true, true, 0);
            
            source_view_container.ShowAll();
            
            // playlist view
            playlist_container = new Alignment(0.0f, 0.0f, 0.0f, 0.0f);
            playlist_container.Show();
            
            playlist_view = new Button();
            playlist_view.Show();
            playlist_container.Add(playlist_view);
            
            // main container
            VBox main_container = new VBox();
            main_container.Show();
            main_container.PackStart(playlist_container, true, true, 0);
            
            // source splitter
            source_splitter = new HPaned();
            source_splitter.Add1(source_view_container);
            source_splitter.Add2(main_container);
            source_splitter.Show();
            
            Add(source_splitter);
         
            InterfaceElements.MainWindow = this;
            InterfaceElements.PlaylistContainer = playlist_container;
            InterfaceElements.MainContainer = main_container;
            InterfaceElements.PlaylistView = playlist_view;
           
            Globals.UIManager.SourceViewContainer = source_view_container;
            Globals.UIManager.Initialize();   
         }
        
        protected override bool OnDeleteEvent(Gdk.Event evnt)
        {
            Globals.Shutdown();
            return true;
        }
        
        private bool OnApplicationShutdownRequested()
        {
            Gtk.Application.Quit();
            return true;
        }        
    }
}
