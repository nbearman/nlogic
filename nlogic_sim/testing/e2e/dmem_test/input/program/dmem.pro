// This test uses COPMR to jump to a
// hardcoded location and overwriting the
// skipped part of the program with an earlier
// part of the program
COMPR PC
00 00 00 12
7F GPA
7F FLAG // Program should NOT exit here
FF FF
FF FF
FF FF
FF FF
0C RBASE
RMEM GPA
C0 GPC
GPC CC
RMEM GPD
7F FLAG // Program should exit here
