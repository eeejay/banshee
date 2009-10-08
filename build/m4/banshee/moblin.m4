AC_DEFUN([BANSHEE_CHECK_MOBLIN],
[
	AC_ARG_ENABLE(moblin, AC_HELP_STRING([--enable-moblin], [Enable Moblin integration]), , enable_moblin="no")

	if test "x$enable_moblin" = "xyes"; then
		AM_CONDITIONAL(HAVE_MOBLIN, true)
	else
		AM_CONDITIONAL(HAVE_MOBLIN, false)
	fi
])

