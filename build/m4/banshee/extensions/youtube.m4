AC_DEFUN([BANSHEE_CHECK_EXTENSION_YOUTUBE],
[
    GDATASHARP_REQUIRED_VERSION=1.4

    AC_ARG_ENABLE(youtube, AC_HELP_STRING([--enable-youtube], [Enable YouTube Extension]))

    AS_CASE([$enable_youtube],
    [no],
    [
        has_gdata=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(GDATASHARP, gdata-sharp-youtube >= $GDATASHARP_REQUIRED_VERSION, has_gdata=yes)
    ],
    [
        PKG_CHECK_MODULES(GDATASHARP, gdata-sharp-youtube >= $GDATASHARP_REQUIRED_VERSION, has_gdata=yes, has_gdata=no)
        enable_youtube="auto ($has_gdata)"
    ])

    AM_CONDITIONAL(ENABLE_YOUTUBE, test "x$has_gdata" = "xyes")

    AS_CASE([$has_gdata],
    [no],
    [
        AM_CONDITIONAL(HAVE_GDATASHARP_1_5, false)
    ],
    [
        PKG_CHECK_MODULES(GDATASHARP_1_5_OR_HIGHER, gdata-sharp-youtube >= 1.5,
        [
            AM_CONDITIONAL(HAVE_GDATASHARP_1_5, true)
        ],
        [
            AM_CONDITIONAL(HAVE_GDATASHARP_1_5, false)
        ])
    ])
])
