// Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
//
// (see http://entagged.sourceforge.net)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See
// the License for the specific language governing permissions and
// limitations under the License.

/*
 * $Log$
 * Revision 1.1  2005/06/27 00:47:26  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mpc.Util;
using Entagged.Audioformats.Ape.Util;

namespace Entagged.Audioformats.Mpc {
	public class MpcFileReader : AudioFileReader {
		
		private MpcInfoReader ir = new MpcInfoReader();
		private ApeTagReader tr = new ApeTagReader();
		
		protected override EncodingInfo GetEncodingInfo( Stream raf )  {
			return ir.Read(raf);
		}
		
		protected override Tag GetTag( Stream raf )  {
			return tr.Read(raf);
		}
	}
}
