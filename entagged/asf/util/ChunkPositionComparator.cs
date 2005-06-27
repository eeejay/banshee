/*
 *  ********************************************************************   **
 *  Copyright notice                                                       **
 *  **																	   **
 *  (c) 2003 Entagged Developpement Team				                   **
 *  http://www.sourceforge.net/projects/entagged                           **
 *  **																	   **
 *  All rights reserved                                                    **
 *  **																	   **
 *  This script is part of the Entagged project. The Entagged 			   **
 *  project is free software; you can redistribute it and/or modify        **
 *  it under the terms of the GNU General Public License as published by   **
 *  the Free Software Foundation; either version 2 of the License, or      **
 *  (at your option) any later version.                                    **
 *  **																	   **
 *  The GNU General Public License can be found at                         **
 *  http://www.gnu.org/copyleft/gpl.html.                                  **
 *  **																	   **
 *  This copyright notice MUST APPEAR in all copies of the file!           **
 *  ********************************************************************
 */
namespace entagged.audioformats.asf.util {


using System.Collections;
using entagged.audioformats.asf.data;

/**
 * This class is needed for ordering all types of
 * {@link entagged.audioformats.asf.data.Chunk}s ascending by their Position.
 * <br>
 * 
 * @author Christian Laireiter
 */
public class ChunkPositionComparator : IComparer {

	/**
	 * (overridden)
	 * 
	 * @see java.util.Comparator#compare(java.lang.Object, java.lang.Object)
	 */
	public int compare(object o1, object o2) {
		int result = 0;
		if (o1 is Chunk && o2 is Chunk) {
			Chunk c1 = (Chunk) o1;
			Chunk c2 = (Chunk) o2;
			result = (int) (c1.getPosition() - c2.getPosition());
		}
		return result;
	}

}
}

