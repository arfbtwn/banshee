AC_DEFUN([BANSHEE_CHECK_EXTENSION_MPRIS],
[
    AC_ARG_ENABLE(mpris, AC_HELP_STRING([--enable-mpris], [Build Mpris]),, enable_mpris="no")
    AM_CONDITIONAL(ENABLE_MPRIS, test "x$enable_mpris" = "xyes")
])
