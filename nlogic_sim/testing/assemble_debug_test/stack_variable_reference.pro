// the debug assembler should replace ISTACK_var1 with 00, ISTACK_var2 with 08, and ISTACK_var3 with 18

// 01 83 02 84 03 85 00 8B 00 8C 00 8E 08 8F 18 91 91 95 7F 80

FRAME_START
01 ALUM
02 ALUA
03 ALUB
STACK var1 08

00 RBASE
ISTACK_var1 ROFST
00 WBASE
// stack variable reference before declaration is ok
ISTACK_var2 WOFST

STACK var2 10
STACK var3 04

ISTACK_var3 GPA

FRAME_END

GPA GPE
7F FLAG
