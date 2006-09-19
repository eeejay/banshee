#!/bin/bash

if [ -d TagLib ]; then
	pushd TagLib &>/dev/null
	svn up
	popd &>/dev/null
else
	svn co svn://svn.myrealbox.com/source/trunk/taglib-sharp/src/TagLib
fi

