#!/bin/sh
# Run this to generate all the initial makefiles, etc.

srcdir=`dirname $0`
test -z "$srcdir" && srcdir=.

PKG_NAME="banshee"

(test -f $srcdir/configure.ac) || {
    echo -n "**Error**: Directory "\`$srcdir\'" does not look like the"
    echo " top-level $PKG_NAME directory"
    exit 1
}

which svn 1>/dev/null || {
	echo "**Error**: subversion was not found, please install it"
	exit 1
}

which gnome-autogen.sh || {
    echo "You need to install gnome-common from the GNOME CVS"
    exit 1
}

pushd taglib-sharp
./update-checkout.sh
popd

REQUIRED_AUTOMAKE_VERSION=1.9 USE_GNOME2_MACROS=1 . gnome-autogen.sh


