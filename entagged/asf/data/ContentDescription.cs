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




using entagged.audioformats.asf.util;

/**
 * This class represents the data of a chunk which contains title, author,
 * copyright, description and the rating of the file. <br>
 * It is optional whithin asf files. But if exists only once.
 * 
 * @author Christian Laireiter
 */
public class ContentDescription : Chunk {

    /**
     * File artist.
     */
    private string author = null;

    /**
     * File copyright.
     */
    private string copyRight = null;

    /**
     * File comment.
     */
    private string description = null;

    /**
     * File rating.
     */
    private string rating = null;

    /**
     * File title.
     */
    private string title = null;

    /**
     * Creates an instance. <br>
     */
    public ContentDescription() {
        this(0, ulong.valueOf(0));
    }

    /**
     * Creates an instance.
     * 
     * @param pos
     *                   Position of content description within file or stream
     * @param chunkLen
     *                   Length of content description.
     */
    public ContentDescription(long pos, ulong chunkLen) {
        super(GUID.GUID_CONTENTDESCRIPTION, pos, chunkLen);
    }

    /**
     * @return Returns the author.
     */
    public string getAuthor() {
        if (author == null)
            return "";
        return author;
    }

    /**
     * This method creates a byte array that could directly be written to an asf
     * file. <br>
     * 
     * @return The asf chunk representation of a content description with the
     *               values of the current object.
     */
    public byte[] getBytes() {
        ByteArrayOutputStream result = new ByteArrayOutputStream();
        try {
            ByteArrayOutputStream tags = new ByteArrayOutputStream();
            string[] toWrite = new string[] { getTitle(), getAuthor(),
                    getCopyRight(), getComment(), getRating() };
            byte[][] stringRepresentations = new byte[toWrite.Length][];
            // Create byte[] of UTF-16LE encodings
            for (int i = 0; i < toWrite.Length; i++) {
                stringRepresentations[i] = toWrite[i].getBytes("UTF-16LE");
            }
            // Write the amount of bytes needed to store the values.
            for (int i = 0; i < stringRepresentations.Length; i++) {
                tags.write(Utils.getBytes(stringRepresentations[i].Length + 2,
                        2));
            }
            // Write the values themselves.
            for (int i = 0; i < toWrite.Length; i++) {
                tags.write(stringRepresentations[i]);
                // Zero term character.
                tags.write(Utils.getBytes(0, 2));
            }
            // Now tags has got the values. The result just needs
            // The GUID, length of the chunk and the tags.
            byte[] tagContent = tags.toByteArray();
            // The guid of the chunk
            result.write(GUID.GUID_CONTENTDESCRIPTION.getBytes());
            /*
             * The length of the chunk. 16 Bytes guid 8 Bytes the length
             * tagContent.Length bytes.
             */
            result.write(Utils.getBytes(tagContent.Length + 24, 8));
            // The tags.
            result.write(tagContent);
        } catch (Exception e) {
            e.printStackTrace();
        }
        return result.toByteArray();
    }

    /**
     * @return Returns the comment.
     */
    public string getComment() {
        if (description == null)
            return "";
        return description;
    }

    /**
     * @return Returns the copyRight.
     */
    public string getCopyRight() {
        if (copyRight == null)
            return "";
        return copyRight;
    }

    /**
     * @return returns the rating.
     */
    public string getRating() {
        if (rating == null)
            return "";
        return rating;
    }

    /**
     * @return Returns the title.
     */
    public string getTitle() {
        if (title == null)
            return "";
        return title;
    }

    /**
     * (overridden)
     * 
     * @see entagged.audioformats.asf.data.Chunk#prettyPrint()
     */
    public override string prettyPrint() {
        stringBuffer result = new stringBuffer(super.prettyPrint());
        result.insert(0, Utils.LINE_SEPARATOR + "Content Description:"
                + Utils.LINE_SEPARATOR);
        result.append("   Title      : " + getTitle() + Utils.LINE_SEPARATOR);
        result.append("   Author     : " + getAuthor() + Utils.LINE_SEPARATOR);
        result.append("   Copyright  : " + getCopyRight()
                + Utils.LINE_SEPARATOR);
        result.append("   Description: " + getComment() + Utils.LINE_SEPARATOR);
        result.append("   Rating     :" + getRating() + Utils.LINE_SEPARATOR);
        return result.Tostring();
    }

    /**
     * @param fileAuthor
     *                   The author to set.
     * @throws IllegalArgumentException
     *                    If "UTF-16LE"-byte-representation would take more than 65535
     *                    bytes.
     */
    public void setAuthor(string fileAuthor) {
        Utils.checkstringLengthNullSafe(fileAuthor);
        this.author = fileAuthor;
    }

    /**
     * @param tagComment
     *                   The comment to set.
     * @throws IllegalArgumentException
     *                    If "UTF-16LE"-byte-representation would take more than 65535
     *                    bytes.
     */
    public void setComment(string tagComment) {
        Utils.checkstringLengthNullSafe(tagComment);
        this.description = tagComment;
    }

    /**
     * @param cpright
     *                   The copyRight to set.
     * @throws IllegalArgumentException
     *                    If "UTF-16LE"-byte-representation would take more than 65535
     *                    bytes.
     */
    public void setCopyRight(string cpright) {
        Utils.checkstringLengthNullSafe(cpright);
        this.copyRight = cpright;
    }

    /**
     * @param ratingText
     *                   The rating to be set.
     * @throws IllegalArgumentException
     *                    If "UTF-16LE"-byte-representation would take more than 65535
     *                    bytes.
     */
    public void setRating(string ratingText) {
        Utils.checkstringLengthNullSafe(ratingText);
        this.rating = ratingText;
    }

    /**
     * @param songTitle
     *                   The title to set.
     * @throws IllegalArgumentException
     *                    If "UTF-16LE"-byte-representation would take more than 65535
     *                    bytes.
     */
    public void setTitle(string songTitle) {
        Utils.checkstringLengthNullSafe(songTitle);
        this.title = songTitle;
    }
}
}

