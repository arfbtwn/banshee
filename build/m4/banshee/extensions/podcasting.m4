AC_DEFUN([BANSHEE_CHECK_EXTENSION_PODCASTING],
[
    AC_ARG_ENABLE(podcasting, AC_HELP_STRING([--enable-podcasting], [Build Podcasting]),, enable_podcasting="no")
    AM_CONDITIONAL(ENABLE_PODCASTING, test "x$enable_podcasting" = "xyes")
])
