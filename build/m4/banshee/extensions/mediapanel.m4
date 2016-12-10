AC_DEFUN([BANSHEE_CHECK_EXTENSION_MEDIAPANEL],
[
    AC_ARG_ENABLE(mediapanel, AC_HELP_STRING([--enable-mediapanel], [Build MediaPanel]), , enable_mediapanel="no")
    AM_CONDITIONAL(ENABLE_MEDIAPANEL, test "x$enable_mediapanel" = "xyes")
])
