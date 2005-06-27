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
 * Revision 1.1  2005/06/27 00:47:24  abock
 * Added entagged-sharp
 *
 * Revision 1.3  2005/02/08 12:54:43  kikidonk
 * Added cvs log and header
 *
 */

using System;

namespace Entagged.Audioformats.exceptions {
	public class CannotReadException : Exception {
		public CannotReadException( string message ) : base( message ) {}
		public CannotReadException() : base() {}
	}
}
