# © 2006-2008 Novell Inc. Licensed under the MIT X11 license.
# Written by Aaron Bockover <abockover@novell.com>
#
# This script changes the formatting of any filenames that Banshee
# will create while managing your library (i.e. CD ripping)
#
# The provided pattern transforms all filenames to lower case,
# removes any non-digit/alpha characters, and replaces spaces 
# with underscores for the UNIX diehard.

Banshee.Base.FileNamePattern.Filter = { songpath as string |
    @/[ ]+/.Replace (@/[^0-9A-Za-z\/ ]+/.Replace (songpath, "").ToLower (), "_")
}
