AC_DEFUN([BANSHEE_CHECK_GIO_SHARP],
[
	GNOMESHARP_REQUIRED=2.8

    enable_gio=no

	PKG_CHECK_MODULES(GTKSHARP_BEANS,
		gtk-sharp-beans-2.0 >= $GNOMESHARP_REQUIRED,
        enable_gio=yes, enable_gio=no)

	PKG_CHECK_MODULES(GIOSHARP,
		gio-sharp-2.0 >= $GNOMESHARP_REQUIRED,
        enable_gio="$enable_gio", enable_gio=no)

	AM_CONDITIONAL(ENABLE_GIO, test "x$enable_gio" = "xyes")
])

