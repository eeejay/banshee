AC_DEFUN([BANSHEE_CHECK_DAP_MTP],
[
	LIBGPHOTO2SHARP_REQUIRED=2.3.0

	AC_ARG_ENABLE(mtp, AC_HELP_STRING([--enable-mtp], 
		[Enable MTP DAP support]), , enable_mtp="no")
	
	enable_mtp_dap="${enable_mtp}"

	PKG_CHECK_MODULES(LIBGPHOTO2SHARP,
		libgphoto2-sharp >= $LIBGPHOTO2SHARP_REQUIRED,
		enable_mtp_dap="$enable_mtp_dap", enable_mtp_dap=no)
		
	if test "x$enable_mtp" != "xno" -a "x$enable_mtp_dap" = "xno"; then
		AC_MSG_ERROR([libgphoto2-sharp was not found or is not up to date. Please install libgphoto2-sharp of at least version $LIBGPHOTO2SHARP_REQUIRED, or disable MTP support by not passing --enable-mtp])
	fi

	AC_MSG_RESULT([$enable_mtp_dap])
	
	if test "x$enable_mtp_dap" = "xyes"; then
		LIBGPHOTO2SHARP_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries libgphoto2-sharp`"
		AC_SUBST(LIBGPHOTO2SHARP_ASSEMBLIES)
		AC_SUBST(LIBGPHOTO2SHARP_LIBS)
	fi
	
	AM_CONDITIONAL(ENABLE_MTP, test "x$enable_mtp_dap" = "xyes")
])

