AC_DEFUN([BANSHEE_CHECK_BOO],
[
	AC_ARG_ENABLE([boo], AC_HELP_STRING([--disable-boo], 
		[Do not build with boo support]),
		[
			if test "x$enableval" = "xno"; then
				enable_boo=no
			elif test "x$enableval" = "xyes"; then
				enable_boo=yes
			fi
		], enable_boo=yes
	)

	if test "x$enable_boo" = "xyes"; then
		PKG_CHECK_MODULES(BOO, boo >= 0.8.1)
		AC_SUBST(BOO_LIBS)
		AM_CONDITIONAL(HAVE_BOO, true)
	else
		AM_CONDITIONAL(HAVE_BOO, false)
	fi
])

