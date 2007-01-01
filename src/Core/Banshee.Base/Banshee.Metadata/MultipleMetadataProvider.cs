/***************************************************************************
 *  MultipleMetadataProvider.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
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

namespace Banshee.Metadata
{
    public class MultipleMetadataProvider : IMetadataProvider
    {
        private static MultipleMetadataProvider instance;
        public static MultipleMetadataProvider Instance {
            get {
                if(instance == null) {
                    instance = new MultipleMetadataProvider();
                }
                
                return instance;
            }
        }
    
        private IMetadataProvider [] providers;
        
        public MultipleMetadataProvider()
        {
            providers = new IMetadataProvider[MetadataProviderFactory.Providers.Length];
            
            for(int i = 0; i < providers.Length; i++) {
                providers[i] = MetadataProviderFactory.CreateProvider(MetadataProviderFactory.Providers[i]);
            }
        }
    
        public event MetadataLookupResultHandler HaveResult {
            add {
                foreach(IMetadataProvider provider in providers) {
                    provider.HaveResult += value;
                }
            }
            
            remove {
                foreach(IMetadataProvider provider in providers) {
                    provider.HaveResult -= value;
                }
            }
        }
        
        public void Lookup(IBasicTrackInfo track)
        {
            foreach(IMetadataProvider provider in providers) {
                provider.Lookup(track);
            }
        }
        
        public void Cancel(IBasicTrackInfo track)
        {
            foreach(IMetadataProvider provider in providers) {
                provider.Cancel(track);
            }
        }

        public void Cancel()
        {
            foreach(IMetadataProvider provider in providers) {
                provider.Cancel();
            }
        }
    }
}
