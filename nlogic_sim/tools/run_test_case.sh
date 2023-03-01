set -e

if [[ -z "$PY_ASSEMBLER" ]] ; then
	echo "[run_test_case][Error] You must run 'export PY_ASSEMBLER=<debug assembler .py path>' first."
	exit 1
fi

if [[ -z "$SIM_EXE" ]] ; then
	echo "[run_test_case][Error] You must run 'export SIM_EXE=<simulator .exe path>' first."
	exit 1
fi

INDENT() { sed 's/^/  -> /'; }
LIST() { sed 's/^/    :: /'; }

TEST_CASE_FAILED() {
    echo -e "\033[1;31mTest failed!\033[0m"
    exit 1
}

trap TEST_CASE_FAILED ERR

GENERATING=false

if ! [ -d "$1" ]; then
    echo -e "\nFirst argument must be a directory holding a test case."
    exit 1
fi

cd $1

echo ""
if [ "$2" = "--generate" ]
    then
        echo "Generating test $1"
        GENERATING=true
        if [ -d "expected" ]
            then
                echo "Cannot generate test case; expected directory already exists."
                echo "    To run the test case instead, omit the --generate argument."
                echo "    To regenerate the test case, remove the expected directory."
                exit 1
        fi
    else
        echo "Running test case $1"
fi

echo "[1] Building virtual disk"
STARTING_DIR=$(pwd)
rm -rf output
mkdir output
mkdir output/DISK_ASM
mkdir output/BUILD_ASM
mkdir output/BUILD_DEBUG
mkdir output/LOGS

# make it so *s that don't match anything become empty lists, rather than the string "*"
shopt -s nullglob

for d in input/disk/*; do
    cd $STARTING_DIR
    if [ -d "$d" ]; then
        echo $d | INDENT
        cd $d
        disk_folder_name=${PWD##*/} #crazy thing from stack overflow
        pro_files=$(find . -type f -name "*.pro")
        echo "$pro_files" | LIST
        echo "Running debug assembler" | INDENT
        python3 $PY_ASSEMBLER -p -o $STARTING_DIR/output/DISK_DEBUG/$disk_folder_name $pro_files > $STARTING_DIR/output/DISK_ASM/$disk_folder_name.asm
    fi
done

echo "Dividing virtual disk data into blocks" | INDENT
cd $STARTING_DIR/output/DISK_ASM
for f in *; do
    number_only=$(echo $f | cut -f 1 -d '.')
    split -b12288 --numeric-suffixes=$number_only -a 5 --additional-suffix .txt $f ""
done

# remove undivided assembly output, leaving only blocks
rm -f *.asm

# turn off because this interferes with the simulator outputting logs for some reason
shopt -u nullglob

echo "[2] Building boot program"
cd $STARTING_DIR
cd input/program
echo "Finding pro files" | INDENT
pro_files=$(find . -type f -name "*.pro")
echo "$pro_files" | LIST
echo "Running debug assembler" | INDENT
python3 $PY_ASSEMBLER -p -o $STARTING_DIR/output/BUILD_DEBUG $pro_files > $STARTING_DIR/output/BUILD_ASM/program.asm

echo "[3] Running simulator"
# the simulator will fail if we don't use a proper windows path (file does not exist...)
WINDOWS_PROGRAM_PATH=$(wslpath -w $STARTING_DIR/output/BUILD_ASM/program.asm)
WINDOWS_OUTPUT_PATH=$(wslpath -w $STARTING_DIR/output/LOGS/)\\cpu_log.txt
$SIM_EXE run $WINDOWS_PROGRAM_PATH -l $WINDOWS_OUTPUT_PATH -t
echo "Done" | INDENT

cd $STARTING_DIR

if $GENERATING
    then
        # create a test case by saving the actual output to use as
        # the expected output in future runs
        echo "[4] Generating expected output"
        cp -r output expected
        echo -e "\033[1;34mGenerated test case.\033[0m"
        exit 0
fi


echo "[4] Diffing expected vs. output"
set -o pipefail
echo "Diff" | INDENT
diff --brief --recursive expected/ output/ | LIST

# Test case has has passed!
echo -e "\033[1;32mTest passed.\033[0m"
