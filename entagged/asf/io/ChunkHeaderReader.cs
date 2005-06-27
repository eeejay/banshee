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
namespace entagged.audioformats.asf.io {
using System.IO;
using entagged.audioformats.asf.data;
using entagged.audioformats.asf.util;

/**
 * Default reader, Reads GUID and size out of an inputsream and creates a
 * {@link entagged.audioformats.asf.data.Chunk}object.
 * 
 * @author Christian Laireiter
 */
class ChunkHeaderReader {

	/**
	 * Interprets current data as a header of a chunk.
	 * 
	 * @param input
	 *                  inputdata
	 * @return Chunk.
	 * @throws IOException
	 *                   Access errors.
	 */
	public static Chunk readChunckHeader(FileStream input)
			{
		long pos = input.Position;
		GUID guid = Utils.readGUID(input);
		ulong chunkLength = Utils.readBig64(input);
		return new Chunk(guid, pos, chunkLength);
	}

}
}

