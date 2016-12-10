AC_DEFUN([BANSHEE_CHECK_CLIENT_NEREID],
[
    AC_ARG_ENABLE(nereid, AC_HELP_STRING([--enable-nereid], [Enable Nereid Client]),, enable_nereid="yes")
    AM_CONDITIONAL([ENABLE_CLIENT_NEREID], [test "x$enable_nereid" = "xyes"])
])

