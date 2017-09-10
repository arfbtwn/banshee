AC_DEFUN([BANSHEE_CHECK_WINDOWS],
[
    AS_IF([test "x${host_os%${host_os#???????}}" = "xWindows"], [enable_windows="yes"], [enable_windows="no"])

    AM_CONDITIONAL([PLATFORM_WINDOWS], [test "x$enable_windows" = "xyes"])
])
