/***************************************************************************
 *  Profile.cs
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
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Banshee.MediaProfiles
{
    public class Profile
    {
        private List<string> mimetypes = new List<string>();
        private string id;
        private string name;
        private string description;
        private string output_file_extension;
        private bool? available = null;
        private Pipeline pipeline;
        private ProfileConfiguration configuration;

        internal Profile(MediaProfileManager manager, XmlNode node)
        {
            id = node.Attributes["id"].Value.Trim();
            name = Banshee.Base.Localization.SelectSingleNode(node, "name").InnerText.Trim();
            description = Banshee.Base.Localization.SelectSingleNode(node, "description").InnerText.Trim();
            output_file_extension = node.SelectSingleNode("output-file-extension").InnerText.Trim();

            foreach(XmlNode mimetype_node in node.SelectNodes("mimetype")) {
                mimetypes.Add(mimetype_node.InnerText.Trim());
            }

            pipeline = new Pipeline(manager, node.SelectSingleNode("pipeline"));
        }

        public void LoadConfiguration(string configurationId)
        {
            SetConfiguration (ProfileConfiguration.Load(this, configurationId));
        }

        public void SetConfiguration(ProfileConfiguration configuration)
        {
            this.configuration = configuration;
            foreach(KeyValuePair<string, string> variable in configuration) {
                pipeline[variable.Key] = variable.Value;
            }
        }

        public void SaveConfiguration()
        {
            SaveConfiguration(configuration.Id);
        }

        public void SaveConfiguration(string configurationId)
        {
            if(configuration == null) {
                LoadConfiguration(configurationId);
            }

            foreach(PipelineVariable variable in pipeline) {
                configuration.Add(variable.Id, variable.CurrentValue);
            }

            configuration.Save();
        }

        public bool HasMimeType(string mimetype)
        {
            return mimetypes.Contains(mimetype);
        }

        public string Id {
            get { return id; }
            set { id = value; }
        }

        public bool? Available {
            get { return available; }
            internal set { available = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Description {
            get { return description; }
            set { description = value; }
        }

        public string OutputFileExtension {
            get { return output_file_extension; }
            set { output_file_extension = value; }
        }

        public Pipeline Pipeline {
            get { return pipeline; }
            set { pipeline = value; }
        }

        public ProfileConfiguration Configuration {
            get { return configuration; }
        }

        public IList<string> MimeTypes {
            get { return mimetypes; }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(String.Format("ID          = {0}\n", Id));
            builder.Append(String.Format("Name        = {0}\n", Name));
            builder.Append(String.Format("Description = {0}\n", Description));
            builder.Append(String.Format("Extension   = {0}\n", OutputFileExtension));
            builder.Append("Pipeline    =\n");
            builder.Append(Pipeline);

            return builder.ToString();
        }
    }
}
