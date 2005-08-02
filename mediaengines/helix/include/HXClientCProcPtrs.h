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

/* Standard C function Proc Ptrs for accessing the Helix Client Core. */

#ifndef _HXCLIENT_CPROCPTRS_H_
#define _HXCLIENT_CPROCPTRS_H_

#ifdef __cplusplus
extern "C" {
#endif

#include "HXClientTypes.h"
#include "HXClientCallbacks.h"

#ifdef _MAC_MACHO
typedef CFStringRef ( *ClientCreateErrorStringProcPtr ) ( UInt32 hxCode, const char* pErrorString );
typedef bool ( *ClientEngineHandleClassicEventProcPtr ) ( EventRecord* classicEvent );
#endif

typedef void ( *ClientEngineSetCallbacksProcPtr ) ( const HXClientEngineCallbacks* pClientEngineCallbacks );

typedef bool ( *ClientPlayerCreateProcPtr ) ( HXClientPlayerToken* pClientPlayerToken, SHXClientWindow* pWindow, void* userInfo, const HXClientCallbacks* pClientCallbacks );
typedef void ( *ClientPlayerCloseProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerOpenURLProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pURL, const char* pMimeType );
typedef bool ( *ClientPlayerOpenDataProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pURL, const char* pMimeType, UInt32 dataLength, bool autoPlay, void** ppOutData );
typedef bool ( *ClientPlayerWriteDataProcPtr ) ( HXClientPlayerToken clientPlayerToken, void* pData, UInt32 bufferLength, unsigned char* pBuffer );
typedef void ( *ClientPlayerCloseDataProcPtr ) ( HXClientPlayerToken clientPlayerToken, void* pData );
typedef bool ( *ClientPlayerGetOpenedURLProcPtr ) ( HXClientPlayerToken clientPlayerToken, char* pURLBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
typedef bool ( *ClientPlayerCanViewSourceProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerViewSourceProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerCanViewRightsProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerViewRightsProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerAuthenticateProcPtr ) ( HXClientPlayerToken clientPlayerToken, bool shouldValidateUser, const char* pUsername, const char* pPassword );
typedef int ( *ClientPlayerGetContentStateProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerSetStatusProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pStatus );
typedef void ( *ClientPlayerPlayProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerPauseProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerStopProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerGetBufferedRangeProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt32* pMinPosition, UInt32* pWritePosition, UInt32* pMaxPosition );
typedef bool ( *ClientPlayerGetSeekRangeProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt32* pMinPosition, UInt32* pMaxPosition );
typedef bool ( *ClientPlayerStartSeekingProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerSetPositionProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt32 position );
typedef void ( *ClientPlayerStopSeekingProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef UInt32 ( *ClientPlayerGetPositionProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef UInt32 ( *ClientPlayerGetLengthProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerIsLiveProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef const char* ( *ClientPlayerGetTitleProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef const char* ( *ClientPlayerGetContextURLProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerHasVisualContentProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerGetIdealSizeProcPtr ) ( HXClientPlayerToken clientPlayerToken, SInt32* pSiteIdealWidth, SInt32* pSiteIdealHeight );
typedef SInt32 ( *ClientPlayerGetClipBandwidthProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerSetSizeProcPtr ) ( HXClientPlayerToken clientPlayerToken, SInt32 siteWidth, SInt32 siteHeight );
typedef UInt16 ( *ClientPlayerGetSourceCountProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef UInt16 ( *ClientPlayerGetGroupCountProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef UInt16 ( *ClientPlayerGetCurrentGroupProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerGetGroupURLProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex, char* pURLBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
typedef bool ( *ClientPlayerGetGroupTitleProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex, char* pTitleBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
typedef bool ( *ClientPlayerSetCurrentGroupProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt16 groupIndex );
typedef void ( *ClientPlayerDrawSiteProcPtr ) ( HXClientPlayerToken clientPlayerToken, const SHXClientRect* pSiteRect );
typedef void ( *ClientPlayerSetVolumeProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt16 volume );
typedef UInt16 ( *ClientPlayerGetVolumeProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerMuteProcPtr ) ( HXClientPlayerToken clientPlayerToken, bool shouldMute );
typedef bool ( *ClientPlayerIsMutedProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerEnableEQProcPtr ) ( HXClientPlayerToken clientPlayerToken, bool enable );
typedef bool ( *ClientPlayerIsEQEnabledProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerSetEQGainProcPtr ) ( HXClientPlayerToken clientPlayerToken, int band, SInt32 gain );
typedef SInt32 ( *ClientPlayerGetEQGainProcPtr ) ( HXClientPlayerToken clientPlayerToken, int band );
typedef void ( *ClientPlayerSetEQPreGainProcPtr ) ( HXClientPlayerToken clientPlayerToken, SInt32 preGain );
typedef SInt32 ( *ClientPlayerGetEQPreGainProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerEnableEQAutoPreGainProcPtr ) ( HXClientPlayerToken clientPlayerToken, bool enable );
typedef bool ( *ClientPlayerIsEQAutoPreGainEnabledProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef void ( *ClientPlayerSetEQReverbProcPtr ) ( HXClientPlayerToken clientPlayerToken, SInt32 roomSize, SInt32 reverb );
typedef void ( *ClientPlayerGetEQReverbProcPtr ) ( HXClientPlayerToken clientPlayerToken, SInt32* pRoomSize, SInt32* pReverb );
typedef bool ( *ClientPlayerAddAudioHookProcPtr) ( HXClientPlayerToken clientPlayerToken, const HXAudioHookCallbacks* pAudioHookCallbacks, void* hookInfo );
typedef void ( *ClientPlayerRemoveAudioHookProcPtr) ( HXClientPlayerToken clientPlayerToken, const HXAudioHookCallbacks* pAudioHookCallbacks, void* hookInfo );
typedef bool ( *ClientPlayerGetVideoAttributeProcPtr ) ( HXClientPlayerToken clientPlayerToken, int attributeKey, float* pAttributeValue );
typedef bool ( *ClientPlayerSetVideoAttributeProcPtr ) ( HXClientPlayerToken clientPlayerToken, int attributeKey, float attributeValue );
typedef bool ( *ClientPlayerGetStatisticProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, unsigned char* pValueBuffer, UInt32 bufferLength, int* pValueType, UInt32* pUsedBufferLength );
typedef bool ( *ClientPlayerAddStatisticObserverProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, const HXStatisticsCallbacks* pStatisticsCallbacks, void* observerInfo );
typedef void ( *ClientPlayerRemoveStatisticObserverProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pStatisticKey, const HXStatisticsCallbacks* pStatisticsCallbacks, void* observerInfo );
typedef bool ( *ClientPlayerGetRegistryDataProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pDataKey, unsigned char* pValueBuffer, UInt32 bufferLength, int* pValueType, UInt32* pUsedBufferLength );
typedef bool ( *ClientPlayerSetRegistryDataProcPtr ) ( HXClientPlayerToken clientPlayerToken, const char* pDataKey, const unsigned char* pValueBuffer, UInt32 bufferLength, int valueType );
typedef void ( *ClientPlayerEnableSuperBufferProcPtr ) ( HXClientPlayerToken clientPlayerToken, bool enable );
typedef bool ( *ClientPlayerIsSuperBufferEnabledProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef bool ( *ClientPlayerSetPreferredSuperBufferSizeProcPtr ) ( HXClientPlayerToken clientPlayerToken, UInt32 millisecs );
typedef UInt32 ( *ClientPlayerGetPreferredSuperBufferSizeProcPtr ) ( HXClientPlayerToken clientPlayerToken );
typedef UInt32 ( *ClientPlayerGetSuperBufferSizeProcPtr ) ( HXClientPlayerToken clientPlayerToken );

#ifdef __cplusplus
}
#endif

#endif
