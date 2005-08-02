/***************************************************************************
 *  HXPlayer.c
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
#include <stdlib.h>
#include <stdio.h>
#include <memory.h>

#include "HXPlayer.h"

/* Callback Execution Macros */

#define CBC(PLAYER_PTR, CALLBACK)              \
	HXPlayer *player = (HXPlayer *)PLAYER_PTR; \
	if(player->callbacks.CALLBACK != NULL)

#define RUN_CALLBACK_1(PLAYER_PTR, CALLBACK, A) \
	{ CBC(PLAYER_PTR, CALLBACK)(player->callbacks.CALLBACK)(A); } 

#define RUN_CALLBACK_2(PLAYER_PTR, CALLBACK, A, B) \
	{ CBC(PLAYER_PTR, CALLBACK)(player->callbacks.CALLBACK)(A, B); } 
	
#define RUN_CALLBACK_3(PLAYER_PTR, CALLBACK, A, B, C) \
	{ CBC(PLAYER_PTR, CALLBACK)(player->callbacks.CALLBACK)(A, B, C); } 
	
#define RUN_CALLBACK_6(PLAYER_PTR, CALLBACK, A, B, C, D, E, F) \
	{ CBC(PLAYER_PTR, CALLBACK)(player->callbacks.CALLBACK)(A, B, C, D, E, F); } 
	
/* Default Client Engine Callback Signatures */
	
static void OnVisualStateChanged(void* userInfo, bool hasVisualContent);
static void OnIdealSizeChanged(void* userInfo, SInt32 idealWidth, 
	SInt32 idealHeight);

static void OnLengthChanged(void* userInfo, UInt32 length);
static void OnTitleChanged(void* userInfo, const char* pTitle);
static void OnGroupsChanged(void* userInfo);
static void OnGroupStarted(void* userInfo, UInt16 groupIndex);
static void OnContacting(void* userInfo, const char* pHostName);

static void OnBuffering(void* userInfo, UInt32 bufferingReason, 
	UInt16 bufferPercent);

static void OnContentConcluded(void* userInfo);
static void OnContentStateChanged(void* userInfo, int oldContentState, 
	int newContentState);

static void OnStatusChanged(void* userInfo, const char* pStatus);
static void OnVolumeChanged(void* userInfo, UInt16 volume);
static void OnMuteChanged(void* userInfo, bool hasMuted);
static void OnClipBandwidthChanged(void* userInfo, SInt32 clipBandwidth);

static void OnErrorOccurred(void* userInfo, UInt32 hxCode, UInt32 userCode,
		const char* pErrorString, const char* pUserString,
		const char* pMoreInfoURL);

static bool GoToURL(void* userInfo, const char* pURL, const char* pTarget, 
	bool isPlayerURL, bool isAutoActivated);

static bool RequestAuthentication(void* userInfo, const char* pServer, 
	const char* pRealm, bool isProxyServer);

static bool RequestUpgrade(void* userInfo, const char* pUrl, 
	UInt32 numOfComponents, const char* componentNames[], bool isBlocking);
		   
static bool HasComponent(void* userInfo, const char* componentName);

/* Default Client Engine Callback Connection */

static const HXClientCallbacks hxPlayerCallbacks = {
	OnVisualStateChanged,
	OnIdealSizeChanged,
	OnLengthChanged,
	OnTitleChanged,
	OnGroupsChanged,
	OnGroupStarted,
	OnContacting,
	OnBuffering,
	OnContentStateChanged,
	OnContentConcluded,
	OnStatusChanged,
	OnVolumeChanged,
	OnMuteChanged,
	OnClipBandwidthChanged,
	OnErrorOccurred,
	GoToURL,
	RequestAuthentication,
	RequestUpgrade,
	HasComponent
};

/* Default Client Engine Callbacks */ 

static void
OnVisualStateChanged(void* userInfo, bool hasVisualContent)
{
	RUN_CALLBACK_2(userInfo, OnVisualStateChanged, userInfo, hasVisualContent);
}

static void
OnIdealSizeChanged(void* userInfo, SInt32 idealWidth, SInt32 idealHeight)
{
	RUN_CALLBACK_3(userInfo, OnIdealSizeChanged, userInfo, 
		idealWidth, idealHeight);
}

static void
OnLengthChanged(void* userInfo, UInt32 length)
{
	RUN_CALLBACK_2(userInfo, OnLengthChanged, userInfo, length);
}

static void
OnTitleChanged(void* userInfo, const char* pTitle)
{
	RUN_CALLBACK_2(userInfo, OnTitleChanged, userInfo, pTitle);
}

