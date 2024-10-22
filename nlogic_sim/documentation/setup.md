# First time setup

See `nlogic_sim/documentation/Readme.txt` for complete description.

1. Install the language highlight extension
    - Right click > "Install Extension VSIX" on the lastest *.vsix file in nlogic_lang/nlogic-lang

2. Compile the simulator with Visual Studio

3. Run these commands and ensure all tests pass

```sh
cd nlogic_sim/tools/
chmod +x run_test_case.sh
chmod +x test_all.sh
chmod +x go.sh
chmod +x ../bin/Debug/nlogic_sim.exe
export SIM_EXE=$(realpath ../bin/Debug/nlogic_sim.exe)
export PY_ASSEMBLER=$(realpath AssembleDebug.py)
export TEST_SH=$(realpath run_test_case.sh)
./test_all.sh ../testing/e2e/
```
