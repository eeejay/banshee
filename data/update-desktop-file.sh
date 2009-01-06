#!/bin/bash

MIMETYPES_FILE=mimetypes.txt
DESKTOP_FILE=banshee-1.desktop.in.in

MIMETYPES=$(
	grep -vE '^[[:space:]]*(#.*|)$' $MIMETYPES_FILE | \
	sort | uniq | \
	awk '{printf $1 ";"}' | sed 's,;$,,'
)

sed -r "s,^MimeType=.+,MimeType=$MIMETYPES," < $DESKTOP_FILE > $DESKTOP_FILE.tmp
mv $DESKTOP_FILE.tmp $DESKTOP_FILE
