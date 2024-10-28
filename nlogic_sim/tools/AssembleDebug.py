import re
import argparse
import os
from pathlib import Path
from abc import ABC, abstractmethod

name_to_byte = {
    "IMM": "00",
    "FLAG": "80",
    "EXE": "81",
    "PC": "82",
    "ALUM": "83",
    "ALUA": "84",
    "ALUB": "85",
    "ALUR": "86",
    "FPUM": "87",
    "FPUA": "88",
    "FPUB": "89",
    "FPUR": "8a",
    "RBASE": "8b",
    "ROFST": "8c",
    "RMEM": "8d",
    "WBASE": "8e",
    "WOFST": "8f",
    "WMEM": "90",
    "GPA": "91",
    "GPB": "92",
    "GPC": "93",
    "GPD": "94",
    "GPE": "95",
    "GPF": "96",
    "GPG": "97",
    "GPH": "98",
    "COMPA": "99",
    "COMPB": "9a",
    "COMPR": "9b",
    "IADN": "9c",
    "IADF": "9d",
    "LINK": "9e",
    "SKIP": "9f",
    "RTRN": "a0",
    "DMEM": "c0",
}

alu_op_to_byte = {
    "ANOP": "00",
    "AADD": "01",
    "AMUL": "02",
    "ASUB": "03",
    "ADIV": "04",
    "ALSFT": "05",
    "ARSFT": "06",
    "AOR": "07",
    "AAND": "08",
    "AXOR": "09",
    "ANAND": "0a",
    "ANOR": "0b",
}

fpu_op_to_byte = {
    "FNOP": "00",
    "FADD": "01",
    "FMUL": "02",
    "FSUB": "03",
    "FDIV": "04",
}

def address_to_bytes(addr:int) -> str:
    """
    Return string of 4 bytes separated by spaces
    "00 AA BB CC"
    """
    if addr is None:
        return "-- -- -- --"
    num = f"{addr:0>8X}"
    if len(num) > 8:
        raise Exception("address longer than 32 bits")
    return f"{num[0:2]} {num[2:4]} {num[4:6]} {num[6:8]}"

def number_to_byte(num:int) -> str:
    """
    Return a string of 1 byte (exactly 2 characters)
    "AA"
    """
    s = f"{num:0>2X}"
    if len(s) > 2:
        raise Exception("number is longer than 8 bits")
    return s

def replace_dmem(dmem:str) -> str:
    """
    Return the single-byte instruction string corresponding to
    the given DMEM macro string
    "DMEM00" -> "C0"
    """
    if not "dmem" in dmem.lower():
        raise Exception("replace_dmem called on string without dmem")
    num = int(dmem.lower().replace("dmem", ""), base=16) + 0xC0
    if num > 0xFF:
        # DMEM can only target between 00 and 3F
        # 3F + C0 == FF; do not allow instructions above FF
        raise Exception("illegal DMEM instruction")
    return f"{num:0>2X}"


class FileInfo:
    def __init__(self, filename, line_number):
        self.filename = filename
        self.line = line_number

    def get_local_label_prefix(self):
        return f"__file_{self.filename}__"

class StackVariableDeclaration:
    def __init__(self, stack_variable_identifier, stack_variable_size):
        if type(stack_variable_identifier) is not StackVariableIdentifier:
            raise Exception(f"cannot create StackVariableDeclaration: {type(stack_variable_identifier)} is not StackVariableIdentifier")
        if type(stack_variable_size) is not StackVariableSize:
            raise Exception(f"cannot create StackVariableDeclaration: {type(stack_variable_size)} is not StackVariableSize")
        self.size = stack_variable_size.num_bytes
        self.name = stack_variable_identifier.name

class ConstVariableDeclaration:
    def __init__(self, const_variable_identifier: "ConstVariableIdentifier", const_variable_value: "ConstVariableValue"):
        if type(const_variable_identifier) is not ConstVariableIdentifier:
            raise Exception(f"cannot create ConstVariableDeclaration: {type(const_variable_identifier)} is not ConstVariableIdentifier")
        if type(const_variable_value) is not ConstVariableValue:
            raise Exception(f"cannot create ConstVariableDeclaration: {type(const_variable_value)} is not ConstVariableValue")
        self.value = const_variable_value.value
        self.name = const_variable_identifier.name

class Identifier:
    def __init__(self, name):
        stack_var_id_match = re.match("[a-zA-Z]+\w*$", name)
        if not stack_var_id_match:
            raise Exception(f"illegal {type(self).__name__}: {name} must start with a letter and contain only numbers, letters, and underscores")
        self.name = name

