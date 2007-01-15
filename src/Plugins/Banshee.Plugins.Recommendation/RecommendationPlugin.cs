/***************************************************************************
 *  RecommendationPlugin.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Fredrik Hedberg
 *             Aaron Bockover
 *             Lukas Lipka
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
using Gtk;

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.MediaEngine;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Recommendation.RecommendationPlugin)
        };
    }
}

namespace Banshee.Plugins.Recommendation
{
    public class RecommendationPlugin : Banshee.Plugins.Plugin
    {
        protected override string ConfigurationName { get { return "recommendation"; } }
        public override string DisplayName { get { return Catalog.GetString ("Music Recommendations"); } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically recommends music that you might like, based on the currently " +
                    "playing song. It finds artists and popular songs that others with similar " +
                    "musical tastes enjoy."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] {
                    "Fredrik Hedberg",
                    "Aaron Bockover",
                    "Lukas Lipka"
                };
            }
        }
        
        // --------------------------------------------------------------- //
                
        private RecommendationPane recommendation_pane;
        private ActionGroup actions;
        private uint ui_manager_id;
        private string current_artist;
        
        protected override void PluginInitialize()
        {    
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
            SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            
            if(recommendation_pane != null && ValidTrack) {
                ShowRecommendations(PlayerEngineCore.CurrentTrack.Artist);
            }
        }

        protected override void InterfaceInitialize()
        {
            InstallInterfaceActions();
        }
        
        protected override void PluginDispose()
        {
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
            
            PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;
            SourceManager.ActiveSourceChanged -= OnActiveSourceChanged;
            
            if(PaneVisible) {
                HideRecommendations();
            }
            
            if(recommendation_pane != null) {
                recommendation_pane.Destroy();
                recommendation_pane = null;
            }
        }
        
        // --------------------------------------------------------------- //
        
        private void InstallInterfaceElements()
        {
            recommendation_pane = new RecommendationPane();
            InterfaceElements.MainContainer.PackEnd(recommendation_pane, false, false, 0);
        }

        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Recommendation");
            
            actions.Add(new ToggleActionEntry [] {
                new ToggleActionEntry("ShowRecommendationAction", null,
                    Catalog.GetString("Show Recommendations"), "<control>R",
                    Catalog.GetString("Show Recommendations"), OnToggleShow, true)
            });
            
            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("RecommendationMenu.xml");
        }
        
        private void OnToggleShow(object o, EventArgs args) 
        {
            Enabled = (o as ToggleAction).Active;
        }

        // --------------------------------------------------------------- //
        
        private bool enabled = true;
        public bool Enabled {
            get { return enabled; }
            set { 
                if(enabled && !value && PaneVisible) {
                    HideRecommendations();
                } else if(!enabled && value && ValidTrack) {
                    ShowRecommendations(PlayerEngineCore.CurrentTrack.Artist);
                }
                
                enabled = value;
            }
        }

        public bool ValidTrack {
            get {
                return (PlayerEngineCore.CurrentTrack != null &&
                    PlayerEngineCore.CurrentTrack.Artist != null && 
                    PlayerEngineCore.CurrentTrack.Artist != String.Empty);
            }
        }

        // --------------------------------------------------------------- //

        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            if(!Enabled) {
                return; 
            }
            
            switch(args.Event) {
                case PlayerEngineEvent.TrackInfoUpdated:
                    if(ValidTrack && current_artist != PlayerEngineCore.CurrentTrack.Artist) {
                        ShowRecommendations(PlayerEngineCore.CurrentTrack.Artist);
                    }
                    break;
                    
                case PlayerEngineEvent.StartOfStream:
                    if(ValidTrack) {
                        ShowRecommendations(PlayerEngineCore.CurrentTrack.Artist);
                    }
                    break;
                
                case PlayerEngineEvent.EndOfStream:
                    if(PaneVisible) {
                        HideRecommendations();
                    }
                    break;
            }
        }

        private Source displayed_on_source;
        private string displayed_on_artist;
        
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            PaneVisible = args.Source == displayed_on_source && 
                displayed_on_artist == recommendation_pane.CurrentArtist;
        }

        private bool PaneVisible {
            get {
                if(recommendation_pane == null) {
                    return false;
                }
                
                return recommendation_pane.Visible;
            }

            set {
                if(recommendation_pane == null) {
                    return;
                }
                
                recommendation_pane.Visible = value;
            }
        }

        private void ShowRecommendations(string artist)
        {
            lock(this) {
                if(recommendation_pane == null) {
                    InstallInterfaceElements();
                }
                
                // Don't do anything if we already are showing recommendations for the
                // requested artist.
                if(PaneVisible && recommendation_pane.CurrentArtist == artist) {
                    return;
                }
                
                current_artist = artist;
                
                // If we manually switch track we don't get an EndOfStream event and 
                // must clear the recommendation pane here.
                if(PaneVisible) {
                    HideRecommendations();
                }
                
                recommendation_pane.ShowRecommendations(artist);

                displayed_on_source = SourceManager.ActiveSource;
                displayed_on_artist = artist;
            }
        }
        
        private void HideRecommendations()
        {
            recommendation_pane.HideRecommendations();
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.recommendation", "enabled",
            true,
            "Plugin enabled",
            "Recommendation plugin enabled"
        );
    }
}
