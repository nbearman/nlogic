set -e

if [[ -z "$PY_ASSEMBLER" ]] ; then
	echo "[test_all][Error] You must run 'export PY_ASSEMBLER=<debug assembler .py path>' first."
	exit 1
fi

if [[ -z "$SIM_EXE" ]] ; then
	echo "[test_all][Error] You must run 'export SIM_EXE=<simulator .exe path>' first."
	exit 1
fi

if [[ -z "$TEST_SH" ]] ; then
	echo "[test_all][Error] You must run 'export TEST_SH=<run_test_case.sh path>' first."
	exit 1
fi

echo ""

if ! [ -d "$1" ]; then
    echo "First argument must be a directory (holding test case directories)."
    exit 1
fi

FAIL=false
cd $1
echo "Running test cases in $1"
for d in ./*; do
    if [ -d "$d" ]; then
        echo -n "  $d  "
        $TEST_SH $d &>/dev/null && echo -ne "\033[1;32m✓\033[0m" || {
            echo -ne "\033[1;31mX\033[0m"
            FAIL=true
        }
        echo ""
    fi
done

if $FAIL; then
    echo -e "\033[1;31mTest run failed.\033[0m"
    echo "Use run_test_case.sh on individual failing cases to see details."
    exit 1
    else
    echo -e "\033[1;32mAll tests passed.\033[0m"
fi