class StackVariableIdentifier(Identifier):
    pass

class ConstVariableIdentifier(Identifier):
    pass

class StackVariableSize:
    def __init__(self, num_bytes):
        # if the size is not a valid number, this will cause an exception
        self.num_bytes = int(num_bytes.lower(), base=16)

class ConstVariableValue:
    def __init__(self, value):
        # if the value is not a valid number, this will cause an exception
        self.value = int(value.lower(), base=16)

class VariableReference(ABC):
    @abstractmethod
    def get_reference_prefix(self):
        pass

    @abstractmethod
    def get_reference_prefix_help_string(self):
        pass

    def __init__(self, name):
        reference_prefix = self.get_reference_prefix()
        if not re.match(reference_prefix, name):
            raise Exception(f"{type(self).__name__} created for identifier without {self.get_reference_prefix_help_string()} prefix: {name}")
        # remove the prefix from the reference macro to get just the variable name
        identifier = re.sub(reference_prefix, "", name)
        self.name = identifier

class StackVariableReferenceImmediate(VariableReference):
    def get_reference_prefix(self):
        return "(?i)^istack_"

    def get_reference_prefix_help_string(self):
        return "ISTACK_"

class StackVariableReferenceFull(VariableReference):
    def get_reference_prefix(self):
        return "(?i)^stack_"

    def get_reference_prefix_help_string(self):
        return "STACK_"

class ConstVariableReferenceImmediate(VariableReference):
    def get_reference_prefix(self):
        return "(?i)^iconst_"

    def get_reference_prefix_help_string(self):
        return "ICONST_"

class ConstVariableReferenceFull(VariableReference):
    def get_reference_prefix(self):
        return "(?i)^const_"

    def get_reference_prefix_help_string(self):
        return "CONST_"

class StackFrameStart:
    def __init__(self):
        pass

class StackFrameEnd:
    def __init__(self):
        pass

class StackFrameSizeImmediate:
    def __init__(self):
        pass

class StackFrameSizeFull:
    def __init__(self):
        pass

class LabelReference:
    def __init__(self, label, linked=False, file_info:FileInfo=None):
        if not linked:
            if not file_info:
                raise Exception("LabelReference created for local label with no file info")
            # change label ":label" to ":__prefix__label"
            self.label = ":" + file_info.get_local_label_prefix() + label[1:]
        else:
            self.label = label

class LabelDefinition:
    def __init__(self, label, linked=False, file_info:FileInfo=None):
        if not linked:
            # local labels need to be prefixed with file name to prevent collisions across files
            if not file_info:
                raise Exception("LabelDefinition created for local label with no file info")
            # change label "@label" to "@__prefix__label"
            self.label = "@" + file_info.get_local_label_prefix() + label[1:]
        else:
            self.label = label

        # calculate the name that will be used to look up this label
        # label references that match self.lookup refer to this label
        self.lookup = re.sub("^@", ":", re.sub("^@@", "::", self.label))

        # target VA of this label; not known until after first pass is completed
        self.va = None

class Literal:
    """
    A byte literal that appears in the source
    """
    def __init__(self, literal):
        self.literal = literal

class Fill:
    def __init__(self, fill: str):
        # convert fill instruction to int of VA it targets,
        # "FILLFF" -> 127
        self.target = int(fill.lower().replace("fill", ""), base=16)

class Instruction:
    """
    Processor location that appears in source in traditional form
    E.g.: "WMEM", "FLAG", "GPA", "PC"
    """
    def __init__(self, inst: str):
        # original string of instruction
        self.inst = inst

        # string of byte it translates to
        self.byte = name_to_byte.get(inst)
        if not self.byte:
            self.byte = alu_op_to_byte.get(inst)
        if not self.byte:
            self.byte = fpu_op_to_byte.get(inst)
        if not self.byte:
            # no known processor location or op macro
            raise Exception(f"cannot parse instruction {inst}")

