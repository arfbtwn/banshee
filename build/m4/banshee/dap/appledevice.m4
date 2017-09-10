AC_DEFUN([BANSHEE_CHECK_DAP_APPLEDEVICE],
[
    LIBGPODSHARP_REQUIRED=0.1

    AC_ARG_ENABLE(appledevice, AC_HELP_STRING([--enable-appledevice], [Enable Apple device (iPhone, iPod, iPad) DAP support]))

    AS_CASE([$enable_appledevice],
    [no],
    [
        AM_CONDITIONAL(ENABLE_APPLEDEVICE, false)
    ],
    [yes],
    [
        has_libgpod=no
        PKG_CHECK_MODULES(LIBGPODSHARP,
            libgpod-sharp >= $LIBGPODSHARP_REQUIRED,
            has_libgpod=yes, has_libgpod=no)
        if test "x$has_libgpod" = "xno"; then
            AC_MSG_ERROR([libgpod-sharp was not found or is not up to date. Please install libgpod-sharp of at least version $LIBGPODSHARP_REQUIRED, or disable Apple device support by passing --disable-appledevice])
        fi

        AM_CONDITIONAL(ENABLE_APPLEDEVICE, true)
    ],
    [
        PKG_CHECK_MODULES(LIBGPODSHARP, libgpod-sharp >= $LIBGPODSHARP_REQUIRED, has_libgpod=yes, has_libgpod=no)

        AM_CONDITIONAL(ENABLE_APPLEDEVICE, test "x$has_libgpod" = "xyes")
        enable_appledevice="auto ($has_libgpod)"
    ])

    AS_IF([test "x$has_libgpod" = "xyes"],
    [
        asm="`$PKG_CONFIG --variable=Libraries libgpod-sharp`"
        LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm"
        [[ -r "$asm.config" ]] && LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm.config"
        [[ -r "$asm.mdb" ]] && LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm.mdb"
        AC_SUBST(LIBGPODSHARP_ASSEMBLIES)
    ])
])

