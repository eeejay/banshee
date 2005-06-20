/***************************************************************************
 *  PlayerInterface.cs
 *
 *  Copyright (C) 2005 Aaron Bockover
 *  aaron@aaronbock.net
 ****************************************************************************/

/*
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  sSee the
 *  GNU Library General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections;

namespace Sonance
{
	public class Playlist
	{
		private static int uid;
		private Hashtable songs;
		private ArrayList orderList;
		
		public static int NextUid
		{
			get {
				return uid++;
			}
		}
		
		public Playlist()
		{
			songs = new Hashtable();
		}
		
		public int Add(TrackInfo track)
		{
			int uid = NextUid;
			
			if(orderList.IndexOf(uid) >= 0)
				throw new Exception("UID already in orderList");
				
			songs[uid] = track;
			orderList.Add(uid);
			
			return uid;
		}
		
		public void Remove(int uid)
		{
			if(songs.ContainsKey(uid))
				songs.Remove(uid);
				
			orderList.Remove(uid);
		}
		
		
		
	}
}