class Line:
    """
    Object representing an original line in a source file
    """
    def parse(self, line):
        """
        return the intermediate representation of this line, a list
        of code generating items (Fill, Instruction, Literal, LabelReference, etc.)
        """
        # trim the line to only the non-commented part
        comment_start = line.find("//")
        if comment_start >= 0:
            line = line[:comment_start]

        # list of intermediate code item objects
        result = []

        # stack variable declarations are multi-word macros; some statefulness is needed to mark the part of the macro that we expect next
        next_stack_var_word = None
        next_const_var_word = None

        for word in line.split():
            # for each word on the line, construct the matching code item
            # the order of pattern detection here determines code generation precedence

            # try to complete in-progress stack variable declaration
            if next_stack_var_word == "IDENTIFIER":
                result.append(StackVariableIdentifier(word))
                # the next word on the line must be a valid stack variable size
                next_stack_var_word = "SIZE"
                continue

            if next_stack_var_word == "SIZE":
                result.append(StackVariableSize(word.lower()))

                # pop the parts of the declaration off the code item list to combine them into one item
                # this is possible because all parts of the declaration must be consecutive and in order
                variable_size = result.pop()
                variable_identifier = result.pop()

                # push the combined declaration onto the result
                result.append(StackVariableDeclaration(variable_identifier, variable_size))

                # stack variable declaration macro now complete; no expectation for next word
                next_stack_var_word = ""
                continue

            # we are in the middle of a stack variable declaration, but the next word was not caught by the above cases
            # it's probably an error with the code above
            if next_stack_var_word:
                raise Exception(f"invalid stack variable declaration; expected '{next_stack_var_word.lower()}' but no parsing for that part exists")

            # do the same process as stack variable declarations for const variables
            # try to complete in-progress const variable declaration
            if next_const_var_word == "IDENTIFIER":
                result.append(ConstVariableIdentifier(word))
                next_const_var_word = "VALUE"
                continue

            if next_const_var_word == "VALUE":
                result.append(ConstVariableValue(word.lower()))
                variable_value = result.pop()
                variable_identifier = result.pop()
                result.append(ConstVariableDeclaration(variable_identifier, variable_value))
                # macro parsing complete; clear the expectation for the next word -- it can be anything
                next_const_var_word = ""
                continue

            if next_const_var_word:
                raise Exception(f"invalid const variable declaration; expected '{next_const_var_word.lower()}' but no parsing for that part exists")

            # find stack variable declarations
            stack_var_def_match = re.match("stack$", word.lower())
            if stack_var_def_match:
                # do not create the StackVariableDeclaration; that will be created after the full macro is parsed
                next_stack_var_word = "IDENTIFIER"
                continue

            # find const variable declarations
            const_var_def_match = re.match("const$", word.lower())
            if const_var_def_match:
                # do not create the ConstVariableDeclaration; that will be created after the full macro is parsed
                next_const_var_word = "IDENTIFIER"
                continue


            # identify label definitions and references
            global_label_def_match = re.match("^@@.+", word)
            if global_label_def_match:
                result.append(LabelDefinition(word, linked=True))
                continue

            local_label_def_match = re.match("^@.+", word)
            if local_label_def_match:
                result.append(LabelDefinition(word, linked=False, file_info=self.file_info))
                continue

            global_label_use_match = re.match("^::.+", word)
            if global_label_use_match:
                result.append(LabelReference(word, linked=True))
                continue

            local_label_use_match = re.match("^:.+", word)
            if local_label_use_match:
                result.append(LabelReference(word, linked=False, file_info=self.file_info))
                continue

            # find DMEM macros
            dmem_match = re.match("(dmem)[0-9a-f][0-9a-f]$", word.lower())
            if dmem_match:
                result.append(Literal(replace_dmem(dmem_match.group())))
                continue

            # find FILL macros
            fill_match = re.match("(fill)[0-9a-f]+$", word.lower())
            if fill_match:
                result.append(Fill(word))
                continue

            # find BREAKPOINT macros
            breakpoint_match = re.match("(break)$", word.lower())
            if breakpoint_match:
                result.append(Literal("7b"))
                result.append(Literal("7b"))
                continue

            # find stack frame markers
            stack_frame_start_match = re.match("^frame_start$", word.lower())
            if stack_frame_start_match:
                result.append(StackFrameStart())
                continue

            stack_frame_end_match = re.match("^frame_end$", word.lower())
            if stack_frame_end_match:
                result.append(StackFrameEnd())
                continue

            # find stack variable references
            stack_variable_imm_match = re.match("^(ISTACK|istack)_([a-zA-Z]+\w*$)", word)
            if stack_variable_imm_match:
                result.append(StackVariableReferenceImmediate(word))
                continue

            stack_variable_full_match = re.match("^(STACK|stack)_([a-zA-Z]+\w*$)", word)
            if stack_variable_full_match:
                result.append(StackVariableReferenceFull(word))
                continue

            # find const variable references
            const_variable_imm_match = re.match("^(ICONST|iconst)_([a-zA-Z]+\w*$)", word)
            if const_variable_imm_match:
                result.append(ConstVariableReferenceImmediate(word))
                continue

            const_variable_full_match = re.match("^(CONST|const)_([a-zA-Z]+\w*$)", word)
            if const_variable_full_match:
                result.append(ConstVariableReferenceFull(word))
                continue

            # find stack size references
            stack_size_imm_match = re.match("^isize_frame$", word.lower())
            if stack_size_imm_match:
                result.append(StackFrameSizeImmediate())
                continue

            stack_size_imm_match = re.match("^size_frame$", word.lower())
            if stack_size_imm_match:
                result.append(StackFrameSizeFull())
                continue

            # identify byte literals
            literal_match = re.match("^[0-9a-f][0-9a-f]$", word.lower())
            if literal_match:
                result.append(Literal(word))
                continue

            # otherwise, this might be an instruction, or it might be a syntax error
            # try to parse the word as an instruction, this will raise exception if
            # unrecognizable token
            result.append(Instruction(word))

        # stack variable declarations must be contained to one line
        # we've reached the end of the line, so if we are looking for the next
        # part of a stack variable declaration, this is an error
        if next_stack_var_word:
            raise Exception(f"line ended in the middle of a stack variable declaration; expected {next_stack_var_word.lower()}")
        return result

    def __init__(self, filename, line_num, line):
        self.file_info = FileInfo(filename, line_num)
        # string contents of line as it appeared in the source
        self.original_line = line
        # final VA in output assembly; not known until after first pass
        self.va = None
        # intermediate code, list of code generating item objects
        # (e.g.: Fill, Literal, LabelDefinition)
        self.intermediate = self.parse(line)

