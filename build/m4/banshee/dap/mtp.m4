AC_DEFUN([BANSHEE_CHECK_DAP_MTP],
[
    LIBMTP_REQUIRED=1.1.0

    AC_ARG_ENABLE(mtp, AC_HELP_STRING([--enable-mtp], [Enable MTP DAP support]))

    AS_CASE([$enable_mtp],
    [no],
    [
        AM_CONDITIONAL(ENABLE_MTP, false)
    ],
    [yes],
    [
        PKG_CHECK_MODULES(LIBMTP, libmtp >= $LIBMTP_REQUIRED, has_libmtp="yes")
    ],
    [
        PKG_CHECK_MODULES(LIBMTP, libmtp >= $LIBMTP_REQUIRED, has_libmtp="yes", has_libmtp="no")
        enable_mtp="auto ($has_libmtp)"
    ])

    AS_IF([test "x$has_libmtp" = "xyes"],
    [
        LIBMTP_SO_MAP=$(basename $(find $($PKG_CONFIG --variable=libdir libmtp) -maxdepth 1 -regex '.*libmtp\.so\.[[0-9]][[0-9]]*$' | sort | tail -n 1))
        AC_SUBST(LIBMTP_SO_MAP)

        AM_CONDITIONAL(ENABLE_MTP, true)
        AC_CHECK_SIZEOF(time_t)
        AM_CONDITIONAL(LIBMTP_SIZEOF_TIME_T_64, [test "x$ac_cv_sizeof_time_t" = "x8"])
    ])
])

