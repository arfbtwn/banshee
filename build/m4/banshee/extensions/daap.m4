AC_DEFUN([BANSHEE_CHECK_EXTENSION_DAAP],
[
    AC_ARG_ENABLE(daap, AC_HELP_STRING([--enable-daap], [Enable DAAP support]),, enable_daap="$has_daap")

    AS_CASE([$enable_daap,$has_daap],
    [yes,no],
    [
        AC_MSG_ERROR(Please build --with-daap)
    ])

    AM_CONDITIONAL(ENABLE_DAAP, test "x$enable_daap" = "xyes")
])
