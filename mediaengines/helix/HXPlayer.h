/***************************************************************************
 *  HXPlayer.h
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

#include <HXClientCFuncs.h>
#include <HXClientCallbacks.h>
#include <HXClientConstants.h>

typedef struct {
	HXClientPlayerToken handle;
	int state;
	bool initialized;
	HXClientCallbacks callbacks;
} HXPlayer;

HXPlayer *HXPlayerCreate();
bool HXPlayerInit(HXPlayer *player);
void HXPlayerShutdown(HXPlayer *player);
void HXPlayerFree(HXPlayer *player);
int HXPlayerIterate(HXPlayer *player);

unsigned int HXPlayerGetVolume(HXPlayer *player);
void HXPlayerSetVolume(HXPlayer *player, unsigned int volume);
int HXPlayerGetPosition(HXPlayer *player);
void HXPlayerSetPosition(HXPlayer *player, int position);
void HXPlayerStop(HXPlayer *player);
bool HXPlayerOpenUrl(HXPlayer *player, const char *url);
void HXPlayerPlay(HXPlayer *player);
void HXPlayerPause(HXPlayer *player);

void HXPlayerRegisterOnVisualStateChangedCallback(HXPlayer *player, 
	HXOnVisualStateChangedProcPtr cb);

void HXPlayerRegisterOnIdealSizeChangedCallback(HXPlayer *player, 
	HXOnIdealSizeChangedProcPtr cb);

void HXPlayerRegisterOnLengthChangedCallback(HXPlayer *player, 
	HXOnLengthChangedProcPtr cb);

void HXPlayerRegisterOnTitleChangedCallback(HXPlayer *player, 
	HXOnTitleChangedProcPtr cb);

void HXPlayerRegisterOnGroupsChangedCallback(HXPlayer *player, 
	HXOnGroupsChangedProcPtr cb);

void HXPlayerRegisterOnGroupStartedCallback(HXPlayer *player, 
	HXOnGroupStartedProcPtr cb);

void HXPlayerRegisterOnContactingCallback(HXPlayer *player, 
	HXOnContactingProcPtr cb);

void HXPlayerRegisterOnBufferingCallback(HXPlayer *player, 
	HXOnBufferingProcPtr cb);

void HXPlayerRegisterOnContentStateChangedCallback(HXPlayer *player, 
	HXOnContentStateChangedProcPtr cb);

void HXPlayerRegisterOnContentConcludedCallback(HXPlayer *player, 
	HXOnContentConcludedProcPtr cb);

void HXPlayerRegisterOnStatusChangedCallback(HXPlayer *player, 
	HXOnStatusChangedProcPtr cb);

void HXPlayerRegisterOnVolumeChangedCallback(HXPlayer *player, 
	HXOnVolumeChangedProcPtr cb);

void HXPlayerRegisterOnMuteChangedCallback(HXPlayer *player, 
	HXOnMuteChangedProcPtr cb);

void HXPlayerRegisterOnClipBandwidthChangedCallback(HXPlayer *player, 
	HXOnClipBandwidthChangedProcPtr cb);

void HXPlayerRegisterOnErrorOccurredCallback(HXPlayer *player, 
	HXOnErrorOccurredProcPtr cb);

void HXPlayerRegisterGoToURLCallback(HXPlayer *player, 
	HXGoToURLProcPtr cb);

void HXPlayerRegisterRequestAuthenticationCallback(HXPlayer *player, 
	HXRequestAuthenticationProcPtr cb);

void HXPlayerRegisterRequestUpgradeCallback(HXPlayer *player, 
	HXRequestUpgradeProcPtr cb);

void HXPlayerRegisterHasComponentCallback(HXPlayer *player, 
	HXHasComponentProcPtr cb);
