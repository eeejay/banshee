dnl Test compiler for BXC #79698
AC_DEFUN([BANSHEE_VERIFY_GMCS],
[
	AC_MSG_CHECKING([for broken mcs 1.1.18 compiler])
	if $MCS $srcdir/build/mcs-test-79698.cs &>/dev/null; then
    	AC_MSG_RESULT([compiler okay])
	else
		AC_MSG_RESULT([broken, using internal gmcs])
		MCS="$MONO \${top_srcdir}/build/gmcs.exe"
		chmod +x build/gmcs.exe
	fi
])

