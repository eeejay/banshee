#!/bin/bash

pushd $(dirname $0) &>/dev/null

rm -rf Banshee.app
cp -rf app-bundle-data Banshee.app

DEST=Banshee.app/Contents/MacOS

cp -rf bundle-deps/* $DEST
cp -rf ../../bin/* $DEST
cp -rf glib-sharp-workaround $DEST

zip -r Banshee.zip Banshee.app

rm -rf Banshee.app

popd &>/dev/null

