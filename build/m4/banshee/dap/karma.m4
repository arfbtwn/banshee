AC_DEFUN([BANSHEE_CHECK_DAP_KARMA],
[
    KARMASHARP_REQUIRED=0.0.5

    AC_ARG_ENABLE(karma, AC_HELP_STRING([--enable-karma], [Enable Rio Karma DAP support]))

    AS_CASE([$enable_karma],
    [no],
    [
        AM_CONDITIONAL(ENABLE_KARMA, false)
    ],
    [yes],
    [
        PKG_CHECK_MODULES(KARMASHARP, karma-sharp >= $KARMASHARP_REQUIRED, has_karmasharp=yes)
    ],
    [
        PKG_CHECK_MODULES(KARMASHARP, karma-sharp >= $KARMASHARP_REQUIRED, has_karmasharp=yes, has_karmasharp=no)
        enable_karma="auto ($has_karmasharp)"
    ])

    AS_IF([test "x$has_karmasharp" = "xyes"],
    [
        asms="`$PKG_CONFIG --variable=Libraries karma-sharp`"
        for asm in $asms; do
            KARMASHARP_ASSEMBLIES="$KARMASHARP_ASSEMBLIES $asm"
            [[ -r "$asm.config" ]] && KARMASHARP_ASSEMBLIES="$KARMASHARP_ASSEMBLIES $asm.config"
            [[ -r "$asm.mdb" ]] && KARMASHARP_ASSEMBLIES="$KARMASHARP_ASSEMBLIES $asm.mdb"
        done
        AC_SUBST(KARMASHARP_ASSEMBLIES)
        AC_SUBST(KARMASHARP_LIBS)
    ])

    AM_CONDITIONAL(ENABLE_KARMA, test "x$has_karmasharp" = "xyes")
])

