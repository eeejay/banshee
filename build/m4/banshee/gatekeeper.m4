AC_DEFUN([BANSHEE_CHECK_GATEKEEPER],
[
	AC_ARG_WITH(trunk_readme, AC_HELP_STRING([--with-trunk-readme], 
		[Pass if you understand README.trunk]), 
		with_trunk_readme=yes, with_trunk_readme=no)

	if ! test -e .banshee-developer; then
		if test "x$with_trunk_readme" = "xno"; then
			echo
			echo
			AC_MSG_ERROR([You must read README.trunk before building trunk.])
		fi
	fi
])

