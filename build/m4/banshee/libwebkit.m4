AC_DEFUN([BANSHEE_CHECK_LIBWEBKIT],
[
	WEBKIT_MIN_VERSION=2.16.6
	SOUP_MIN_VERSION=2.42

	AC_ARG_ENABLE(webkit, AC_HELP_STRING([--disable-webkit], [Disable extensions which require WebKit]), , enable_webkit="yes")

	if test "x$enable_webkit" = "xyes"; then
		have_libwebkit=no
		PKG_CHECK_MODULES(LIBWEBKIT,
			webkit2gtk-4.0 >= $WEBKIT_MIN_VERSION
			libsoup-2.4 >= $SOUP_MIN_VERSION,
			have_libwebkit=yes, have_libwebkit=no)
		AC_SUBST(LIBWEBKIT_LIBS)
		AC_SUBST(LIBWEBKIT_CFLAGS)
		AM_CONDITIONAL(HAVE_LIBWEBKIT, [test x$have_libwebkit = xyes])
	else
		have_libwebkit=no
		have_libsoup_gnome=no
		AM_CONDITIONAL(HAVE_LIBWEBKIT, false)
	fi
])

