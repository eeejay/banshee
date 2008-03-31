#!/bin/bash

rm -f *.cs
svn co svn://svn.gnome.org/svn/banshee/trunk/musicbrainz-sharp/src/MusicBrainz/MusicBrainz
cp MusicBrainz/*.cs .
rm -rf MusicBrainz

