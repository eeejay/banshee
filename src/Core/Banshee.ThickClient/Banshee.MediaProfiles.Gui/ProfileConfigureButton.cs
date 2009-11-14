/***************************************************************************
 *  ProfileConfigureButton.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

namespace Banshee.MediaProfiles.Gui
{
    public class ProfileConfigureButton : Button
    {
        private ProfileComboBox combo;
        private string configuration_id;

        public ProfileConfigureButton(string configurationId) : base(Stock.Edit)
        {
            this.configuration_id = configurationId;
        }

        protected override void OnClicked()
        {
            Profile profile = combo.ActiveProfile;
            profile.LoadConfiguration(configuration_id);

            if(profile != null) {
                ProfileConfigurationDialog dialog = new ProfileConfigurationDialog(profile);
                dialog.Run();
                dialog.Destroy();
                profile.SaveConfiguration();
            }
        }

        private void OnComboUpdated(object o, EventArgs args)
        {
            if(combo != null && combo.ActiveProfile != null && combo.ActiveProfile.Pipeline != null) {
                Sensitive = combo.Sensitive && combo.ActiveProfile.Pipeline.VariableCount > 0;
            } else {
                Sensitive = false;
            }
        }

        public ProfileComboBox ComboBox {
            get { return combo; }
            set {
                if(combo == value) {
                    return;
                } else if(combo != null) {
                    combo.Updated -= OnComboUpdated;
                    combo.Changed -= OnComboUpdated;
                }

                combo = value;

                if(combo != null) {
                    combo.Updated += OnComboUpdated;
                    combo.Changed += OnComboUpdated;
                    OnComboUpdated(null, null);
                }
            }
        }
    }
}
