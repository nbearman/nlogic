import re

def address_to_bytes(addr):
    num = f"{addr:0>8X}"
    if len(num) > 8:
        raise Exception("address longer than 32 bits")
    return f"{num[0:2]} {num[2:4]} {num[4:6]} {num[6:8]}"

def replace_dmem(dmem):
        if not "dmem" in dmem.lower():
            raise Exception("replace_dmem called on string without dmem")
        num = int(dmem.lower().replace("dmem", ""), base=16) + 0xC0
        return f"{num:0>2X}"


class FileInfo:
    def __init__(self, filename, line_number):
        self.filename = filename
        self.line = line_number

    def get_local_label_prefix(self):
        return f"__file_{self.filename}__"

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
        self.va = None

class Literal:
    def __init__(self, literal):
        self.literal = literal

class Fill:
    def __init__(self, fill: str):
        self.target = int(fill.lower().replace("fill", ""), base=16)

class Instruction:
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

    def __init__(self, inst):
        self.inst = inst
        self.byte = self.name_to_byte.get(inst)
        if not self.byte:
            raise Exception(f"cannot parse instruction {inst}")

class Line:
    def parse(self, line):
        location = line.find("//")
        if location >= 0:
            line = line[:location]

        result = []
        for word in line.split():

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
            
            dmem_match = re.match("(dmem)[0-9a-f][0-9a-f]$", word.lower())
            if dmem_match:
                result.append(Literal(replace_dmem(dmem_match.group())))
                continue

            fill_match = re.match("(fill)[0-9a-f]+$", word.lower())
            if fill_match:
                result.append(Fill(word))
                continue

            literal_match = re.match("^[0-9a-f][0-9a-f]$", word.lower())
            if literal_match:
                result.append(Literal(word))
                continue

            # try to parse the word as an instruction
            result.append(Instruction(word))
        return result

    def __init__(self, filename, line_num, line):
        self.file_info = FileInfo(filename, line_num)
        self.original_line = line
        self.va = None
        self.intermediate = self.parse(line)

class Program:
    def __init__(self, filenames):
        all_lines = []
        for name in filenames:
            file = open(name)
            line_num = 0
            for line in file:
                all_lines.append(Line(name, line_num, line))

        # labels -> VAs
        label_mapping = {}

        pc = 0
        code = []

        for l in all_lines:
            for item in l.intermediate:

                t = type(item)
                if t is Literal:
                    if l.va is None:
                        l.va = pc
                    code.append(item.literal)
                    pc += 1

                elif t is Instruction:
                    if l.va is None:
                        l.va = pc
                    code.append(item.byte)
                    pc += 1
                
                elif t is Fill:
                    if pc > item.target:
                        raise Exception("cannot fill; already past target")
                    if l.va is None:
                        l.va = pc
                    while pc < item.target:
                        code.append("00")
                        pc += 1
                
                elif t is LabelReference:
                    if l.va is None:
                        l.va = pc
                    code.append(item.label)
                    pc += 4

                elif t is LabelDefinition:
                    # store label without preceding "@"
                    label_mapping[item.label[1:]] = pc

        replaced_code = []
        import pdb; pdb.set_trace()
        for word in code:
            # look up label without preceding ":"
            # looking up global labels fails because they are looked up as ":label" but stored as "@"
            label_addr = label_mapping.get(word[1:])
            if label_addr:
                for b in address_to_bytes(label_addr).split(" "):
                    replaced_code.append(b)
            else:
                if ":" in word:
                    raise Exception(f"untranslated label {word}")
                replaced_code.append(word)
        self.code = replaced_code


p = Program(["assemble_debug_test/f1.txt"])
print(p.code)



"""

@local_label
@@global_label

@__local__local_label
@@__local__local_label

"""


