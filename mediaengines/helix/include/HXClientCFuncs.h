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

/* Standard C functions for accessing the Helix Client Core. */

#ifndef _HXCLIENT_CFUNCS_H_
#define _HXCLIENT_CFUNCS_H_

#ifdef __cplusplus
extern "C" {
#endif

#include "HXClientTypes.h"
#include "HXClientCallbacks.h"

#ifdef _MAC_MACHO

CFStringRef ClientCreateErrorString( UInt32 hxCode, const char* pErrorString );
bool ClientEngineHandleClassicEvent( EventRecord* classicEvent );

#elif defined(_UNIX)

#include <X11/Xlib.h>
    
bool ClientEngineProcessXEvent( XEvent* pXEvent );

#endif

void ClientEngineSetCallbacks( const HXClientEngineCallbacks* pClientEngineCallbacks );

bool ClientPlayerCreate( HXClientPlayerToken* pClientPlayerToken, SHXClientWindow* pWindow, void* userInfo, const HXClientCallbacks* pClientCallbacks );
void ClientPlayerClose( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerOpenURL( HXClientPlayerToken clientPlayerToken, const char* pURL, const char* pMimeType );
bool ClientPlayerOpenData( HXClientPlayerToken clientPlayerToken, const char* pURL, const char* pMimeType, UInt32 dataLength, bool autoPlay, void** ppOutData );
bool ClientPlayerWriteData( HXClientPlayerToken clientPlayerToken, void* pData, UInt32 bufferLength, unsigned char* pBuffer );
void ClientPlayerCloseData( HXClientPlayerToken clientPlayerToken, void* pData );
bool ClientPlayerGetOpenedURL( HXClientPlayerToken clientPlayerToken, char* pURLBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
bool ClientPlayerCanViewSource( HXClientPlayerToken clientPlayerToken );
void ClientPlayerViewSource( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerCanViewRights( HXClientPlayerToken clientPlayerToken );
void ClientPlayerViewRights( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerAuthenticate( HXClientPlayerToken clientPlayerToken, bool shouldValidateUser, const char* pUsername, const char* pPassword );
int ClientPlayerGetContentState( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerSetStatus( HXClientPlayerToken clientPlayerToken, const char* pStatus );
void ClientPlayerPlay( HXClientPlayerToken clientPlayerToken );
void ClientPlayerPause( HXClientPlayerToken clientPlayerToken );
void ClientPlayerStop( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerGetBufferedRange( HXClientPlayerToken clientPlayerToken, UInt32* pMinPosition, UInt32* pWritePosition, UInt32* pMaxPosition );
bool ClientPlayerGetSeekRange( HXClientPlayerToken clientPlayerToken, UInt32* pMinPosition, UInt32* pMaxPosition );
bool ClientPlayerStartSeeking( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerSetPosition( HXClientPlayerToken clientPlayerToken, UInt32 position );
void ClientPlayerStopSeeking( HXClientPlayerToken clientPlayerToken );
UInt32 ClientPlayerGetPosition( HXClientPlayerToken clientPlayerToken );
UInt32 ClientPlayerGetLength( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerIsLive( HXClientPlayerToken clientPlayerToken );
const char* ClientPlayerGetTitle( HXClientPlayerToken clientPlayerToken );
const char* ClientPlayerGetContextURL( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerHasVisualContent( HXClientPlayerToken clientPlayerToken );
void ClientPlayerGetIdealSize( HXClientPlayerToken clientPlayerToken, SInt32* pSiteIdealWidth, SInt32* pSiteIdealHeight );
SInt32 ClientPlayerGetClipBandwidth( HXClientPlayerToken clientPlayerToken );
void ClientPlayerSetSize( HXClientPlayerToken clientPlayerToken, SInt32 siteWidth, SInt32 siteHeight );
UInt16 ClientPlayerGetSourceCount( HXClientPlayerToken clientPlayerToken );
UInt16 ClientPlayerGetGroupCount( HXClientPlayerToken clientPlayerToken );
UInt16 ClientPlayerGetCurrentGroup( HXClientPlayerToken clientPlayerToken );

// If p...Buffer is NULL and/or bufferLength is 0, returns the size in pUsedBufferLength, assuming it isn't NULL.
bool ClientPlayerGetGroupURL( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex, char* pURLBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
bool ClientPlayerGetGroupTitle( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex, char* pTitleBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
bool ClientPlayerSetCurrentGroup( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex );
void ClientPlayerDrawSite( HXClientPlayerToken clientPlayerToken, const SHXClientRect* pSiteRect );
void ClientPlayerSetVolume( HXClientPlayerToken clientPlayerToken, UInt16 volume );
UInt16 ClientPlayerGetVolume( HXClientPlayerToken clientPlayerToken );
void ClientPlayerMute( HXClientPlayerToken clientPlayerToken, bool shouldMute );
bool ClientPlayerIsMuted( HXClientPlayerToken clientPlayerToken );
void ClientPlayerEnableEQ( HXClientPlayerToken clientPlayerToken, bool enable );
bool ClientPlayerIsEQEnabled( HXClientPlayerToken clientPlayerToken );
void ClientPlayerSetEQGain( HXClientPlayerToken clientPlayerToken, int band, SInt32 gain );
SInt32 ClientPlayerGetEQGain( HXClientPlayerToken clientPlayerToken, int band );
void ClientPlayerSetEQPreGain( HXClientPlayerToken clientPlayerToken, SInt32 preGain );
SInt32 ClientPlayerGetEQPreGain( HXClientPlayerToken clientPlayerToken );
void ClientPlayerEnableEQAutoPreGain( HXClientPlayerToken clientPlayerToken, bool enable );
bool ClientPlayerIsEQAutoPreGainEnabled( HXClientPlayerToken clientPlayerToken );
void ClientPlayerSetEQReverb( HXClientPlayerToken clientPlayerToken, SInt32 roomSize, SInt32 reverb );
void ClientPlayerGetEQReverb( HXClientPlayerToken clientPlayerToken, SInt32* pRoomSize, SInt32* pReverb );
bool ClientPlayerAddAudioHook( HXClientPlayerToken clientPlayerToken, const HXAudioHookCallbacks* pAudioHookCallbacks, void* hookInfo );
void ClientPlayerRemoveAudioHook( HXClientPlayerToken clientPlayerToken, const HXAudioHookCallbacks* pAudioHookCallbacks, void* hookInfo );
bool ClientPlayerGetVideoAttribute( HXClientPlayerToken clientPlayerToken, int attributeKey, float* pAttributeValue );
bool ClientPlayerSetVideoAttribute( HXClientPlayerToken clientPlayerToken, int attributeKey, float attributeValue );
bool ClientPlayerGetStatistic( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, unsigned char* pValueBuffer, UInt32 bufferLength, int* pValueType, UInt32* pUsedBufferLength );
bool ClientPlayerAddStatisticObserver( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, const HXStatisticsCallbacks* pStatisticsCallbacks, void* observerInfo );
void ClientPlayerRemoveStatisticObserver( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, const HXStatisticsCallbacks* pStatisticsCallbacks, void* observerInfo );
bool ClientPlayerGetRegistryData( HXClientPlayerToken clientPlayerToken, const char* pDataKey, unsigned char* pValueBuffer, UInt32 bufferLength, int* pValueType, UInt32* pUsedBufferLength );
bool ClientPlayerSetRegistryData( HXClientPlayerToken clientPlayerToken, const char* pDataKey, const unsigned char* pValueBuffer, UInt32 bufferLength, int valueType );
void ClientPlayerEnableSuperBuffer( HXClientPlayerToken clientPlayerToken, bool enable );
bool ClientPlayerIsSuperBufferEnabled( HXClientPlayerToken clientPlayerToken );
bool ClientPlayerSetPreferredSuperBufferSize( HXClientPlayerToken clientPlayerToken, UInt32 millisecs );
UInt32 ClientPlayerGetPreferredSuperBufferSize( HXClientPlayerToken clientPlayerToken );
UInt32 ClientPlayerGetSuperBufferSize( HXClientPlayerToken clientPlayerToken );

#ifdef __cplusplus
}
#endif

#endif