class Program:
    def __init__(self, filenames, print_final=False):
        """
        Given a list of file names, create the combined output program assembly
        and a mapping of filename (str) -> annotated debug source file lines (list(str))
        """

        # read all files and convert to line objects
        self.annotated_files = {}
        all_lines = []
        for name in filenames:
            file = open(name)
            # create entry in debug output mapping, to be populated after assembly
            self.annotated_files[name] = []

            # create a Line object with original file information
            line_num = 0
            for line in file:
                # some files start with byte order markers that need to be removed, apparently
                line = bytes(line, "utf-8").decode("utf-8-sig")
                all_lines.append(Line(name, line_num, line))
            file.close()

        # identifier -> offset to use when replacing StackVariableReference code items
        stack_variables_to_offsets = {}
        within_stack_frame = False # true if currently in a stack frame context
        current_stack_frame_size = 0

        # identifier -> value to use when replace ConstVariableReference code items
        const_variables_to_values = {}

        # labels -> VAs to use when resolving labels after first assembly pass
        label_mapping = {}

        # final VA counter
        pc = 0

        # list of strings, output code after first pass
        # can contain literal bytes ("0A", "7F") or label references
        code = []

        # generate first pass code from all lines and populate label definition mapping
        # first pass generates all code except label references, which are left in place since
        # we don't know their final target VA until after the first pass
        # (label target VAs are calculated during first pass, in order, from line 0 to line N)
        for line_index in range(len(all_lines)):
            l = all_lines[line_index]
            # Line's intermediate holds a list of code generating items
            for item_index in range(len(l.intermediate)):
                item = l.intermediate[item_index]
                t = type(item)

                # record the VA of the first piece of data on this line
                # after the first piece of data, the line will have a VA, so don't overwrite it
                # ignore label definitions and some stack macros; they are the only instructions
                # which do not correspond to any data in the output assembly (and so have no VA)
                if t not in [
                    LabelDefinition,
                    StackVariableDeclaration,
                    ConstVariableDeclaration,
                    StackFrameStart,
                    StackFrameEnd
                ]:
                    if l.va is None:
                        l.va = pc

                # for each type of code generating item, we take different actions
                # and emit different code
                # increment PC for each emitted byte so that our VA counter is correct
                # the VA counter is used to resolve label targets as we encounter definitions
                if t is Literal:
                    code.append(item.literal)
                    pc += 1

                elif t is Instruction:
                    code.append(item.byte)
                    pc += 1

                elif t is Fill:
                    if pc > item.target:
                        # FILLXX places the next instruction at address XX
                        # We can't place the next instruction there if we are already past it
                        raise Exception(f"cannot fill; already past target {item.target} (current: {pc})")
                    while pc < item.target:
                        # push a byte and increment PC for each filled instruction until
                        # we reach the target
                        code.append("00")
                        pc += 1

                elif t is LabelReference:
                    # label references refer to 32 bit addresses, so increment counter by 4
                    # push the label reference as is, and we will convert it to 4 bytes in the
                    # second pass, after all label definitions are resovled
                    code.append(item.label)
                    pc += 4

                elif t is LabelDefinition:
                    # no code is generated for label definitions, so don't increment PC
                    # resolve the target of this label as the current VA
                    if label_mapping.get(item.lookup):
                        raise Exception(f"Duplicate label defined: {item.label}")
                    label_mapping[item.lookup] = pc

                elif t is StackFrameStart:
                    # check that we are not already in a stack frame
                    if within_stack_frame:
                        raise Exception("FRAME_START cannot be used inside stack frame context; use FRAME_END first")
                    within_stack_frame = True
                    # calculate the size of the stack frame by finding all the variable declarations before FRAME_END
                    size_accumulator = 0
                    frame_terminated = False
                    # look forward through the next code items to find the end of the frame
                    # accumulate the frame size and calculate variable offsets on the way
                    # start by looking through the rest of the code on this line
                    for frame_item in l.intermediate[item_index + 1:]:
                        item_type = type(frame_item)
                        if item_type is StackVariableDeclaration:
                            offset = size_accumulator
                            stack_variables_to_offsets[frame_item.name] = offset
                            size_accumulator += frame_item.size
                        elif item_type is StackFrameEnd:
                            current_stack_frame_size = size_accumulator
                            frame_terminated = True
                            break
                    # if the frame context is still open, go through upcoming lines
                    if not frame_terminated:
                        for future_line in all_lines[line_index + 1:]:
                            for future_item in future_line.intermediate:
                                item_type = type(future_item)
                                if item_type is StackVariableDeclaration:
                                    offset = size_accumulator
                                    stack_variables_to_offsets[future_item.name] = offset
                                    size_accumulator += future_item.size
                                elif item_type is StackFrameEnd:
                                    current_stack_frame_size = size_accumulator
                                    frame_terminated = True
                                    break
                            if frame_terminated:
                                break
                    if not frame_terminated:
                        raise Exception("stack frame never terminated: FRAME_END must be used after each FRAME_START")

                elif t is StackFrameEnd:
                    if not within_stack_frame:
                        raise Exception("FRAME_END cannot be used outside stack frame context; use FRAME_START first")
                    within_stack_frame = False
                    # stack frame concluded; clear all local variables mappings to avoid accidental use by assembler
                    stack_variables_to_offsets = {}
                    current_stack_frame_size = 0

                elif t is StackVariableDeclaration:
                    if not within_stack_frame:
                        raise Exception("stack variable declarations cannot be used outside of stack frame context; use FRAME_START first")
                    # do nothing; variable declarations do not emit code, and these were already
                    # counted when we encountered the StackFrameStart

                elif t is ConstVariableDeclaration:
                    # no code is generate for const variable declarations, so don't increment PC
                    # set the value of this variable as the value from the declaration
                    const_variables_to_values[item.name] = item.value

                elif ( t is StackVariableIdentifier
                    or t is StackVariableSize
                    or t is ConstVariableIdentifier
                    or t is ConstVariableValue
                ):
                    # there should be none of these in the code items
                    raise Exception(f"{t}s should have been consumed during Line.parse")

                elif t is StackFrameSizeImmediate:
                    if not within_stack_frame:
                        raise Exception("IFRAME_SIZE cannot be used outside stack frame context; use FRAME_START first")
                    if current_stack_frame_size > 0x7F:
                        raise Exception("IFRAME_SIZE cannot be used when stack frame size exceeds 0x7F; use FRAME_SIZE instead")
                    code.append(number_to_byte(current_stack_frame_size))
                    # immediate will take one byte
                    pc += 1

                elif t is StackFrameSizeFull:
                    if not within_stack_frame:
                        raise Exception("FRAME_SIZE cannot be used outside stack frame context; use FRAME_START first")
                    if current_stack_frame_size > 0xFFFFFFFF:
                        raise Exception("FRAME_SIZE cannot be used; frame size exceeds 0xFFFFFFFF")
                    # full frame size reference uses 4 bytes so it can be loaded with IAD instructions
                    code.append(address_to_bytes(current_stack_frame_size))
                    pc += 4

                elif t is StackVariableReferenceImmediate:
                    if not within_stack_frame:
                        raise Exception("stack variable cannot be used outside stack frame context; use FRAME_START first")
                    # if there's an exception here, the variable referenced has not been declared
                    offset = stack_variables_to_offsets[item.name]
                    if offset > 0x7F:
                        raise Exception("stack variable cannot be used; offset exceeds 0x7F; use STACK_ instead")
                    code.append(number_to_byte(offset))
                    # immediate will take one byte
                    pc += 1

                elif t is StackVariableReferenceFull:
                    if not within_stack_frame:
                        raise Exception("stack variable cannot be used outside stack frame context; use FRAME_START first")
                    # if there's an exception here, the variable referenced has not been declared
                    offset = stack_variables_to_offsets[item.name]
                    if offset > 0xFFFFFFFF:
                        raise Exception("stack variable cannot be used; offset exceeds 0xFFFFFFFF")
                    # full stack variable reference uses 4 bytes so it can be loaded with IAD instructions
                    code.append(address_to_bytes(offset))
                    pc += 4

                elif t is ConstVariableReferenceImmediate:
                    # if there's an exception here, the variable referenced has not been declared
                    value = const_variables_to_values[item.name]
                    if value > 0x7F:
                        raise Exception("const variable cannot be used; value exceeds 0x7F; use CONST_ instead")
                    code.append(number_to_byte(value))
                    # immediate will take one byte
                    pc += 1

                elif t is ConstVariableReferenceFull:
                    # if there's an exception here, the variable referenced has not been declared
                    value = const_variables_to_values[item.name]
                    if value > 0xFFFFFFFF:
                        raise Exception("const variable cannot be used; value exceeds 0xFFFFFFFF")
                    # full const variable reference uses 4 bytes so it can be loaded with IAD instructions
                    code.append(address_to_bytes(value))
                    pc += 4

        # final output assembly, with all label references replaced
        # list of byte literals only, (e.g.: "7F", "A0")
        replaced_code = []

        # second pass, goal is to replace all label references with targets,
        # which are all known now that the first pass is over
        for word in code:
            # find the label in the label mapping
            label_addr = label_mapping.get(word)

            if label_addr:
                # if it exists, push the 4 bytes to the output code
                for b in address_to_bytes(label_addr).split(" "):
                    replaced_code.append(b)
            else:
                # no matching label
                if re.match("^:+", word):
                    # if it's a label reference, its target was never defined
                    raise Exception(f"untranslated label {word}")
                # otherwise, it should be a legal instruction (a regular byte literal, "7F")
                replaced_code.append(word)

        # store the final generated assembly
        self.code = replaced_code

        # populate the annotated debug mapping
        for l in all_lines:
            annotated_line = f"{address_to_bytes(l.va)} |\t{l.original_line}"
            self.annotated_files[l.file_info.filename].append(annotated_line)
            if print_final:
                print(annotated_line)

