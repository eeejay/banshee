AC_DEFUN([BANSHEE_CHECK_OSX],
[
    IGEMACINTEGRATIONSHARP_REQUIRED=0.6

	AC_ARG_ENABLE(osx, AC_HELP_STRING([--enable-osx], [Enable OSX support]), enable_osx=$enableval, enable_osx="no")

	if test "x$enable_osx" = "xyes"; then
        dnl FIXME: detect osx
	    have_osx="yes"

        PKG_CHECK_MODULES(IGEMACINTEGRATIONSHARP, 
	        ige-mac-integration-sharp >= $IGEMACINTEGRATIONSHARP_REQUIRED,
	        have_igemacintegrationsharp=yes, have_igemacintegrationsharp=no)
	
        if test "x$have_igemacintegrationsharp" = "xno"; then
	        AC_MSG_ERROR([ige-mac-integration-sharp was not found or is not up to date. Please install ige-mac-integration-sharp of at least version $IGEMACINTEGRATIONSHARP_REQUIRED])
	    fi
	    AC_SUBST(IGEMACINTEGRATIONSHARP_LIBS)
	fi

	AM_CONDITIONAL(ENABLE_OSX, test "x$have_osx" = "xyes")
])
