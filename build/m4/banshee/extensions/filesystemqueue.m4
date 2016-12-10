AC_DEFUN([BANSHEE_CHECK_EXTENSION_FILESYSTEMQUEUE],
[
    AC_ARG_ENABLE(filesystemqueue, AC_HELP_STRING([--enable-filesystemqueue], [Build FileSystemQueue]),, enable_filesystemqueue="no")
    AM_CONDITIONAL(ENABLE_FILESYSTEMQUEUE, test "x$enable_filesystemqueue" = "xyes")
])
