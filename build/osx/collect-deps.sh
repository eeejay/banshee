#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env

BUNDLE=bundle-deps

rm -rf $BUNDLE

mkdir $BUNDLE
mkdir $BUNDLE/gstreamer-0.10

cp $BUILD_PREFIX/bin/{gst-launch,gst-inspect}-0.10 $BUNDLE &>/dev/null
find $BUILD_PREFIX/lib -name *.dylib -type f -exec cp {} $BUNDLE \; &>/dev/null
find $BUILD_PREFIX/lib/gstreamer-0.10 -name *.so -type f -exec cp {} $BUNDLE/gstreamer-0.10 \; &>/dev/null
find $BUILD_PREFIX/lib/mono -name *.dll* -not -name *policy* -type f -exec cp {} $BUNDLE \; &>/dev/null

popd &>/dev/null

