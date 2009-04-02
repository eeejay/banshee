#!/bin/bash

rm -f *.cs
svn co http://svn.musicbrainz.org/musicbrainz-sharp/trunk/src/MusicBrainz/MusicBrainz
cp MusicBrainz/*.cs .
rm -rf MusicBrainz

