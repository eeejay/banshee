/***************************************************************************
 *  PodcastPlugin.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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

using Mono.Gettext;

using Banshee.Base;
using Banshee.Plugins.Podcast.UI;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Podcast.PodcastPlugin)
        };
    }
}

namespace Banshee.Plugins.Podcast
{
    public class PodcastPlugin : Banshee.Plugins.Plugin
    {
        private ActionGroup actions;
        private uint ui_manager_id;

        protected override string ConfigurationName { get { return "podcast"; } }
        public override string DisplayName { get { return Catalog.GetString("Podcasting"); } }

        public override string Description {
            get
            {
                return String.Format(Catalog.GetString(
                    "Podcasting is a form of audio blogging where users subscribe to a feed of shows and " +
                    "its episodes are downloaded and managed for offline listening.\n\nIts name comes from " +
                    "the targeting of audio posts to Apple's iPod\u00ae audio player, although podcasts " + 
                    "can be listened to directly in {0}."), Branding.ApplicationName);
            }
        }

        public override string [] Authors {
            get
            {
                return new string [] {
                           "Mike Urbanski"
                       };
            }
        }

        protected override void PluginInitialize ()
        {       
        }

        protected override void InterfaceInitialize ()
        {
            InstallInterfaceActions ();
            PodcastCore.Initialize (this);
        }

        protected override void PluginDispose ()
        {
            Globals.ActionManager.UI.RemoveUi (ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup (actions);

            actions = null;
            PodcastCore.Dispose ();
        }


        // TODO later add option for max downloads / download directory
        /*
        public override Widget GetConfigurationWidget()
        {
            return new PodcastConfigPage ();
        }
        */

        private void InstallInterfaceActions ()
        {
            actions = new ActionGroup("Podcast");

            // Pixbufs in 'PodcastPixbufs' should be registered with the StockManager and used here.
            actions.Add (new ActionEntry [] {
                             new ActionEntry ("PodcastAction", null,
                                              Catalog.GetString ("Podcast"), null,
                                              Catalog.GetString ("Manage the Podcast plugin"), null),

                             new ActionEntry ("PodcastUpdateFeedsAction", Stock.Refresh,
                                              Catalog.GetString ("Update Feeds"), "<control><shift>U",
                                              Catalog.GetString ("Update Subscribed Podcast Feeds"),
                                              OnPodcastUpdateFeedsHandler),

                             new ActionEntry ("PodcastSubscribeAction", Stock.New,
                                              Catalog.GetString ("Subscribe to New Feed"), "<control>F",
                                              Catalog.GetString ("Subscribe to New Podcast Feed"),
                                              OnPodcastSubscribeHandler),

                             new ActionEntry ("PodcastVisitPodcastAlleyAction", Stock.JumpTo,
                                              Catalog.GetString ("Find New Podcasts"), "<control>P",
                                              Catalog.GetString ("Find New Podcasts at PodcastAlley.com"),
                                              OnVisitPodcastAlleyHandler),
                         });

            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("PodcastMenu.xml");
        }

        private void OnPodcastUpdateFeedsHandler (object sender, EventArgs args)
        {
            PodcastCore.UpdateAllFeeds ();
        }

        private void OnPodcastSubscribeHandler (object sender, EventArgs args)
        {
            PodcastCore.RunSubscribeDialog ();
        }

        private void OnVisitPodcastAlleyHandler (object sender, EventArgs args)
        {
            PodcastCore.VisitPodcastAlley ();
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.podcast", "enabled",
            true,
            "Plugin enabled",
            "Podcast plugin enabled"
        );
    }
}
