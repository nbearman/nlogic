// debug assembler should replace SIZE_FRAME with 00 00 00 00 in the first instance, 00 00 00 10 in the second, and 00 00 01 11 in the third
FRAME_START
10 RBASE
IADF ROFST
SKIP PC
SIZE_FRAME
FRAME_END
01 GPA
02 GPE

// non-zero size
FRAME_START
STACK x 08
STACK y 08
IADF WOFST
SKIP PC
SIZE_FRAME
FRAME_END

// multiple definitions on one line; odd numbered sizes
FRAME_START STACK x 109 STACK y 07
STACK z 01
IADF ALUA
SKIP PC
SIZE_FRAME
FRAME_END

7F FLAG
