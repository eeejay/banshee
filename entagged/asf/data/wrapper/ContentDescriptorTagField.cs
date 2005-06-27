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
namespace entagged.audioformats.asf.data.wrapper {



using entagged.audioformats.asf.data;
using Entagged.Audioformats.Generic;

/**
 * This class encapsulates a
 * {@link entagged.audioformats.asf.data.ContentDescriptor}and provides access
 * to it. <br>
 * The content descriptor used for construction is copied.
 * 
 * @author Christian Laireiter (liree)
 */
public class ContentDescriptorTagField : TagField {

    /**
     * This descriptor is wrapped.
     */
    private ContentDescriptor toWrap;

    /**
     * Creates an instance.
     * 
     * @param source
     *                   The descriptor which should be represented as a
     *                   {@link TagField}.
     */
    public ContentDescriptorTagField(ContentDescriptor source) {
        this.toWrap = source.createCopy();
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#copyContent(entagged.audioformats.generic.TagField)
     */
    public void copyContent(TagField field) {
        throw new UnsupportedOperationException("Not implemented yet.");
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#getId()
     */
    public string getId() {
        return toWrap.getName();
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#getRawContent()
     */
    public byte[] getRawContent() {
        return toWrap.getRawData();
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#isBinary()
     */
    public bool isBinary() {
        return toWrap.getType() == ContentDescriptor.TYPE_BINARY;
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#isBinary(bool)
     */
    public void isBinary(bool b) {
        if (!b && isBinary()) {
            throw new UnsupportedOperationException("No conversion supported.");
        }
        toWrap.setBinaryValue(toWrap.getRawData());
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#isCommon()
     */
    public bool isCommon() {
        return toWrap.isCommon();
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#isEmpty()
     */
    public bool isEmpty() {
        return toWrap.isEmpty();
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.generic.TagField#Tostring()
     */
    public string Tostring() {
        return toWrap.getstring();
    }

}
}

