#!/bin/bash

pushd $(dirname $0) &>/dev/null
source build.env || exit $?

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

[[ -d $BUILD_PREFIX ]] || { ./build-deps.sh || bail "Could not build dependencies"; }

pushd $SOURCE_DIR &>/dev/null
./configure --prefix=$INSTALL_PREFIX \
	--disable-mtp \
	--disable-daap \
	--disable-ipod \
	--disable-boo \
	--disable-gnome \
	--disable-docs \
	--enable-osx \
	--with-vendor-build-id="Banshee:OSX-10.5-Intel" || bail "Configure failed"
make || bail "Build failed"
make install || bail "Install failed"
popd &>/dev/null

./make-dmg-bundle.sh || "Image creation failed"
DMG_FILE=banshee-1-$VERSION.macosx.intel.dmg
mv Banshee.dmg $DMG_FILE
rm -rf $SOURCE_DIR
rm -rf $INSTALL_PREFIX

echo "$DMG_FILE is ready."

popd &>/dev/null

