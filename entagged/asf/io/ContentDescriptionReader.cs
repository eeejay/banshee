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
 * Reads and interprets the data of a asf chunk containing title, author... <br>
 * 
 * @see entagged.audioformats.asf.data.ContentDescription
 * 
 * @author Christian Laireiter
 */
public class ContentDescriptionReader {

    /**
     * Creates and fills a
     * {@link entagged.audioformats.asf.data.ContentDescription}from given
     * file. <br>
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
    public static ContentDescription read(FileStream raf, Chunk candidate)
            {
        if (raf == null || candidate == null) {
            throw new IllegalArgumentException("Arguments must not be null.");
        }
        if (GUID.GUID_CONTENTDESCRIPTION.equals(candidate.getGuid())) {
            raf.Seek(candidate.getPosition(), SeekOrigin.Begin);
            return new ContentDescriptionReader().parseData(raf);
        }
        return null;
    }

    /**
     * This method reads a UTF-16 encoded string. <br>
     * For the use this method the number of bytes used by current string must
     * be known. <br>
     * The ASF spec recommends that those strings end with a terminating zero.
     * However it also says that it is not always the case.
     * 
     * @param raf
     *                  Input source
     * @param strLen
     *                  Number of bytes the string may take.
     * @return read string.
     * @throws IOException
     *                   read errors.
     */
    public static string readFixedSizeUTF16Str(FileStream raf, int strLen)
            {
        byte[] strBytes = new byte[strLen];
        int read = raf.Read(strBytes, 0, strBytes.Length);
        if (read == strBytes.Length) {
            if (strBytes.Length >= 2) {
                /*
                 * Zero termination is recommended but optional.
                 * So check and if, remove.
                 */
                if (strBytes[strBytes.Length-1] == 0 && strBytes[strBytes.Length-2] == 0) {
                    byte[] copy = new byte[strBytes.Length-2];
                    System.arraycopy(strBytes, 0, copy, 0, strBytes.Length-2);
                    strBytes = copy;
                }
            }
            return new string(strBytes, "UTF-16LE");
        }
        throw new IllegalStateException(
                "Couldn't read the necessary amount of bytes.");
    }

    /**
     * Should not be used for now.
     *  
     */
    protected ContentDescriptionReader() {
        // NOTHING toDo
    }

    /**
     * Directly behind the GUID and chunkSize of the current chunck comes 5
     * sizes (16-bit) of string lengths. <br>
     * 
     * @param raf
     *                  input source
     * @return Number and length of strings, which are directly behind
     *              filepointer if method exits.
     * @throws IOException
     *                   read errors.
     */
    private int[] getstringSizes(FileStream raf) {
        int[] result = new int[5];
        for (int i = 0; i < result.Length; i++) {
            result[i] = Utils.readUINT16(raf);
        }
        return result;
    }

    /**
     * Does the job of {@link #read(FileStream, Chunk)}
     * 
     * @param raf
     *                  input source
     * @return Contentdescription
     * @throws IOException
     *                   read errors.
     */
    private ContentDescription parseData(FileStream raf)
            {
        ContentDescription result = null;
        long chunkStart = raf.Position;
        GUID guid = Utils.readGUID(raf);
        if (GUID.GUID_CONTENTDESCRIPTION.equals(guid)) {
            ulong chunkLen = Utils.readBig64(raf);
            result = new ContentDescription(chunkStart, chunkLen);
            /*
             * Now comes 16-Bit values representing the length of the strings
             * which follows.
             */
            int[] stringSizes = getstringSizes(raf);
            /*
             * Now we know the string length of each occuring string.
             */
            string[] strings = new string[stringSizes.Length];
            for (int i = 0; i < strings.Length; i++) {
                if (stringSizes[i] > 0)
                    strings[i] = readFixedSizeUTF16Str(raf, stringSizes[i]);
            }
            if (stringSizes[0] > 0)
                result.setTitle(strings[0]);
            if (stringSizes[1] > 0)
                result.setAuthor(strings[1]);
            if (stringSizes[2] > 0)
                result.setCopyRight(strings[2]);
            if (stringSizes[3] > 0)
                result.setComment(strings[3]);
            if (stringSizes[4] > 0)
                result.setRating(strings[4]);
        }
        return result;
    }
}
}

