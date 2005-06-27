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
using System;
using entagged.audioformats.asf.util;

/**
 * This class stores the information about the file, which is contained within a
 * special chunk of asf files.<br>
 * 
 * @author Christian Laireiter
 */
public class FileHeader : Chunk {

	/**
	 * Duration of the media content in 100ns steps.
	 */
	private  ulong duration;

	/**
	 * The time the file was created.
	 */
	private  DateTime fileCreationTime;

	/**
	 * Size of the file or stream.
	 */
	private ulong fileSize;

	/**
	 * Usually contains value of 2.
	 */
	private  long flags;

	/**
	 * Maximum size of stream packages. <br>
	 * <b>Warning: </b> must be same size as {@link #minPackageSize}. Its not
	 * known how to handle deviating values.
	 */
	private  long maxPackageSize;

	/**
	 * Minimun size of stream packages. <br>
	 * <b>Warning: </b> must be same size as {@link #maxPackageSize}. Its not
	 * known how to handle deviating values.
	 */
	private  long minPackageSize;

	/**
	 * Number of stream packages within the File.
	 */
	private  ulong packageCount;

	/**
	 * No Idea of the Meaning, but stored anyway. <br>
	 * Source documentation says it is: "Timestamp of end position"
	 */
	private  ulong timeEndPos;

	/**
	 * Like {@link #timeEndPos}no Idea.
	 */
	private  ulong timeStartPos;

	/**
	 * Size of an uncompressed video frame.
	 */
	private  long uncompressedFrameSize;

	/**
	 * Creates an instance.
	 * 
	 * @param fileHeaderStart
	 *                  Position in file or stream, where the file header starts.
	 * @param chunckLen
	 *                  Length of the file header (chunk)
	 * @param size
	 *                  Size of file or stream
	 * @param fileTime
	 *                  Time file or stream was created. Time is calculated since 1st
	 *                  january of 1601 in 100ns steps.
	 * @param pkgCount
	 *                  Number of stream packages.
	 * @param dur
	 *                  Duration of media clip in 100ns steps
	 * @param timestampStart
	 *                  Timestamp of start {@link #timeStartPos}
	 * @param timestampEnd
	 *                  Timestamp of end {@link #timeEndPos}
	 * @param headerFlags
	 *                  some stream related flags.
	 * @param minPkgSize
	 *                  minimun size of packages
	 * @param maxPkgSize
	 *                  maximum size of packages
	 * @param uncmpVideoFrameSize
	 *                  Size of an uncompressed Video Frame.
	 */
	public FileHeader(long fileHeaderStart, ulong chunckLen,
			ulong size, ulong fileTime, ulong pkgCount,
			ulong dur, ulong timestampStart, ulong timestampEnd,
			long headerFlags, long minPkgSize, long maxPkgSize,
			long uncmpVideoFrameSize) {
		super(GUID.GUID_FILE, fileHeaderStart, chunckLen);
		this.fileSize = size;
		this.packageCount = pkgCount;
		this.duration = dur;
		this.timeStartPos = timestampStart;
		this.timeEndPos = timestampEnd;
		this.flags = headerFlags;
		this.minPackageSize = minPkgSize;
		this.maxPackageSize = maxPkgSize;
		this.uncompressedFrameSize = uncmpVideoFrameSize;
		this.fileCreationTime = Utils.getDateOf(fileTime).getTime();
	}

	/**
	 * @return Returns the duration.
	 */
	public ulong getDuration() {
		return duration;
	}

	/**
	 * This method converts {@link #getDuration()}from 100ns steps to normal
	 * seconds.
	 * 
	 * @return Duration of the media in seconds.
	 */
	public int getDurationInSeconds() {
		return duration.divide(new ulong("10000000")).intValue();
	}

	/**
	 * @return Returns the fileCreationTime.
	 */
	public DateTime getFileCreationTime() {
		return fileCreationTime;
	}

	/**
	 * @return Returns the fileSize.
	 */
	public ulong getFileSize() {
		return fileSize;
	}

	/**
	 * @return Returns the flags.
	 */
	public long getFlags() {
		return flags;
	}

	/**
	 * @return Returns the maxPackageSize.
	 */
	public long getMaxPackageSize() {
		return maxPackageSize;
	}

	/**
	 * @return Returns the minPackageSize.
	 */
	public long getMinPackageSize() {
		return minPackageSize;
	}

	/**
	 * @return Returns the packageCount.
	 */
	public ulong getPackageCount() {
		return packageCount;
	}

	/**
	 * @return Returns the timeEndPos.
	 */
	public ulong getTimeEndPos() {
		return timeEndPos;
	}

	/**
	 * @return Returns the timeStartPos.
	 */
	public ulong getTimeStartPos() {
		return timeStartPos;
	}

	/**
	 * @return Returns the uncompressedFrameSize.
	 */
	public long getUncompressedFrameSize() {
		return uncompressedFrameSize;
	}

	/**
	 * (overridden)
	 * 
	 * @see entagged.audioformats.asf.data.Chunk#prettyPrint()
	 */
	public override string prettyPrint() {
		stringBuffer result = new stringBuffer(super.prettyPrint());
		result.insert(0, "\nFileHeader\n");
		result.append("   Filesize      = " + getFileSize().Tostring()
				+ " Bytes \n");
		result.append("   Media duration= "
				+ getDuration().divide(new ulong("10000")).Tostring()
				+ " ms \n");
		result.append("   Created at    = " + getFileCreationTime() + "\n");
		return result.Tostring();
	}
}
}