###################################################################################################
# define CLI
parser = argparse.ArgumentParser(description="Assemble given code files into program and debug source files.")
parser.add_argument(
    "code_files",
    metavar="Files",
    type=str,
    nargs="+",
    help="list of code files to assemble, in order"
)
parser.add_argument(
    "-o",
    "--out",
    metavar="Output Directory",
    type=str,
    help="directory to write output files to"
)
parser.add_argument(
    "-p",
    "--print",
    action="store_true",
    help="print final assembly to stdout"
)

# parse command line arguments
args = parser.parse_args()

# assemble program
p = Program(args.code_files, print_final=False)

# create output path if provided and it doesn't exist
if args.out:
    Path(args.out).mkdir(parents=True, exist_ok=True)

# either print the output to stdout or create program.asm in output directory
# write the assembled program to file
final_assembly = " ".join(p.code).upper()
if args.print:
    print(final_assembly)
else:
    asm_file = open(os.path.join(args.out or "", "program.asm"), "w+")
    asm_file.write(final_assembly)
    asm_file.close()

# create the annotated debug source files
for filename in p.annotated_files:
    # write the annotated files back to their original directories
    # or in subdirectory of the given output directory that matches their original directory

    # extract original directory, minus the file name
    original_dir = os.path.dirname(filename)

    # create output directory if it doesn't exist
    output_dir = os.path.join(args.out or "", original_dir)
    Path(output_dir).mkdir(parents=True, exist_ok=True)

    # calculate the final file location with file name
    location = os.path.join(output_dir, f"{os.path.split(filename)[-1]}.debug")

    # open file and write contents
    output_file = open(location, "w+")
    output_file.writelines(p.annotated_files[filename])
    output_file.close()
