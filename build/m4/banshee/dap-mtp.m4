AC_DEFUN([BANSHEE_CHECK_DAP_MTP],
[
	LIBMTP_REQUIRED=0.2.0

	AC_ARG_ENABLE(mtp, AC_HELP_STRING([--disable-mtp], [Disable MTP DAP support]), , enable_mtp="yes")
	
	enable_libmtp="${enable_mtp}"

	PKG_CHECK_MODULES(LIBMTP,
		libmtp >= $LIBMTP_REQUIRED,
		enable_libmtp="$enable_libmtp", enable_libmtp=no)

	PKG_CHECK_MODULES(LIBMTP,
		libmtp < 0.3.0,
		enable_libmtp="$enable_libmtp", enable_libmtp=no)
		
	if test "x$enable_mtp" = "xyes" -a "x$enable_libmtp" = "xno"; then
		AC_MSG_ERROR([libmtp was not found or is not up to date. Please install libmtp of at least version $LIBMTP_REQUIRED and less than 0.3.0, or disable MTP support by passing --disable-mtp])
	fi

	if test "x$enable_libmtp" = "xyes"; then
		LIBMTP_SO_MAP=$(basename $(find $($PKG_CONFIG --variable=libdir libmtp) -maxdepth 1 -regex '.*libmtp\.so\.\w+$' | sort | tail -n 1))
		AC_SUBST(LIBMTP_SO_MAP)
	fi

	AM_CONDITIONAL(ENABLE_MTP, test "x$enable_libmtp" = "xyes")
])

