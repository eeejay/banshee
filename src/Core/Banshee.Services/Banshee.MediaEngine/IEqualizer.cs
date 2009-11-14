//
// IEqualizer.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2006-2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Banshee.MediaEngine
{
    public interface IEqualizer
    {
        /// <summary>
        /// Sets the gain of an equalizer band.
        /// </summary>
        void SetEqualizerGain (uint band, double value);

        /// <summary>
        /// Whether or not the engine supports the equalizer.
        /// </summary>
        bool SupportsEqualizer {
            get;
        }

        double AmplifierLevel {
            set;
        }

        /// <summary>
        /// Minimum and maximum dB values for equalizer bands.
        /// </summary>
        int [] BandRange {
            get;
        }

        uint [] EqualizerFrequencies {
            get;
        }
    }
}
