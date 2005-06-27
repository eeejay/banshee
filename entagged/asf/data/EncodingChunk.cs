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



using System.Collections;


using entagged.audioformats.asf.util;

/**
 * This class was intended to store the data of a chunk which contained the
 * encoding parameters in textual form. <br>
 * Since the needed parameters were found in other chunks the implementation of
 * this class was paused. <br>
 * TODO complete analysis. 
 * 
 * @author Christian Laireiter
 */
public class EncodingChunk : Chunk {

	/**
	 * The read strings.
	 */
	private  ArrayList strings;

	/**
	 * Creates an instance.
	 * 
	 * @param pos
	 *                  Position of the chunk within file or stream
	 * @param chunkLen
	 *                  Length of current chunk.
	 */
	public EncodingChunk(long pos, ulong chunkLen) {
		super(GUID.GUID_ENCODING, pos, chunkLen);
		this.strings = new ArrayList();
	}

	/**
	 * This method appends a string.
	 * 
	 * @param toAdd
	 *                  string to add.
	 */
	public void addstring(string toAdd) {
		strings.add(toAdd);
	}

	/**
	 * This method returns a collection of all {@link string}s which were addid
	 * due {@link #addstring(string)}.
	 * 
	 * @return Inserted strings.
	 */
	public ICollection getstrings() {
		return new ArrayList(strings);
	}

	/**
	 * (overridden)
	 * 
	 * @see entagged.audioformats.asf.data.Chunk#prettyPrint()
	 */
	public override string prettyPrint() {
		stringBuffer result = new stringBuffer(super.prettyPrint());
		result.insert(0, Utils.LINE_SEPARATOR + "Encoding:"
				+ Utils.LINE_SEPARATOR);
		Iterator iterator = this.strings.iterator();
		while (iterator.hasNext()) {
			result.append("   " + iterator.next() + Utils.LINE_SEPARATOR);
		}
		return result.Tostring();
	}
}
}
