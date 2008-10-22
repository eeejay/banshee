#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env || exit $?

BUNDLE=bundle-deps

rm -rf $BUNDLE

mkdir $BUNDLE
mkdir $BUNDLE/gstreamer-0.10

LIB_PREFIX=$BUILD_PREFIX/lib

#cp $BUILD_PREFIX/bin/{gst-launch,gst-inspect}-0.10 $BUNDLE &>/dev/null
find $LIB_PREFIX -name *.dylib -type f -exec cp {} $BUNDLE \; &>/dev/null
#find $LIB_PREFIX/gstreamer-0.10 -name *.so -type f -exec cp {} $BUNDLE/gstreamer-0.10 \; &>/dev/null
#find $LIB_PREFIX/mono -name *.dll* -not -name *policy* -type f -exec cp {} $BUNDLE \; &>/dev/null

pushd $BUNDLE &>/dev/null

# Rebuild symlinks
for link in $(find $LIB_PREFIX -name \*.dylib -type l); do
	ln -s "$(basename $(readlink $link))" "$(basename $link)"
done

# Relocate libraries
for dep in $(find . -type f -name \*.dylib -o -name \*.so); do
	link_deps=$(otool -L $dep | cut -f2 | cut -f1 -d' ')
	echo "Processing $dep for relocation..."
	for link_dep in $link_deps; do
		if [ "x${link_dep:0:${#LIB_PREFIX}}" = "x$LIB_PREFIX" ]; then
			echo "--> $link_dep"
			install_name_tool -change $link_dep $(basename $link_dep) $dep
		fi	
	done
done

popd &>/dev/null
popd &>/dev/null

