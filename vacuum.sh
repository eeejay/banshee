#!/bin/sh

if [ ! -f Makefile ]; then
	exit 1;
fi

make maintainer-clean
rm -f compile INSTALL config.h.in aclocal.m4 ltmain.sh Makefile.in depcomp missing install-sh configure config.sub config.guess mkinstalldirs

