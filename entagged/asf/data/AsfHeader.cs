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
namespace entagged.audioformats.asf.data {

using Mono.Math;


/**
 * Each asf file starts with a so called header. <br>
 * This header contains other chunks. Each chunk starts with a 16 byte GUID
 * followed by the length (in bytes) of the chunk (including GUID). The length
 * number takes 8 bytes and is unsigned. Finally the chunk's data appears. <br>
 * 
 * @author Christian Laireiter
 */
public class AsfHeader : Chunk {

    /**
     * An asf header contains multiple chunks. <br>
     * The count of those is stored here.
     */
    private long chunkCount;

    /**
     * The content description of the entire file.
     */
    private ContentDescription contentDescription;

    /**
     * Stores the encoding chunk.
     */
    private EncodingChunk encodingChunk;

    /**
     * Stores the tag header.
     */
    private ExtendedContentDescription extendedContentDescription;

    /**
     * Stores the file header.
     */
    private FileHeader fileHeader;

    /**
     * This array stores all found stream chunks.
     */
    private StreamChunk[] streamChunks;

    /**
     * This field stores all chunks which aren't specified and not represented
     * by a wrapper. <br>
     * However during write operations this position and size of those chunks is
     * useful.
     */
    private Chunk[] unspecifiedChunks;

    /**
     * Creates an instance.
     * 
     * @param pos
     *                   see {@link Chunk#position}
     * @param chunkLen
     *                   see {@link Chunk#chunkLength}
     * @param chunkCnt
     */
    public AsfHeader(long pos, ulong chunkLen, long chunkCnt) {
        super(GUID.GUID_HEADER, pos, chunkLen);
        this.chunkCount = chunkCnt;
        this.streamChunks = new StreamChunk[0];
        this.unspecifiedChunks = new Chunk[0];
    }

    /**
     * This method appends a StreamChunk to the header. <br>
     * 
     * @param toAdd
     *                   Chunk to add.
     */
    public void addStreamChunk(StreamChunk toAdd) {
        if (toAdd == null) {
            throw new IllegalArgumentException("Argument must not be null.");
        }
        if (!Arrays.asList(this.streamChunks).contains(toAdd)) {
            StreamChunk[] tmp = new StreamChunk[this.streamChunks.Length + 1];
            System.arraycopy(this.streamChunks, 0, tmp, 0,
                    this.streamChunks.Length);
            tmp[tmp.Length - 1] = toAdd;
            this.streamChunks = tmp;
        }
    }

    /**
     * This method appends the given chunk to the
     * {@linkplain #unspecifiedChunks unspecified}list. <br>
     * 
     * @param toAppend
     *                   The chunk whose use is unknown or of no interest.
     */
    public void addUnspecifiedChunk(Chunk toAppend) {
        if (toAppend == null) {
            throw new IllegalArgumentException("Argument must not be null.");
        }
        if (!Arrays.asList(unspecifiedChunks).contains(toAppend)) {
            Chunk[] tmp = new Chunk[unspecifiedChunks.Length + 1];
            System.arraycopy(unspecifiedChunks, 0, tmp, 0,
                    unspecifiedChunks.Length);
            tmp[tmp.Length - 1] = toAppend;
            unspecifiedChunks = tmp;
        }
    }

    /**
     * This method returns the first audio stream chunk found in the asf file or
     * stream.
     * 
     * @return Returns the audioStreamChunk.
     */
    public AudioStreamChunk getAudioStreamChunk() {
        AudioStreamChunk result = null;
        for (int i = 0; i < getStreamChunkCount() && result == null; i++) {
            StreamChunk tmp = getStreamChunk(i);
            if (tmp is AudioStreamChunk) {
                result = (AudioStreamChunk) tmp;
            }
        }
        return result;
    }

    /**
     * @return Returns the chunkCount.
     */
    public long getChunkCount() {
        return chunkCount;
    }

    /**
     * @return Returns the contentDescription.
     */
    public ContentDescription getContentDescription() {
        return contentDescription;
    }

    /**
     * @return Returns the encodingChunk.
     */
    public EncodingChunk getEncodingChunk() {
        return encodingChunk;
    }

    /**
     * @return Returns the tagHeader.
     */
    public ExtendedContentDescription getExtendedContentDescription() {
        return extendedContentDescription;
    }

    /**
     * @return Returns the fileHeader.
     */
    public FileHeader getFileHeader() {
        return fileHeader;
    }

    /**
     * This method returns the StreamChunk at given index. <br>
     * 
     * @param index
     *                   index of the wanted chunk
     * @return StreamChunk at given index.
     */
    public StreamChunk getStreamChunk(int index) {
        return this.streamChunks[index];
    }

    /**
     * This method returns the amount of StreamChunks in this header. <br>
     * 
     * @return Number of inserted StreamChunks.
     */
    public int getStreamChunkCount() {
        return this.streamChunks.Length;
    }

    /**
     * This method returns the unspecified chunk at given position. <br>
     * 
     * @param index
     *                   Index of the wanted chunk
     * @return The chunk at given index.
     */
    public Chunk getUnspecifiedChunk(int index) {
        return this.unspecifiedChunks[index];
    }

    /**
     * This method returns the number of {@link Chunk}objects which where
     * inserted using {@link #addUnspecifiedChunk(Chunk)}.
     * 
     * @return Number of unspecified chunks.
     */
    public int getUnspecifiedChunkCount() {
        return this.unspecifiedChunks.Length;
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.asf.data.Chunk#prettyPrint()
     */
    public override string prettyPrint() {
        stringBuffer result = new stringBuffer(super.prettyPrint());
        result.insert(0, "\nASF Chunk\n");
        result.append("   Contains: \"" + getChunkCount() + "\" chunks\n");
        result.append(getFileHeader());
        result.append(getExtendedContentDescription());
        result.append(getEncodingChunk());
        result.append(getContentDescription());
        for (int i = 0; i < getStreamChunkCount(); i++) {
            result.append(getStreamChunk(i));
        }
        return result.Tostring();
    }

    /**
     * @param contentDesc
     *                   sets the contentDescription. <code>null</code> deletes the
     *                   chunk.
     */
    public void setContentDescription(ContentDescription contentDesc) {
        this.contentDescription = contentDesc;
    }

    /**
     * @param encChunk
     *                   The encodingChunk to set.
     */
    public void setEncodingChunk(EncodingChunk encChunk) {
        if (encChunk == null)
            throw new IllegalArgumentException("Argument must not be null.");
        this.encodingChunk = encChunk;
    }

    /**
     * @param th
     *                   sets the extendedContentDescription. <code>null</code>
     *                   delete the chunk.
     */
    public void setExtendedContentDescription(ExtendedContentDescription th) {
        this.extendedContentDescription = th;
    }

    /**
     * @param fh
     */
    public void setFileHeader(FileHeader fh) {
        if (fh == null)
            throw new IllegalArgumentException("Argument must not be null.");
        this.fileHeader = fh;
    }
}
}

