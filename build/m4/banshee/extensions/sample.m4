AC_DEFUN([BANSHEE_CHECK_EXTENSION_SAMPLE],
[
    AC_ARG_ENABLE(sample, AC_HELP_STRING([--enable-sample], [Build Sample]),, enable_sample="no")
    AM_CONDITIONAL(ENABLE_SAMPLE, test "x$enable_sample" = "xyes")
])
