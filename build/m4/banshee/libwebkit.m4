AC_DEFUN([BANSHEE_CHECK_LIBWEBKIT],
[
    WEBKIT_MIN_VERSION=1.2.2
    SOUP_MIN_VERSION=2.42

    WEBKIT_REQUIRES="webkitgtk-3.0 >= $WEBKIT_MIN_VERSION \
                     libsoup-2.4 >= $SOUP_MIN_VERSION"

    AC_ARG_WITH(webkit, AC_HELP_STRING([--with-webkit], [Disable extensions which require WebKit]))

    AS_CASE([$with_webkit],
    [no],
    [
        have_libwebkit=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(LIBWEBKIT, $WEBKIT_REQUIRES, have_libwebkit=yes)
    ],
    [
        PKG_CHECK_MODULES(LIBWEBKIT, $WEBKIT_REQUIRES, have_libwebkit=yes, have_libwebkit=no)

        with_webkit="auto ($have_libwebkit)"
    ])

    AM_CONDITIONAL(HAVE_LIBWEBKIT, [test x$have_libwebkit = xyes])
])

