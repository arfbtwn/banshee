AC_DEFUN([SHAMROCK_CHECK_NUNIT],
[
	NUNIT_REQUIRED=2.5

	AC_ARG_ENABLE(tests, AS_HELP_STRING([--enable-tests], [Enable NUnit tests]),, enable_tests="no")

	do_tests=no
	PKG_CHECK_MODULES(NUNIT, nunit >= $NUNIT_REQUIRED, have_nunit="yes", have_nunit="no")

	if test "x$have_nunit" = "xyes"; then
		AS_IF([test "x$enable_tests" != "xno"], [
			do_tests=yes
			AC_SUBST(NUNIT_LIBS)
		])
	else
		AS_IF([test "x$enable_tests" = "xyes"], [
			AC_MSG_ERROR([nunit was not found or is not up to date. Please install nunit $NUNIT_REQUIRED or higher.])
		])

	fi
	AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")
])