static void
OnGroupsChanged(void* userInfo)
{
	RUN_CALLBACK_1(userInfo, OnGroupsChanged, userInfo);
}

static void
OnGroupStarted(void* userInfo, UInt16 groupIndex)
{
	RUN_CALLBACK_2(userInfo, OnGroupStarted, userInfo, groupIndex);
}

static void
OnContacting(void* userInfo, const char* pHostName)
{
	RUN_CALLBACK_2(userInfo, OnContacting, userInfo, pHostName);
}

static void
OnBuffering(void* userInfo, UInt32 bufferingReason, UInt16 bufferPercent)
{
	RUN_CALLBACK_3(userInfo, OnBuffering, userInfo, bufferingReason,
		bufferPercent);
}

static void
OnContentConcluded(void* userInfo)
{
	RUN_CALLBACK_1(userInfo, OnContentConcluded, userInfo);
}

static void
OnContentStateChanged(void* userInfo, int oldContentState, int newContentState)
{
	RUN_CALLBACK_3(userInfo, OnContentStateChanged, userInfo, oldContentState,
		newContentState);
}

static void
OnStatusChanged(void* userInfo, const char* pStatus)
{
	RUN_CALLBACK_2(userInfo, OnStatusChanged, userInfo, pStatus);
}

static void
OnVolumeChanged(void* userInfo, UInt16 volume)
{
	RUN_CALLBACK_2(userInfo, OnVolumeChanged, userInfo, volume);
}

static void
OnMuteChanged(void* userInfo, bool hasMuted)
{
	RUN_CALLBACK_2(userInfo, OnMuteChanged, userInfo, hasMuted);
}

static void
OnClipBandwidthChanged(void* userInfo, SInt32 clipBandwidth)
{
	RUN_CALLBACK_2(userInfo, OnClipBandwidthChanged, userInfo, clipBandwidth);
}

static void
OnErrorOccurred(void* userInfo, UInt32 hxCode, UInt32 userCode,
		const char* pErrorString, const char* pUserString,
		const char* pMoreInfoURL)		
{
	RUN_CALLBACK_6(userInfo, OnErrorOccurred, userInfo, hxCode, userCode,
		pErrorString, pUserString, pMoreInfoURL);
}

bool
GoToURL(void* userInfo, const char* pURL, const char* pTarget, 
	bool isPlayerURL, bool isAutoActivated)
{
    return FALSE;
}

bool
RequestAuthentication(void* userInfo, const char* pServer, const char* pRealm,
		      bool isProxyServer)
{
    return FALSE;
}

bool
RequestUpgrade(void* userInfo, const char* pUrl, UInt32 numOfComponents,
	       const char* componentNames[], bool isBlocking)
{
    return FALSE;
}

bool
HasComponent(void* userInfo, const char* componentName)
{
    return FALSE;
}

/* HXPlayer Client Engine Core Wrapper */

HXPlayer *HXPlayerCreate()
{
	HXPlayer *player = (HXPlayer *)malloc(sizeof(HXPlayer));
	if(player == NULL)
		return NULL;
	
	player->handle = 0;
	player->state = 0;
	player->initialized = FALSE;
	
	memset(&player->callbacks, 0, sizeof(HXClientCallbacks));
	
	return player;
}

bool HXPlayerInit(HXPlayer *player)
{
	if(player == NULL)
		return FALSE;
	
	setenv("HELIX_LIBS", "/usr/lib/RealPlayer10", 0);
	
	if(ClientPlayerCreate(&player->handle, 0, player, &hxPlayerCallbacks))
		player->initialized = TRUE;
		
	return player->initialized;
}

void HXPlayerShutdown(HXPlayer *player)
{
	if(player == NULL)
		return;
	
	if(!player->initialized)
		return;
	
	ClientPlayerClose(player->handle);
	player->initialized = FALSE;
}
	
void HXPlayerFree(HXPlayer *player)
{
	if(player == NULL);
		return;
	
	if(player->initialized)
		HXPlayerShutdown(player);
	
	free(player);
}

int HXPlayerIterate(HXPlayer *player)
{
	if(player == NULL)
		return 0;
	
	usleep(20);
	ClientEngineProcessXEvent(NULL);
	return ClientPlayerGetContentState(player->handle);
}

HXClientPlayerToken HXPlayerGetHandle(HXPlayer *player)
{
	if(player == NULL)
		return NULL;
	
	return player->handle;
}

/* This is a hack... need to figure out why mono won't properly p/invoke
   into hxclient, but works fine here. */
   
unsigned int HXPlayerGetVolume(HXPlayer *player)
{
	if(player == NULL)
		return 0;
		
	return ClientPlayerGetVolume(player->handle);
}

