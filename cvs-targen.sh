#!/bin/sh

DATE=`date +"%Y%m%d-%H%M"`
DIR="banshee-cvs-$DATE"
FILE="$DIR.tar.gz"

if [ -e Makefile ]; then
	make maintainer-clean
fi

mkdir $DIR

for i in `find ./`; do
	if test "x$i" = "x./$DIR"; then
		continue;
	fi

	if test "x`echo $i | grep 'CVS'`" != "x"; then
		continue;
	fi

	if test "x`echo $i | grep 'snapshots'`" != "x"; then
		continue;
	fi

	if [ -d $i ]; then
		if test "x$i" != "x./"; then
			mkdir $DIR/$i
		fi
	else
		cp $i $DIR/$i
	fi
done

cd $DIR
if ./autogen.sh && make && make maintainer-clean; then
	cd ..
	tar cfz $FILE $DIR
	rm -rf $DIR
	mkdir -p snapshots
	mv $FILE snapshots
	echo
	echo "*********************************************************"
	echo "CVS Snapshot '$FILE' ready!"
	echo "*********************************************************"
else
	cd ..
	echo
	echo "*********************************************************"
	echo "ERROR: Build failed!"
	echo "*********************************************************"
fi

