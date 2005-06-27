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
 * Reads and interprets the data of the file header. <br>
 * 
 * @author Christian Laireiter
 */
public class FileHeaderReader {

	/**
	 * Creates and fills a {@link FileHeader}from given file. <br>
	 * 
	 * @param raf
	 *                  Input
	 * @param candidate
	 *                  Chunk which possibly is a file header.
	 * @return FileHeader if filepointer of <code>raf</code> is at valid
	 *              fileheader.
	 * @throws IOException
	 *                   Read errors.
	 */
	public static FileHeader read(FileStream raf, Chunk candidate)
			{
		if (raf == null || candidate == null) {
			throw new IllegalArgumentException("Arguments must not be null.");
		}
		if (GUID.GUID_FILE.equals(candidate.getGuid())) {
			raf.Seek(candidate.getPosition(), SeekOrigin.Begin);
			return new FileHeaderReader().parseData(raf);
		}
		return null;
	}

	/**
	 * Should not be used for now.
	 *  
	 */
	protected FileHeaderReader() {
		// NOTHING toDo
	}

	/**
	 * Tries to extract an ASF file header object out of the given input.
	 * 
	 * @param raf
	 * @return <code>null</code> if no valid file header object.
	 * @throws IOException
	 */
	private FileHeader parseData(FileStream raf) {
		FileHeader result = null;
		long fileHeaderStart = raf.Position;
		GUID guid = Utils.readGUID(raf);
		if (GUID.GUID_FILE.equals(guid)) {
			ulong chunckLen = Utils.readBig64(raf);
			// Skip client GUID.
			raf.skipBytes(16);

			ulong fileSize = Utils.readBig64(raf);
			if (fileSize.intValue() != raf.Length()) {
				System.err
						.println("Filesize of file doesn't match len of Fileheader. ("
								+ fileSize.Tostring() + ", file: "+raf.Length()+")");
			}
			// fileTime in 100 ns since midnight of 1st january 1601 GMT
			ulong fileTime = Utils.readBig64(raf);

			ulong packageCount = Utils.readBig64(raf);

			ulong timeEndPos = Utils.readBig64(raf);
			ulong duration = Utils.readBig64(raf);
			ulong timeStartPos = Utils.readBig64(raf);

			long flags = Utils.readUINT32(raf);

			long minPkgSize = Utils.readUINT32(raf);
			long maxPkgSize = Utils.readUINT32(raf);
			long uncompressedFrameSize = Utils.readUINT32(raf);

			result = new FileHeader(fileHeaderStart, chunckLen, fileSize,
					fileTime, packageCount, duration, timeStartPos, timeEndPos,
					flags, minPkgSize, maxPkgSize, uncompressedFrameSize);
		}
		return result;
	}

}
}

