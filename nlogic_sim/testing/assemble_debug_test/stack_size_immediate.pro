// debug assembler should replace ISIZE_FRAME with 00 in the first instance, 10 in the second, and 11 in the third
FRAME_START
10 RBASE
ISIZE_FRAME ROFST
FRAME_END
01 GPA
02 GPE

// non-zero size
FRAME_START
STACK x 08
STACK y 08
ISIZE_FRAME WOFST
FRAME_END

// multiple definitions on one line; odd numbered sizes
FRAME_START STACK x 09 STACK y 07
STACK z 01
ISIZE_FRAME ALUA
FRAME_END

7F FLAG
