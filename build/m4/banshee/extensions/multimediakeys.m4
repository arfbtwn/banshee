AC_DEFUN([BANSHEE_CHECK_EXTENSION_MULTIMEDIAKEYS],
[
    AC_ARG_ENABLE(multimediakeys, AC_HELP_STRING([--enable-multimediakeys], [Build MultimediaKeys]),, enable_multimediakeys="no")
    AM_CONDITIONAL(ENABLE_MULTIMEDIAKEYS, test "x$enable_multimediakeys" = "xyes")
])
