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

using Entagged.Audioformats.exceptions;
using System.Text;

/**
 * This class represents a chunk within asf streams. <br>
 * Each chunk starts with a 16byte guid identifying the type. After that a
 * number (represented by 8 bytes) follows which shows the size in bytes of the
 * chunk. Finally there is the data of the chunk.
 * 
 * @author Christian Laireiter
 */
public class Chunk {

    /**
     * The length of current chunk. <br>
     */
    protected  ulong chunkLength;

    /**
     * The guid of represented chunk header.
     */
    protected  GUID guid;

    /**
     * The position of current header object within file or stream.
     */
    protected  long position;

    /**
     * Creates an instance
     * 
     * @param headerGuid
     *                  The GUID of header object.
     * @param pos
     *                  Position of header object within stream or file.
     * @param chunkLen
     *                  Length of current chunk.
     */
    public Chunk(GUID headerGuid, long pos, ulong chunkLen) {
        if (headerGuid == null) {
            throw new CannotReadException(
                    "GUID must not be null nor anything else than "
                            + GUID.GUID_LENGTH + " entries long.");
        }
        if (pos < 0) {
            throw new CannotReadException(
                    "Position of header can't be negative.");
        }
        
        this.guid = headerGuid;
        this.position = pos;
        this.chunkLength = chunkLen;
    }

    /**
     * This method returns the End of the current chunk introduced by current
     * header object.
     * 
     * @return Position after current chunk.
     */
    public long getChunckEnd() {
        return position + (long) chunkLength;
    }

    /**
     * @return Returns the chunkLength.
     */
    public ulong getChunkLength() {
        return chunkLength;
    }

    /**
     * @return Returns the guid.
     */
    public GUID getGuid() {
        return guid;
    }

    /**
     * @return Returns the position.
     */
    public long getPosition() {
        return position;
    }

    /**
     * This method creates a string containing usefull information prepared to
     * be printed on stdout. <br>
     * This method is intended to be overwritten by inheriting classes.
     * 
     * @return Information of current Chunk Object.
     */
    public virtual string prettyPrint() {
        StringBuilder result = new StringBuilder();
        result.Append("GUID: " + GUID.getGuidDescription(guid));
        result.Append("\n   Starts at position: " + getPosition() + "\n");
        result.Append("   Last byte at: " + (getChunckEnd() - 1) + "\n\n");
        return result.ToString();
    }

    /**
     * (overridden)
     * 
     * @see java.lang.Object#Tostring()
     */
    public override string ToString() {
        return prettyPrint();
    }

}
}
