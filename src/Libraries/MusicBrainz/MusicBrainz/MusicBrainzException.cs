/***************************************************************************
 *  MusicBrainzException.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;

namespace MusicBrainz
{
    public sealed class MusicBrainzInvalidParameterException : Exception
    {
        public MusicBrainzInvalidParameterException ()
            : base ("One of the parameters is invalid. The MBID may be invalid, or you may be using an illegal parameter for this resource type.")
        {
        }
    }

    public sealed class MusicBrainzNotFoundException : Exception
    {
        public MusicBrainzNotFoundException ()
            : base ("Specified resource was not found. Perhaps it was merged or deleted.")
        {
        }
    }

    public sealed class MusicBrainzUnauthorizedException : Exception
    {
        public MusicBrainzUnauthorizedException ()
            : base ("The client is not authorized to perform this action. You may not have authenticated, or the username or password may be incorrect.")
        {
        }
    }
}
