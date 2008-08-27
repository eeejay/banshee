AC_DEFUN([BANSHEE_CHECK_REMOTE_AUDIO],
[
	MZC_REQUIRED=0.7.3

	AC_ARG_ENABLE(remote_audio, AC_HELP_STRING([--disable-remote-audio], [Disable Remote Audio support]), , enable_remote_audio="no")

	if test "x$enable_remote_audio" = "xyes"; then
		PKG_CHECK_MODULES(MONO_ZEROCONF, mono-zeroconf >= $MZC_REQUIRED)
		AC_SUBST(MONO_ZEROCONF_LIBS)
		AM_CONDITIONAL(REMOTE_AUDIO_ENABLED, true)
	else
		AM_CONDITIONAL(REMOTE_AUDIO_ENABLED, false)
	fi
])
