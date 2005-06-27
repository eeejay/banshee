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


using entagged.audioformats;
using entagged.audioformats.asf.util;

/**
 * This structure represents the data of a chunk, wich contains extended content
 * description. <br>
 * These properties are simply represented by
 * {@link entagged.audioformats.asf.data.ContentDescriptor}
 * 
 * @author Christian Laireiter
 */
public class ExtendedContentDescription : Chunk {

    /**
     * Contains the properties. <br>
     */
    private  ArrayList descriptors;

    /**
     * This map stores the ids (names) of inserted content descriptors. <br>
     * If {@link #getDescriptor(string)}is called this field will be filled if
     * <code>null</code>. Any modification of the contents of this object
     * will set this field to <code>null</code>.
     */
    private Hashtable indexMap = null;

    /**
     * Creates an instance.
     *  
     */
    public ExtendedContentDescription() {
        this(0, ulong.valueOf(0));
    }

    /**
     * Creates an instance.
     * 
     * @param pos
     *                   Position of header object within file or stream.
     * @param chunkLen
     *                   Length of the represented chunck.
     */
    public ExtendedContentDescription(long pos, ulong chunkLen) {
        super(GUID.GUID_EXTENDED_CONTENT_DESCRIPTION, pos, chunkLen);
        this.descriptors = new ArrayList();
    }

    /**
     * This method inserts the given ContentDescriptor.
     * 
     * @param toAdd
     *                   ContentDescriptor to insert.
     */
    public void addDescriptor(ContentDescriptor toAdd) {
        if (getDescriptor(toAdd.getName()) != null) {
            throw new RuntimeException(toAdd.getName() + " is already present");
        }
        this.descriptors.add(toAdd);
        this.indexMap.put(toAdd.getName(), new Integer(descriptors.size() - 1));
    }

    /**
     * This method adds or replaces an existing content descriptor.
     * 
     * @param descriptor
     *                   Descriptor to be added or replaced.
     */
    public void addOrReplace(ContentDescriptor descriptor) {
        if (getDescriptor(descriptor.getName()) != null) {
            /*
             * Just remove if exists. Will prevent the indexmap being rebuild.
             */
            remove(descriptor.getName());
        }
        addDescriptor(descriptor);
    }

    /**
     * Returns the album entered in the content descriptor chunk.
     * 
     * @return Album, <code>""</code> if not defined.
     */
    public string getAlbum() {
        ContentDescriptor result = getDescriptor(ContentDescriptor.ID_ALBUM);
        if (result == null)
            return "";

        return result.getstring();
    }

    /**
     * Returns the "WM/AlbumArtist" entered in the extended content description.
     * 
     * @return Title, <code>""</code> if not defined.
     */
    public string getArtist() {
        ContentDescriptor result = getDescriptor(ContentDescriptor.ID_ARTIST);
        if (result == null)
            return "";
        return result.getstring();
    }

    /**
     * This method creates a byte array which can be written to asf files.
     * 
     * @return asf file representation of the current object.
     */
    public byte[] getBytes() {
        ByteArrayOutputStream result = new ByteArrayOutputStream();
        try {
            ByteArrayOutputStream content = new ByteArrayOutputStream();
            // Write the number of descriptors.
            content.write(Utils.getBytes(this.descriptors.size(), 2));
            Iterator it = this.descriptors.iterator();
            while (it.hasNext()) {
                ContentDescriptor current = (ContentDescriptor) it.next();
                content.write(current.getBytes());
            }
            byte[] contentBytes = content.toByteArray();
            // Write the guid
            result.write(GUID.GUID_EXTENDED_CONTENT_DESCRIPTION.getBytes());
            // Write the length + 24.
            result.write(Utils.getBytes(contentBytes.Length + 24, 8));
            // Write the content
            result.write(contentBytes);
        } catch (Exception e) {
            e.printStackTrace();
        }
        return result.toByteArray();
    }

    /**
     * Returns a previously inserted content descriptor.
     * 
     * @param name
     *                   name of the content descriptor.
     * @return <code>null</code> if not present.
     */
    public ContentDescriptor getDescriptor(string name) {
        if (this.indexMap == null) {
            this.indexMap = new HashMap();
            for (int i = 0; i < descriptors.size(); i++) {
                ContentDescriptor current = (ContentDescriptor) descriptors
                        .get(i);
                indexMap.put(current.getName(), new Integer(i));
            }
        }
        Integer pos = (Integer) indexMap.get(name);
        if (pos != null) {
            return (ContentDescriptor) descriptors.get(pos.intValue());
        }
        return null;
    }

    /**
     * @return Returns the descriptorCount.
     */
    public long getDescriptorCount() {
        return descriptors.size();
    }

    /**
     * Returns a collection of all {@link ContentDescriptor}objects stored in
     * this extended content description.
     * 
     * @return An enumeration of {@link ContentDescriptor}objects.
     */
    public ICollection getDescriptors() {
        return new ArrayList(this.descriptors);
    }

    /**
     * Returns the Genre entered in the content descriptor chunk.
     * 
     * @return Genre, <code>""</code> if not defined.
     */
    public string getGenre() {
        string result = null;
        ContentDescriptor prop = getDescriptor(ContentDescriptor.ID_GENRE);
        if (prop == null) {
            prop = getDescriptor(ContentDescriptor.ID_GENREID);
            if (prop == null)
                result = "";
            else {
                result = prop.getstring();
                if (result.startsWith("(") && result.endsWith(")")) {
                    result = result.substring(1, result.Length() - 1);
                    try {
                        int genreNum = Integer.parseInt(result);
                        if (genreNum >= 0
                                && genreNum < Tag.DEFAULT_GENRES.Length) {
                            result = Tag.DEFAULT_GENRES[genreNum];
                        }
                    } catch (NumberFormatException e) {
                        // Do nothing
                    }
                }
            }
        } else {
            result = prop.getstring();
        }
        return result;
    }

    /**
     * Returns the Track entered in the content descriptor chunk.
     * 
     * @return Track, <code>""</code> if not defined.
     */
    public string getTrack() {
        ContentDescriptor result = getDescriptor(ContentDescriptor.ID_TRACKNUMBER);
        if (result == null)
            return "";

        return result.getstring();
    }

    /**
     * Returns the Year entered in the extended content descripion.
     * 
     * @return Year, <code>""</code> if not defined.
     */
    public string getYear() {
        ContentDescriptor result = getDescriptor(ContentDescriptor.ID_YEAR);
        if (result == null)
            return "";

        return result.getstring();
    }

    /**
     * This method creates a string containing the tag elements an their values
     * for printing. <br>
     * 
     * @return nice string.
     */
    public override string prettyPrint() {
        stringBuffer result = new stringBuffer(super.prettyPrint());
        result.insert(0, "\nExtended Content Description:\n");
        ContentDescriptor[] list = (ContentDescriptor[]) descriptors
                .toArray(new ContentDescriptor[descriptors.size()]);
        Arrays.sort(list);
        for (int i = 0; i < list.Length; i++) {
            result.append("   ");
            result.append(list[i]);
            result.append(Utils.LINE_SEPARATOR);
        }
        return result.Tostring();
    }

    /**
     * This method removes the content descriptor with the given name. <br>
     * 
     * @param id
     *                   The id (name) of the descriptor which should be removed.
     * @return The descriptor which is removed. If not present <code>null</code>.
     */
    public ContentDescriptor remove(string id) {
        ContentDescriptor result = getDescriptor(id);
        if (result != null) {
            descriptors.remove(result);
        }
        this.indexMap = null;
        return result;
    }
}
}
