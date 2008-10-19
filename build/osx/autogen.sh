#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env

if [ ! -d $BUILD_PREFIX ]; then
	echo "Error: Banshee bundle dependencies do not appear to be built."
	echo "       please run the build-deps.sh script, and refer to the"
	echo "       README file for building on Mac OS X."
	echo
	echo "       $(dirname $0)/README"
	echo
	exit 1
fi

pushd ../.. &>/dev/null

# Fix up the configure.ac script, unfortunately there
# is not an easy way around this; autoconf just sucks
if [ ! -f configure.ac.orig ]; then
	cp configure.ac configure.ac.orig
	grep -v AM_GCONF_SOURCE_2 < configure.ac.orig > configure.ac
fi

# Run the upstream autogen
./autogen.sh \
	--disable-mtp \
	--disable-daap \
	--disable-ipod \
	--disable-docs \
	--disable-gnome

popd &>/dev/null
popd &>/dev/null

