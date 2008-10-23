#!/bin/bash

pushd $(dirname $0) &>/dev/null

./collect-deps.sh

rm -rf Banshee.app
cp -rf app-bundle-data Banshee.app

DEST=Banshee.app/Contents/MacOS

cp -rf bundle-deps/* $DEST
cp -rf ../../bin/* $DEST
cp -rf glib-sharp-workaround $DEST

find Banshee.app -type d -iregex '.*\.svn$' | xargs rm -rf

popd &>/dev/null

