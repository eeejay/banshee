#!/bin/sh

DATE=`date +"%Y%m%d-%k%m"`
DIR="sonance-cvs-$DATE"
FILE="$DIR.tar.gz"

mkdir $DIR

for i in `find ./`; do
	if test "x$i" = "x./$DIR"; then
		continue;
	fi

	if test "x`echo $i | grep 'CVS'`" != "x"; then
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

