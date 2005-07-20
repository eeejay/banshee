#!/bin/sh

MONO_BRANCH="hal-sharp"
SOURCE_EXCLUDE="HalTest.cs"
svn co svn+ssh://abock@mono-cvs.ximian.com/source/trunk/$MONO_BRANCH

echo " * Removing local sources"
rm -f *.cs

echo " * Syncing sources"
cp ./$MONO_BRANCH/src/*.cs .

echo " * Removing excludes"
for ex in $SOURCE_EXCLUDE; do
	rm $ex;
done;

echo " * Building $MONO_BRANCH.sources"
FILES="ASSEMBLY_SOURCES = "
for file in *.cs; do
	FILES="$FILES \$(srcdir)/$file"
done;
echo $FILES > $MONO_BRANCH.sources

echo " * Removing checked-out sources"
rm -rf $MONO_BRANCH

echo " ** SVN SOURCE MERGED **"

