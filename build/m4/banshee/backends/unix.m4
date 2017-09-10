AC_DEFUN([BANSHEE_CHECK_UNIX],
[
    AS_IF([test "x${host_os%${host_os#?????????}}" = "xlinux-gnu"], [enable_unix="yes"], [enable_unix="no"])

    AM_CONDITIONAL([PLATFORM_UNIX], [test "x$enable_unix" = "xyes"])
])
