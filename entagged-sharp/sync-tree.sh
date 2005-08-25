#!/bin/sh

# this script is for development use only... it is used to fetch updated
# sources from an svn checkout of entagged-sharp to include in the local
# copy of the sonance development tree - do not run if you don't know 
# what you're doing.

# The TREE_SOURCE variable should point to the TOP of an entagged-sharp 
# svn checkout. After setting, sync sources by executing this script fom the
# sonance/entagged directory

MONO_BRANCH="entagged-sharp"
TRIM_FILES="Asf"
MONO_CVS_USER="abock"

#-- don't touch --#

svn co svn+ssh://$MONO_CVS_USER@mono-cvs.ximian.com/source/trunk/$MONO_BRANCH

rm -f entagged.sources

for cs in `find ./$MONO_BRANCH/ | grep -e '\.cs$'`; do
	DIR=`dirname $cs`
	
	if test ! "x${DIR:${#DIR}-1:1}" = "x/"; then
		DIR="$DIR/"
	fi
	
	DESTDIR=`echo "$DIR" | sed "s/^\.\/$MONO_BRANCH\/src\///"`
	FILE=`basename $cs`
	
	if test "x${DESTDIR:0:1}" = "x."; then
		continue;
	fi
	
	if ! test "x$DESTDIR" = "x"; then
		mkdir -p $DESTDIR
		cp $cs $DESTDIR/$FILE
	else
		cp $cs $FILE
	fi
done;

cp $MONO_BRANCH/src/Makefile.am .
touch AssemblyInfo.cs

rm -rf $MONO_BRANCH $TRIM_FILES

#AM_INC="ASSEMBLY_SOURCES = "
#for cs in `find ./ | grep -e '\.cs$'`; do
#	TRFILE=`echo "$cs" | sed 's/^.\///'`
#	AM_INC="$AM_INC \$(srcdir)/$TRFILE"
#done;
#echo $AM_INC > $MONO_BRANCH.sources

