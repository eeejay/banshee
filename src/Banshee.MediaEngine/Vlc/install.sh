#!/bin/sh

LIBDIR=`pkg-config --variable=libdir banshee`
MEDIAENGINEDIR=$LIBDIR/banshee/mediaengines/

if test "x$LIBDIR" = "x"; then
	echo "Banshee does not seem to be installed!"
	exit 1;
fi

if test ! -w $MEDIAENGINEDIR; then
	echo "You do not have write permission on $MEDIAENGINEDIR"
	exit 1
fi

echo "Installing VLC Engine for Banshee..."
cp -rf vlc $MEDIAENGINEDIR
echo "Run Banshee and select the VLC engine from Preferences"

exit 0


