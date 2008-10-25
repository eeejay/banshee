#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env || exit $?

if [ ! -d $BUILD_PREFIX ]; then
	echo "Error: Banshee bundle dependencies do not appear to be built."
	echo "       please run the build-deps.sh script, and refer to the"
	echo "       README file for building on Mac OS X."
	echo
	echo "       $(dirname $0)/README"
	echo
	exit 1
fi

function bail () {
	echo "ERROR: Release build failed: $1"
	exit 1
}

VERSION=$1
SOURCE_DIR=banshee-1-$VERSION
TARBALL=$SOURCE_DIR.tar.bz2
INSTALL_PREFIX=$(pwd)/release-install

[[ -z "$VERSION" ]] && bail "Please specify a release version"

rm -rf $SOURCE_DIR

[[ -f $TARBALL ]] || { 
	curl -Lsf -O http://download.banshee-project.org/banshee/$TARBALL || {
		bail "Could not download tarball for release $VERSION"
	}
}

tar jxf $TARBALL || bail "Could not extract release tarball"

pushd $SOURCE_DIR &>/dev/null
./configure --prefix=$INSTALL_PREFIX \
	--disable-mtp \
	--disable-daap \
	--disable-ipod \
	--disable-boo \
	--disable-gnome \
	--disable-docs \
	--enable-osx || "Configure failed"
make || bail "Build failed"
make install || "Install failed"
popd &>/dev/null

./make-dmg-bundle.sh || "Image creation failed"
DMG_FILE=banshee-1-$VERSION.macosx.intel.dmg
mv Banshee.dmg $DMG_FILE
rm -rf $SOURCE_DIR

echo "$DMG_FILE is ready."

popd &>/dev/null

