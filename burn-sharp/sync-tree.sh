#!/bin/sh

SOURCE_ROOT="/home/snorp/baz/snorp/libnautilus-burn-sharp"

cp $SOURCE_ROOT/src/*.c .
cp $SOURCE_ROOT/src/generated/*.cs .

FILES="ASSEMBLY_SOURCES = "
for file in `find ./ | grep -e '.cs$'`; do
	TRFILE=`echo "$file" | sed 's/^.\///'`
	FILES="$FILES \$(srcdir)/$TRFILE"
done;
echo $FILES > burn-sharp.sources

