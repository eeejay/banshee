#!/bin/bash

if [ -d TagLib ]; then
	pushd TagLib &>/dev/null
	svn up
	popd &>/dev/null
else
	CHECKOUT="$(
		if [ ! -z $1 ]; then
			echo "svn+ssh://$1@mono-cvs.ximian.com/"
		else
			echo "svn://svn.myrealbox.com/"
		fi
	)source/trunk/taglib-sharp/src/TagLib"
	echo "Checking out from $CHECKOUT"
	svn co $CHECKOUT
fi

