/* ***** BEGIN LICENSE BLOCK *****
 * Source last modified: $Id$
 * 
 * Portions Copyright (c) 1995-2004 RealNetworks, Inc. All Rights Reserved.
 * 
 * The contents of this file, and the files included with this file,
 * are subject to the current version of the RealNetworks Public
 * Source License (the "RPSL") available at
 * http://www.helixcommunity.org/content/rpsl unless you have licensed
 * the file under the current version of the RealNetworks Community
 * Source License (the "RCSL") available at
 * http://www.helixcommunity.org/content/rcsl, in which case the RCSL
 * will apply. You may also obtain the license terms directly from
 * RealNetworks.  You may not use this file except in compliance with
 * the RPSL or, if you have a valid RCSL with RealNetworks applicable
 * to this file, the RCSL.  Please see the applicable RPSL or RCSL for
 * the rights, obligations and limitations governing use of the
 * contents of the file.
 * 
 * Alternatively, the contents of this file may be used under the
 * terms of the GNU General Public License Version 2 or later (the
 * "GPL") in which case the provisions of the GPL are applicable
 * instead of those above. If you wish to allow use of your version of
 * this file only under the terms of the GPL, and not to allow others
 * to use your version of this file under the terms of either the RPSL
 * or RCSL, indicate your decision by deleting the provisions above
 * and replace them with the notice and other provisions required by
 * the GPL. If you do not delete the provisions above, a recipient may
 * use your version of this file under the terms of any one of the
 * RPSL, the RCSL or the GPL.
 * 
 * This file is part of the Helix DNA Technology. RealNetworks is the
 * developer of the Original Code and owns the copyrights in the
 * portions it created.
 * 
 * This file, and the files included with this file, is distributed
 * and made available on an 'AS IS' basis, WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESS OR IMPLIED, AND REALNETWORKS HEREBY DISCLAIMS
 * ALL SUCH WARRANTIES, INCLUDING WITHOUT LIMITATION, ANY WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, QUIET
 * ENJOYMENT OR NON-INFRINGEMENT.
 * 
 * Technology Compatibility Kit Test Suite(s) Location:
 *    http://www.helixcommunity.org/content/tck
 * 
 * Contributor(s):
 * 
 * ***** END LICENSE BLOCK ***** */

#ifndef _HXCLIENT_CONSTANTS_H_
#define _HXCLIENT_CONSTANTS_H_

enum // contentState constants
{
	kContentStateNotLoaded = 0,
	kContentStateContacting,
	kContentStateLoading,
	kContentStateStopped,
	kContentStatePlaying,
	kContentStatePaused
};

enum // buffering reasons. Mirrors BUFFERING_REASON in hxcore.h
{
	kBufferReasonStartUp = 0,
	kBufferReasonSeek,
	kBufferReasonCongestion,
	kBufferReasonLivePause
};

enum // EQ bands
{
	kEQBand31Hz = 0,
	kEQBand62Hz = 1,
	kEQBand125Hz = 2,
	kEQBand250Hz = 3,
	kEQBand500Hz = 4,
	kEQBand1KHz = 5,
	kEQBand2KHz = 6,
	kEQBand4KHz = 7,
	kEQBand8KHz = 8,
	kEQBand16KHz = 9,
	kEQBandCount
};

enum // video attributes
{
	kVideoAttrBrightness = 0,
	kVideoAttrContrast = 1,
	kVideoAttrSaturation = 2,
	kVideoAttrHue = 3,
	kVideoAttrSharpness = 4
};

enum // statistics value types
{
	kValueTypeInternalUse = 0,
	kValueType32BitSignedInt = 2,
	kValueTypeString = 4
};

enum // audio hook stream type. Mirrors AudioStreamType in hxausvc.h
{
	kAudioStreamTypeStreaming = 0,
	kAudioStreamTypeInstantaneous = 1,
	kAudioStreamTypeTimed = 2,
	kAudioStreamTypeStreamingInstantaneous = 3
};

#define kSMILWayInTheFutureConstant		1981342000
#define kSMILWayInTheFutureFudgeFactor		  1000 // From EHodge: Should be handled in datatype/smil/renderer/smil2/smldoc.cpp.

#endif
