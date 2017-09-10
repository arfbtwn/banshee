AC_DEFUN([BANSHEE_CHECK_EXTENSION_WIKIPEDIA],
[
    AC_ARG_ENABLE(wikipedia, AC_HELP_STRING([--enable-wikipedia], [Build Wikipedia]),, enable_wikipedia="$have_libwebkit")

    AS_CASE([$enable_wikipedia,$have_libwebkit],
    [yes,no],
    [
        AC_MSG_ERROR(Build with --with-libwebkit)
    ])

    AM_CONDITIONAL(ENABLE_WIKIPEDIA, test "x$enable_wikipedia" = "xyes")
])
