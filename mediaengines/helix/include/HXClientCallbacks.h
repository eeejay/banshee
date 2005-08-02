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

#ifndef _HXCLIENT_CALLBACKS_H_
#define _HXCLIENT_CALLBACKS_H_

#include "HXClientTypes.h"

typedef void ( *HXOnVisualStateChangedProcPtr ) ( void* userInfo, bool hasVisualContent );
typedef void ( *HXOnIdealSizeChangedProcPtr ) ( void* userInfo, SInt32 idealWidth, SInt32 idealHeight );
typedef void ( *HXOnLengthChangedProcPtr ) ( void* userInfo, UInt32 length );
typedef void ( *HXOnTitleChangedProcPtr ) ( void* userInfo, const char* pTitle );
typedef void ( *HXOnGroupsChangedProcPtr ) ( void* userInfo );
typedef void ( *HXOnGroupStartedProcPtr ) ( void* userInfo, UInt16 groupIndex );
typedef void ( *HXOnContactingProcPtr ) ( void* userInfo, const char* pHostName );
typedef void ( *HXOnBufferingProcPtr ) ( void* userInfo, UInt32 bufferingReason, UInt16 bufferPercent );
typedef void ( *HXOnContentStateChangedProcPtr ) ( void* userInfo, int oldContentState, int newContentState );
typedef void ( *HXOnContentConcludedProcPtr ) ( void* userInfo );
typedef void ( *HXOnStatusChangedProcPtr ) ( void* userInfo, const char* pStatus );
typedef void ( *HXOnVolumeChangedProcPtr ) ( void* userInfo, UInt16 volume );
typedef void ( *HXOnMuteChangedProcPtr ) ( void* userInfo, bool hasMuted );
typedef void ( *HXOnClipBandwidthChangedProcPtr ) ( void* userInfo, SInt32 clipBandwidth );
typedef void ( *HXOnErrorOccurredProcPtr ) ( void* userInfo, UInt32 hxCode, UInt32 userCode, const char* pErrorString, const char* pUserString, const char* pMoreInfoURL );
typedef bool ( *HXGoToURLProcPtr ) ( void* userInfo, const char* pURL, const char* pTarget, bool isPlayerURL, bool isAutoActivated ); // pTarget could be NULL.
typedef bool ( *HXRequestAuthenticationProcPtr ) ( void* userInfo, const char* pServer, const char* pRealm, bool isProxyServer );
typedef bool ( *HXRequestUpgradeProcPtr ) ( void* userInfo, const char* pURL, UInt32 numOfComponents, const char* componentNames[], bool isBlocking );
typedef bool ( *HXHasComponentProcPtr ) ( void* userInfo, const char* componentName );

typedef void ( *HXPrivateProcPtr ) ( void* userInfo );

typedef struct HXClientCallbacks
{
	HXOnVisualStateChangedProcPtr OnVisualStateChanged;
	HXOnIdealSizeChangedProcPtr OnIdealSizeChanged;
	HXOnLengthChangedProcPtr OnLengthChanged;
	HXOnTitleChangedProcPtr OnTitleChanged;
	HXOnGroupsChangedProcPtr OnGroupsChanged;
	HXOnGroupStartedProcPtr OnGroupStarted;
	HXOnContactingProcPtr OnContacting;
	HXOnBufferingProcPtr OnBuffering;
	HXOnContentStateChangedProcPtr OnContentStateChanged;
	HXOnContentConcludedProcPtr OnContentConcluded;
	HXOnStatusChangedProcPtr OnStatusChanged;
	HXOnVolumeChangedProcPtr OnVolumeChanged;
	HXOnMuteChangedProcPtr OnMuteChanged;
	HXOnClipBandwidthChangedProcPtr OnClipBandwidthChanged;
	HXOnErrorOccurredProcPtr OnErrorOccurred;
	HXGoToURLProcPtr GoToURL;
	HXRequestAuthenticationProcPtr RequestAuthentication;
	HXRequestUpgradeProcPtr RequestUpgrade;
	HXHasComponentProcPtr HasComponent;
	
	HXPrivateProcPtr PrivateCallback1;
	HXPrivateProcPtr PrivateCallback2;
}
HXClientCallbacks;

// See HXClientConstants.h for valueType's
typedef void ( *HXOnAddedStatisticProcPtr ) ( const char* pStatisticName, int valueType, const unsigned char* pValue, void* observerInfo );
typedef void ( *HXOnModifiedStatisticProcPtr ) ( const char* pStatisticName, int valueType, const unsigned char* pValue, void* observerInfo );
typedef void ( *HXOnDeletedStatisticProcPtr ) ( const char* pStatisticName, void* observerInfo );

typedef struct HXStatisticsCallbacks
{
	HXOnAddedStatisticProcPtr OnAddedStatistic;
	HXOnModifiedStatisticProcPtr OnModifiedStatistic;
	HXOnDeletedStatisticProcPtr OnDeletedStatistic;
}
HXStatisticsCallbacks;

// Audio Hook related
typedef void ( *HXInitAudioDataProcPtr ) ( const SHXAudioFormat* pAudioFormat, void* hookInfo );
typedef void ( *HXOnAudioBufferProcPtr ) ( unsigned char* pAudioBuffer, UInt32 bufferLength, UInt32 audioStartTime, int audioStreamType, void* hookInfo );

typedef struct HXAudioHookCallbacks
{
	HXInitAudioDataProcPtr InitAudioData;
	HXOnAudioBufferProcPtr OnAudioBuffer;
}
HXAudioHookCallbacks;

// If pValueBuffer is NULL and/or bufferLength is 0, just return the preference's size in pUsedBufferLength, assuming it isn't NULL.
typedef bool ( *HXReadPreferenceProcPtr ) ( const char* pPrefKey, unsigned char* pValueBuffer, UInt32 bufferLength, UInt32* pUsedBufferLength );
typedef bool ( *HXWritePreferenceProcPtr ) ( const char* pPrefKey, const unsigned char* pValueBuffer, UInt32 bufferLength );
typedef bool ( *HXDeletePreferenceProcPtr ) ( const char* pPrefKey );
typedef bool ( *HXHasFeatureProcPtr ) ( const char* featureName );

typedef struct HXClientEngineCallbacks
{
	HXReadPreferenceProcPtr ReadPreference;
	HXWritePreferenceProcPtr WritePreference;
	HXDeletePreferenceProcPtr DeletePreference;
	HXHasFeatureProcPtr HasFeature;
}
HXClientEngineCallbacks;

#endif
