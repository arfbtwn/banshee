AC_DEFUN([BANSHEE_CHECK_CLIENT_BEROE],
[
    AC_ARG_ENABLE(beroe, AC_HELP_STRING([--enable-beroe], [Enable Beroe Client]),, enable_beroe="no")
    AM_CONDITIONAL([ENABLE_CLIENT_BEROE], [test "x$enable_beroe" = "xyes"])
])