void HXPlayerSetVolume(HXPlayer *player, unsigned int volume)
{
	if(player == NULL)
		return;
		
	ClientPlayerSetVolume(player->handle, volume);
}

int HXPlayerGetPosition(HXPlayer *player)
{
	if(player == NULL)
		return -1;
		
	return ClientPlayerGetPosition(player->handle);
}

void HXPlayerSetPosition(HXPlayer *player, int position)
{
	if(player == NULL)
		return;
		
	ClientPlayerSetPosition(player->handle, position);
}

void HXPlayerStop(HXPlayer *player)
{
	if(player == NULL)
		return;
		
	ClientPlayerStop(player->handle);
}

bool HXPlayerOpenUrl(HXPlayer *player, const char *url)
{
	return ClientPlayerOpenUrl(player->handle, url, NULL);
}

void HXPlayerPlay(HXPlayer *player)
{
	if(player == NULL)
		return;
		
	ClientPlayerPlay(player->handle);
}

void HXPlayerPause(HXPlayer *player)
{
	if(player == NULL)
		return;
		
	ClientPlayerPause(player->handle);
}

/* Client Engine Callback Registrations */

void HXPlayerRegisterOnVisualStateChangedCallback(HXPlayer *player, 
	HXOnVisualStateChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnVisualStateChanged = cb;
}

void HXPlayerRegisterOnIdealSizeChangedCallback(HXPlayer *player, 
	HXOnIdealSizeChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnIdealSizeChanged = cb;
}

void HXPlayerRegisterOnLengthChangedCallback(HXPlayer *player, 
	HXOnLengthChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnLengthChanged = cb;
}

void HXPlayerRegisterOnTitleChangedCallback(HXPlayer *player, 
	HXOnTitleChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnTitleChanged = cb;
}

void HXPlayerRegisterOnGroupsChangedCallback(HXPlayer *player, 
	HXOnGroupsChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnGroupsChanged = cb;
}

void HXPlayerRegisterOnGroupStartedCallback(HXPlayer *player, 
	HXOnGroupStartedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnGroupStarted = cb;
}

void HXPlayerRegisterOnContactingCallback(HXPlayer *player, 
	HXOnContactingProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnContacting = cb;
}

void HXPlayerRegisterOnBufferingCallback(HXPlayer *player, 
	HXOnBufferingProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnBuffering = cb;
}

void HXPlayerRegisterOnContentStateChangedCallback(HXPlayer *player, 
	HXOnContentStateChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnContentStateChanged = cb;
}

void HXPlayerRegisterOnContentConcludedCallback(HXPlayer *player, 
	HXOnContentConcludedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnContentConcluded = cb;
}

void HXPlayerRegisterOnStatusChangedCallback(HXPlayer *player, 
	HXOnStatusChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnStatusChanged = cb;
}

void HXPlayerRegisterOnVolumeChangedCallback(HXPlayer *player, 
	HXOnVolumeChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnVolumeChanged = cb;
}

void HXPlayerRegisterOnMuteChangedCallback(HXPlayer *player, 
	HXOnMuteChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnMuteChanged = cb;
}

void HXPlayerRegisterOnClipBandwidthChangedCallback(HXPlayer *player, 
	HXOnClipBandwidthChangedProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnClipBandwidthChanged = cb;
}

void HXPlayerRegisterOnErrorOccurredCallback(HXPlayer *player, 
	HXOnErrorOccurredProcPtr cb)
{
	if(player != NULL)
		player->callbacks.OnErrorOccurred = cb;
}

void HXPlayerRegisterGoToURLCallback(HXPlayer *player, 
	HXGoToURLProcPtr cb)
{
	if(player != NULL)
		player->callbacks.GoToURL = cb;
}

void HXPlayerRegisterRequestAuthenticationCallback(HXPlayer *player, 
	HXRequestAuthenticationProcPtr cb)
{
	if(player != NULL)
		player->callbacks.RequestAuthentication = cb;
}

void HXPlayerRegisterRequestUpgradeCallback(HXPlayer *player, 
	HXRequestUpgradeProcPtr cb)
{
	if(player != NULL)
		player->callbacks.RequestUpgrade = cb;
}

void HXPlayerRegisterHasComponentCallback(HXPlayer *player, 
	HXHasComponentProcPtr cb)
{
	if(player != NULL)
		player->callbacks.HasComponent = cb;
}

void RegisterGeneric(HXPlayer *player, void *ptr)
{
	player->callbacks.HasComponent = ptr;	
}

void ass()
{
	RegisterGeneric(NULL, RegisterGeneric);
}
