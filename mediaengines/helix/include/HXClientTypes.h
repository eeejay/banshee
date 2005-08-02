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

#ifndef _HXCLIENT_TYPES_H_
#define _HXCLIENT_TYPES_H_

#ifdef _MAC_MACHO
#elif defined(_SYMBIAN)
#include <e32def.h>
class CDirectScreenAccess;
typedef TUint16 UInt16;
typedef TInt32  SInt32;
typedef TUint32 UInt32;
#else
# ifndef _VXWORKS 
   typedef unsigned short int  UInt16;
#  if (defined _UNIX && defined _LONG_IS_64)
     typedef int               SInt32;
     typedef unsigned int      UInt32;
#  else
     typedef long int          SInt32;
     typedef unsigned long int UInt32;
#  endif
# endif
#endif

typedef void* HXClientPlayerToken;

// Mirrors HXxRect
typedef struct _SHXClientRect
{
    SInt32   left;
    SInt32   top;
    SInt32   right;
    SInt32   bottom;
} SHXClientRect;

#ifdef _MAC_UNIX
enum
{
	// drawingContextKind.
	kDrawingContextKindQuickDrawWindowPtr = 1,
	kDrawingContextKindQuickDrawGrafPtr
};
#endif

// Mirrors HXxWindow
typedef struct _SHXClientWindow
{
    void*      window; // For _MAC_UNIX, point to drawingContextKind.
    UInt32     x;
    UInt32     y;                   
    UInt32     width;
    UInt32     height;
    SHXClientRect    clipRect;
#ifdef _UNIX
    void*      display;
#endif
#ifdef _SYMBIAN
    CDirectScreenAccess* iDSA;
#endif
#ifdef _MAC_UNIX
	unsigned long   drawingContextKind;
	void*			drawingContext; // Depends on drawingContextKind.
#endif
} SHXClientWindow;

// Mirrors HXAudioFormat
typedef struct _SHXAudioFormat
{
	UInt16 uChannels;		/* Num. of Channels (1=Mono, 2=Stereo, etc. */
	UInt16 uBitsPerSample;	/* 8 or 16 */
	UInt32 ulSamplesPerSec; /* Sampling Rate */
	UInt16 uMaxBlockSize;   /* Max Blocksize */
} SHXAudioFormat;

#endif
