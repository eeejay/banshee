#!/bin/bash

VERBOSE=0
BUILD_LOG=`pwd`/build-log

pushd $(dirname $0) &>/dev/null
source build.env || exit $?

function show_help () {
	echo "Usage: $0 [options] [targets]"
	echo
	echo "Available Options:"
	echo "  -h, --help        show this help"
	echo "  -v, --verbose     show all build messages"
	echo "  -r, --root        name of the build root (default=bundle)"
	echo
	exit 1
}

function bail () {
	echo "ERROR: $1" 1>&2
	exit $2
}

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

function run () {
	echo "--> Running: $@"
	BAIL_MESSAGE="Failed to run $1 against ${TARGET_NAME}"
	if [ $VERBOSE -ne 0 ]; then
		$@ || bail "${BAIL_MESSAGE}" $?
	else 
		$@ &>$BUILD_LOG || bail "${BAIL_MESSAGE}" $?
	fi
}

ALL_TARGETS=()

function append_target () {
	FILE=$1
	[[ -f $FILE ]] || { FILE="$FILE.targets"; [[ -f $FILE ]] || { FILE=targets/$FILE; }; }
	source $FILE &>/dev/null || bail "Could not load target set '$FILE'" 1
	echo "Loading target set '$FILE'"
	for ((i = 0, n = ${#TARGETS[@]}; i < n; i++)); do
		ALL_TARGETS[${#ALL_TARGETS[*]}]=${TARGETS[$i]}
	done
}

for arg in $@; do
	case $arg in
		-v|--verbose) VERBOSE=1 ;;
		-h|--help)    show_help ;;
		-*)           bail "Unknown argument: $arg" 1 ;;
		*)            append_target $arg ;;
	esac
done

if [ ${#ALL_TARGETS[@]} -eq 0 ]; then
	for target_file in $(find $(dirname $0)/targets -maxdepth 1 -name \*.targets); do
		append_target $target_file
	done
fi

SOURCES_ROOT=deps/bundle-sources
mkdir -p $SOURCES_ROOT
pushd $SOURCES_ROOT &>/dev/null

echo "Starting to build all targets..."
echo

for ((i = 0, n = ${#ALL_TARGETS[@]}; i < n; i++)); do
	# Break the target definition into its parts
	TARGET=(${ALL_TARGETS[$i]})
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
			curl -Lsf -O $TARGET_URI || bail "Failed to download: ${TARGET_NAME}" $?
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
		if [ ! -f patched ]; then
			patches=$(find ../.. -maxdepth 1 -name ${TARGET_NAME}\*.patch)
			for patch in $patches; do
				echo "--> Running: patch -p0 < $patch"
				patch -p0 < $patch 1>/dev/null || bail "Could not apply patch $patch to $TARGET_NAME" $?
				touch patched
			done
		fi
		
		CONFIGURE=./configure
		if [ -f ../../${TARGET_NAME}-autogen.sh ]; then
			CONFIGURE=../../${TARGET_NAME}-autogen.sh
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

echo "Success! Done."

