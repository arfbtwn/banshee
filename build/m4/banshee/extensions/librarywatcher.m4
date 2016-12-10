AC_DEFUN([BANSHEE_CHECK_EXTENSION_LIBRARYWATCHER],
[
    AC_ARG_ENABLE(librarywatcher, AC_HELP_STRING([--enable-librarywatcher], [Build LibraryWatcher]),, enable_librarywatcher="no")
    AM_CONDITIONAL(ENABLE_LIBRARYWATCHER, test "x$enable_librarywatcher" = "xyes")
])
