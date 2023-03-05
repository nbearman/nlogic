set -e

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

INDENT() { sed 's/^/  -> /'; }
LIST() { sed 's/^/    :: /'; }

if [[ -z "$SIM_EXE" ]] ; then
	echo "[builder_python][Error] You must run 'export SIM_EXE=<simulator .exe path>' first."
	exit 1
fi

if [[ -z "$PY_ASSEMBLER" ]] ; then
	echo "[test_all][Error] You must run 'export PY_ASSEMBLER=<debug assembler .py path>' first."
	exit 1
fi

echo ""

if ! [ -d "$1" ]; then
    echo -e "First argument must be a directory holding a test case."
    exit 1
fi

cd $1
STARTING_DIR=$(pwd)

if [ -d ./BUILD ] ; then
    echo "[WARNING] this will destroy the directory:"
    echo -e "    \033[1;31m$(realpath ./BUILD)\033[0m"
    echo "  Continue?"
    select yn in "No" "Yes"; do
        case $yn in
            Yes ) break;;
            No ) exit 1; break;;
        esac
    done
fi

if [ ! -d ./program ] ; then
    echo ""
    echo "Missing: /program directory."
    echo "Create this required directory and include all boot program .pro files."
    exit 1
fi

SKIP_DISK=false
if [ ! -d ./disk ] ; then
    echo ""
    echo "Missing: /disk directory."
    echo "Continue without creating a virtual disk? "
    select skip_disk in "Yes" "No"; do
        case $skip_disk in
            Yes ) SKIP_DISK=true; break;;
            No ) exit 1; break;;
        esac
    done
fi

rm -rf BUILD
mkdir BUILD
mkdir BUILD/BUILD_ASM
mkdir BUILD/BUILD_DEBUG

if ! $SKIP_DISK ; then
    mkdir BUILD/DISK_ASM
    mkdir BUILD/DISK_DEBUG
    echo "Generating virtual disk..."

    (
        shopt -s nullglob

        for d in disk/*; do
            if [ -d "$d" ]; then
                (
                    echo $d | INDENT
                    cd $d
                    disk_folder_name=${PWD##*/} #crazy thing from stack overflow
                    pro_files=$(find . -type f -name "*.pro")
                    echo "$pro_files" | LIST
                    echo "Running debug assembler" | INDENT
                    python3 $PY_ASSEMBLER -p -o $STARTING_DIR/BUILD/DISK_DEBUG/$disk_folder_name $pro_files > $STARTING_DIR/BUILD/DISK_ASM/$disk_folder_name.asm
                )
            fi
        done

        shopt -u nullglob
    )

    echo "Dividing virtual disk data into blocks" | INDENT
    (
        cd $STARTING_DIR/BUILD/DISK_ASM
        for f in *; do
            number_only=$(echo $f | cut -f 1 -d '.')
            split -b12288 --numeric-suffixes=$number_only -a 5 --additional-suffix .txt $f ""
        done

        # remove undivided assembly output, leaving only blocks
        rm -f *.asm
    )
fi

echo "Building boot program..."
(
    cd program
    echo "Finding pro files" | INDENT
    pro_files=$(find . -type f -name "*.pro")
    echo "$pro_files" | LIST
    echo "Running debug assembler" | INDENT
    python3 $PY_ASSEMBLER -p -o $STARTING_DIR/BUILD/BUILD_DEBUG $pro_files > $STARTING_DIR/BUILD/BUILD_ASM/program.asm
)

# the simulator will fail if we don't use a proper windows path (file does not exist...)
WINDOWS_PROGRAM_PATH=$(wslpath -w $STARTING_DIR/BUILD/BUILD_ASM/program.asm)

if $SKIP_DISK ; then
    $SIM_EXE run $WINDOWS_PROGRAM_PATH "${@:2}"
    exit 0
fi

WINDOWS_VIRTUAL_DISK_PATH=$(wslpath -w $STARTING_DIR/BUILD/DISK_ASM)
$SIM_EXE run $WINDOWS_PROGRAM_PATH disk $WINDOWS_VIRTUAL_DISK_PATH "${@:2}"
