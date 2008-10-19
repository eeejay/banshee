#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env

BUNDLE=bundle-deps

rm -rf $BUNDLE

mkdir $BUNDLE
mkdir $BUNDLE/gstreamer-0.10

find $BUILD_PREFIX/lib -name *.dylib -exec cp {} $BUNDLE \;
find $BUILD_PREFIX/lib/gstreamer-0.10 -name *.so -exec cp {} $BUNDLE/gstreamer-0.10 \;
find $BUILD_PREFIX/lib/mono -name *.dll -not -name *policy* -type f -exec cp {} $BUNDLE \;

popd &>/dev/null

