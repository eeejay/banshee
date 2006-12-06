AC_DEFUN([BANSHEE_CHECK_HELIX_ENGINE],
[
	AC_ARG_ENABLE(helix, AC_HELP_STRING([--enable-helix], 
		[Enable Helix/RealPlayer engine]), , enable_helix="yes")

	if test "x$enable_helix" = "xyes"; then
		PKG_CHECK_MODULES(HELIX_REMOTE, helix-dbus-server >= 0.3, 
		enable_helix="yes", enable_helix="no")
	fi

	AM_CONDITIONAL(ENABLE_HELIX, test "x$enable_helix" = "xyes")
])

