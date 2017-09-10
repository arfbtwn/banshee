AC_DEFUN([BANSHEE_CHECK_EXTENSION_UPNP],
[
    MONOUPNP_REQUIRED=0.1

    MONOUPNP_REQUIRES="mono.ssdp >= $MONOUPNP_REQUIRED \
                       mono.upnp >= $MONOUPNP_REQUIRED \
                       mono.upnp.dcp.mediaserver1 >= MONOUPNP_REQUIRED"

    AC_ARG_ENABLE(upnp, AC_HELP_STRING([--enable-upnp], [Build UPnP Client Extension]))

    AS_CASE([$enable_upnp],
    [no],
    [
        has_mono_upnp=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(MONO_UPNP, $MONOUPNP_REQUIRES, has_mono_upnp=yes)
    ],
    [
        PKG_CHECK_MODULES(MONO_UPNP, $MONOUPNP_REQUIRES, has_mono_upnp=yes, has_mono_upnp=no)
        enable_upnp="auto ($has_mono_upnp)"
    ])

    AS_IF(test "x$has_mono_upnp" = "xyes",
    [
        asms="`$PKG_CONFIG --variable=Libraries mono.ssdp` `$PKG_CONFIG --variable=Libraries mono.upnp` `$PKG_CONFIG --variable=Libraries mono.upnp.dcp.mediaserver1`"
        for asm in $asms; do
            FILENAME=`basename $asm`
            if [[ "`echo $SEENBEFORE | grep $FILENAME`" = "" ]]; then
                MONOUPNP_ASSEMBLIES="$MONOUPNP_ASSEMBLIES $asm"
                [[ -r "$asm.config" ]] && MONOUPNP_ASSEMBLIES="$MONOUPNP_ASSEMBLIES $asm.config"
                [[ -r "$asm.mdb" ]] && MONOUPNP_ASSEMBLIES="$MONOUPNP_ASSEMBLIES $asm.mdb"
                SEENBEFORE="$SEENBEFORE $FILENAME"
            fi
        done
        AC_SUBST(MONOUPNP_ASSEMBLIES)
    ])

    AM_CONDITIONAL(ENABLE_UPNP, test "x$has_mono_upnp" = "xyes")
])
