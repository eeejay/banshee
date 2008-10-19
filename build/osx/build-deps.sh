#!/bin/bash

GST_DOWNLOAD_URI="http://gstreamer.freedesktop.org/src/%n/%f"
GST_CONFIGURE_ARGS="--disable-gtk-doc"

NDBUS_DOWNLOAD_URI="http://www.ndesk.org/archive/dbus-sharp/%f"

TARGETS=(
    # name (%n)        version (%v)  dir (%d)  file (%f)   download uri          configure args
	"liboil            0.3.15        %n-%v     %d.tar.gz   http://liboil.freedesktop.org/download/%f  ${GST_CONFIGURE_ARGS}"
	"gstreamer         0.10.19       %n-%v     %d.tar.gz   ${GST_DOWNLOAD_URI}                        ${GST_CONFIGURE_ARGS}"
	"gst-plugins-base  0.10.19       %n-%v     %d.tar.gz   ${GST_DOWNLOAD_URI}                        ${GST_CONFIGURE_ARGS}"
	"gst-plugins-good  0.10.7        %n-%v     %d.tar.gz   ${GST_DOWNLOAD_URI}                        ${GST_CONFIGURE_ARGS}"
	"ndesk-dbus        0.6.0         %n-%v     %d.tar.gz   ${NDBUS_DOWNLOAD_URI}"
	"ndesk-dbus-glib   0.4.1         %n-%v     %d.tar.gz   ${NDBUS_DOWNLOAD_URI}"
	"taglib-sharp      2.0.3.0       %n-%v     %d.tar.gz   http://www.taglib-sharp.com/Download/%f    --disable-docs"
	"mono-addins       0.3.1         %n-%v     %d.tar.bz2  http://go-mono.com/sources/mono-addins/%f  --disable-docs"
)

# There's probably no need to modify anything below

VERBOSE=0
BUILD_LOG=`pwd`/build-log

pushd $(dirname $0) &>/dev/null
source build.env

function show_help () {
	echo "Usage: $0 [options]"
	echo
	echo "Available Options:"
	echo "  -h, --help        show this help"
	echo "  -v, --verbose     show all build messages"
	echo
	exit 1
}

for arg in $@; do
	case $arg in
		-v|--verbose) VERBOSE=1 ;;
		-h|--help)    show_help ;;
	esac
done

function expand_target_defs () {
	for def in $@; do
		in_value=$(eval "echo \$$(echo ${def})")
		out_value=$(echo "${in_value}" | sed "
			s,%n,${TARGET_NAME},g;
			s,%v,${TARGET_VERSION},g;
			s,%d,${TARGET_DIR},g;
			s,%f,${TARGET_FILE},g;
		")
		eval $def="${out_value}"
	done
}

function bail () {
	echo "ERROR: $1" 1>&2
	exit $2
}

function run () {
	echo "--> Running: $@"
	BAIL_MESSAGE="Failed to run $1 against ${TARGET_NAME}"
	if [ $VERBOSE -ne 0 ]; then
		$@ || bail "${BAIL_MESSAGE}" $?
	else 
		$@ &>$BUILD_LOG || bail "${BAIL_MESSAGE}" $?
	fi
}

which wget &>/dev/null || bail "You need to install wget (sudo port install wget)"

SOURCES_ROOT=bundle-deps-src
mkdir -p $SOURCES_ROOT
pushd $SOURCES_ROOT &>/dev/null

for ((i = 0, n = ${#TARGETS[@]}; i < n; i++)); do
	# Break the target definition into its parts
	TARGET=(${TARGETS[$i]})
	TARGET_NAME=${TARGET[0]}
	TARGET_VERSION=${TARGET[1]}
	TARGET_DIR=${TARGET[2]}
	TARGET_FILE=${TARGET[3]}
	TARGET_URI=${TARGET[4]}
	TARGET_CONFIGURE_ARGS="${TARGET[@]:5}"

	# Perform expansion through indirect variable referencing
	expand_target_defs TARGET_DIR TARGET_FILE TARGET_URI

	echo "Processing ${TARGET_NAME} ($(($i + 1)) of $n)"

	if [ ! -d $TARGET_DIR ]; then
		# Download the tarball
		if [ ! -a $TARGET_FILE ]; then
			echo "--> Downloading ${TARGET_FILE}..."
			wget -q $TARGET_URI || bail "Failed to download: ${TARGET_NAME}" $?
		fi

		# Extract the tarball
		echo "--> Extracting ${TARGET_FILE}..."
		case ${TARGET_FILE#*tar.} in
			bz2) TAR_ARGS=jxf ;;
			gz)  TAR_ARGS=zxf ;;
			*)   bail "Unknown archive type: ${TARGET_FILE}" 1 ;;
		esac

		tar $TAR_ARGS $TARGET_FILE || bail "Could not extract archive: ${TARGET_FILE}" $?
	fi

	pushd $TARGET_DIR &>/dev/null
		CONFIGURE=./configure
		if [ -f ./autogen.sh ]; then
			CONFIGURE=./autogen.sh
		fi

		run $CONFIGURE --prefix=$BUILD_PREFIX $TARGET_CONFIGURE_ARGS
		run make clean
		run make -j2
		run make install
	popd &>/dev/null
done

popd &>/dev/null
popd &>/dev/null

test -f $BUILD_LOG && rm $BUILD_LOG

