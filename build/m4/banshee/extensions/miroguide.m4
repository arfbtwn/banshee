AC_DEFUN([BANSHEE_CHECK_EXTENSION_MIROGUIDE],
[
    AC_ARG_ENABLE(miroguide, AC_HELP_STRING([--enable-miroguide], [Build MiroGuide]),, enable_miroguide="no")
    AM_CONDITIONAL(ENABLE_MIROGUIDE, test "x$enable_miroguide" = "xyes")
])
