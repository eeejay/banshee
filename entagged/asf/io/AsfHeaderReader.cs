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
 * This <i>class </i> reads an Asf header out of an inputstream an creates an
 * {@link entagged.audioformats.asf.data.AsfHeader}object if successfull. <br>
 * For now only ASF ver 1.0 is supported, till ver 2.0 seems not to be used
 * anywhere. <br>
 * Asf headers contains other chunks. As of this other readers of current
 * <b>package </b> are called from within.
 * 
 * @author Christian Laireiter
 */
public class AsfHeaderReader {

    /**
     * This method tries to extract an ASF-header out of the given stream. <br>
     * If no header could be extracted <code>null</code> is returned. <br>
     * 
     * @param in
     *                   File which contains the ASF header.
     * @return AsfHeader-Wrapper, or <code>null</code> if no supported Asf
     *               header was found.
     * @throws IOException
     *                    Read errors
     */
    public static AsfHeader readHeader(FileStream fs) {
        AsfHeaderReader reader = new AsfHeaderReader();
        return reader.parseData(fs);
    }

    /**
     * Protected default constructor. <br>
     * At the time no special use.
     *  
     */
    protected AsfHeaderReader() {
        // Nothing to do
    }

    /**
     * This Method : the reading of the header block. <br>
     * 
     * @param in
     *                   Stream which contains an Asf header.
     * @return <code>null</code> if no valid data found, else a Wrapper
     *               containing all supported data.
     * @throws IOException
     *                    Read errors.
     */
    private AsfHeader parseData(FileStream fs) {
        AsfHeader result = null;
        long chunkStart = fs.Position;
        GUID possibleGuid = Utils.readGUID(fs);

        if (GUID.GUID_HEADER.equals(possibleGuid)) {
            // For Know the filepointer pointed to an ASF header chunk.
            ulong chunkLen = Utils.readBig64(fs);

            long chunkCount = Utils.readUINT32(fs);
            // They are of unknown use.
            fs.skipBytes(2);

            /*
             * Now reading header of chuncks.
             */
            ArrayList chunks = new ArrayList();
            while (chunkLen.compareTo(ulong.valueOf(fs.Position)) > 0) {
                Chunk chunk = ChunkHeaderReader.readChunckHeader(fs);
                chunks.add(chunk);
                fs.seek(chunk.getChunckEnd());
            }

            /*
             * Creating the resulting object because streamchunks will be added.
             */
            result = new AsfHeader(chunkStart, chunkLen, chunkCount);
            /*
             * Now we know all positions and guids of chunks which are contained
             * whithin asf header. Further we need to identify the type of those
             * chunks and parse the interesting ones.
             */
            FileHeader fileHeader = null;
            ExtendedContentDescription extendedDescription = null;
            EncodingChunk encodingChunk = null;
            StreamChunk streamChunk = null;
            ContentDescription contentDescription = null;

            Iterator iterator = chunks.iterator();
            while (iterator.hasNext()) {
                Chunk currentChunk = (Chunk) iterator.next();
                if (fileHeader == null
                        && (fileHeader = FileHeaderReader
                                .read(fs, currentChunk)) != null) {
                    continue;
                }
                if (extendedDescription == null
                        && (extendedDescription = ExtContentDescReader.read(fs,
                                currentChunk)) != null) {
                    continue;
                }
                if (encodingChunk == null
                        && (encodingChunk = EncodingChunkReader.read(fs,
                                currentChunk)) != null) {
                    continue;
                }
                if (streamChunk == null
                        && (streamChunk = StreamChunkReader.read(fs,
                                currentChunk)) != null) {
                    result.addStreamChunk(streamChunk);
                    streamChunk = null;
                    continue;
                }
                if (contentDescription == null
                        && (contentDescription = ContentDescriptionReader.read(
                                fs, currentChunk)) != null) {
                    continue;
                }
                /*
                 * If none of the above statements executed the "continue", this
                 * chunk couldn't be interpreted. Despite this the chunk is
                 * remembered
                 */
                result.addUnspecifiedChunk(currentChunk);
            }
            /*
             * Finally store the parsed chunks in the resulting ASFHeader
             * object.
             */
            result.setFileHeader(fileHeader);
            result.setEncodingChunk(encodingChunk);
            /*
             * Warning, extendedDescription and contentDescription maybe null
             * since they are optional fields.
             */
            result.setExtendedContentDescription(extendedDescription);
            result.setContentDescription(contentDescription);
        }
        return result;
    }

}
}

