AC_DEFUN([BANSHEE_CHECK_CLIENT_HALIE],
[
    AC_ARG_ENABLE(halie, AC_HELP_STRING([--enable-halie], [Enable Halie Client]),, enable_halie="yes")
    AM_CONDITIONAL([ENABLE_CLIENT_HALIE], [test "x$enable_halie" = "xyes"])
])


