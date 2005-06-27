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
 * This class reads the chunk containing encoding data <br>
 * <b>Warning:<b><br>
 * Implementation is not completed. More analysis of this chunk is needed.
 * 
 * @author Christian Laireiter
 */
public class EncodingChunkReader {

	/**
	 * This reads the current data and interprets it as an encoding chunk. <br>
	 * <b>Warning:<b><br>
	 * Implementation is not completed. More analysis of this chunk is needed.
	 * 
	 * @param raf
	 *                  Input source
	 * @param candidate
	 *                  Chunk which possibly contains encoding data.
	 * @return Encoding info. <code>null</code> if its not a valid encoding
	 *              chunk. <br>
	 * @throws IOException
	 *                   read errors.
	 */
	public static EncodingChunk read(FileStream raf, Chunk candidate)
			{
		if (raf == null || candidate == null) {
			throw new IllegalArgumentException("Arguments must not be null.");
		}
		if (GUID.GUID_ENCODING.equals(candidate.getGuid())) {
			raf.Seek(candidate.getPosition(), SeekOrigin.Begin);
			return new EncodingChunkReader().parseData(raf);
		}
		return null;
	}

	/**
	 * Should not be used for now.
	 *  
	 */
	protected EncodingChunkReader() {
		// NOTHING toDo
	}

	/**
	 * see {@link #read(FileStream, Chunk)}
	 * 
	 * @param raf
	 *                  input source.
	 * @return Enconding info. <code>null</code> if its not a valid encoding
	 *              chunk. <br>
	 * @throws IOException
	 *                   read errors.
	 */
	private EncodingChunk parseData(FileStream raf) {
		EncodingChunk result = null;
		long chunkStart = raf.Position;
		GUID guid = Utils.readGUID(raf);
		if (GUID.GUID_ENCODING.equals(guid)) {
			ulong chunkLen = Utils.readBig64(raf);
			result = new EncodingChunk(chunkStart, chunkLen);

			// Can't be interpreted
			/*
			 * What do I think of this data, well it seems to be another GUID.
			 * Then followed by a UINT16 indicating a length of data following
			 * (by half). My test files just had the length of one and a two
			 * bytes zero.
			 */
			raf.skipBytes(20);

			/*
			 * Read the number of strings which will follow
			 */
			int stringCount = Utils.readUINT16(raf);

			/*
			 * Now reading the specified amount of strings.
			 */
			for (int i = 0; i < stringCount; i++) {
				result.addstring(Utils.readCharacterSizedstring(raf));
			}
		}
		return result;
	}

}
}

