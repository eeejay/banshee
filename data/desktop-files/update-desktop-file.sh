#!/bin/bash

DESKTOP_SOURCE="$1"
VERSION="$2"
MIMETYPES_FILE="$(basename "$DESKTOP_SOURCE" .in.in).mime"

echo "[Desktop Entry]"

(cat "$DESKTOP_SOURCE" && cat common.desktop) |
	sed "s/\@VERSION\@/${VERSION}/g"

if [[ -f "$MIMETYPES_FILE" ]]; then
	MIMETYPES=$(
		grep -vE '^[[:space:]]*(#.*|)$' "$MIMETYPES_FILE" | \
		sort | uniq | \
		awk '{printf $1 ";"}' | sed 's,;$,,'
	)

	echo "MimeType=$MIMETYPES"
fi

