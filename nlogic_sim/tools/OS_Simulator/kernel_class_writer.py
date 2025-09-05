from dataclasses import dataclass
from enum import Enum

@dataclass
class Field:
    name: str
    type: type
    comment: str = ""
    size: int = 4

class PageType(Enum):
    Unknown = 0
    LeafPage = 1
    PageTable = 2
    PageDirectory = 3


class TableEntryType(Enum):
    Unknown = 0
    PDE = 1
    PTE = 2

structs = {
    "PhysicalPageMapEntry": [
        Field("page_type", PageType, "which page type resides in this physical page"),
        Field("disk_block_number", int, "which disk block number backs this page; 0 if there is no disk block"),
        Field("share_count", int, "number of processes that map a virtual page to this physical page"),
        Field("dirty", bool, "True if this page was modified since it was brought into memory"),
        Field("wired", bool, "True if this page should be ignored when finding a page to evict"),
        Field("file_backed", bool, "True if this page is backed by a file on disk (so the disk block should never change)"),
        Field("accessed", bool, "True if this page has been accessed recently, for use by eviction algorithm"),
        Field("child_count", int, """# number of pages that point to this as their owning table/directory
    # and number of those pages that are shared (point to this as well as other owners)
    # these counts are only updated on demand (by calling update_page_child_counts)"""),
        Field("shared_child_count", int),
    ],
    "ProcessMapEntry": [
        Field("pid", int),
        Field("page_directory_block", int),
        Field("page_directory_ppage", int),
    ],
    "PhysicalPageReference": [
        Field("ppage", int, "physical page being referenced"),
        Field("pid", int, "process whose reference this is; 0 if this is an empty reference"),
        Field("vpage", int, "the virtual page this process maps to this physical page\n    # unused if the physical page holds a page table or directory"),
        Field("table_ppage", int, "the physical page where the table that holds this mapping resides"),
        Field("table_number", int, "PDE number of the table that holds this mapping"),
    ],
    "DiskBlockReference": [
        Field("pid", int, "process with a page backed by this disk block; 0 if this is an empty reference"),
        Field("disk_block", int, "disk block number"),
    ],
}

auto_gen_message = (
"""
###########################################################
# This is a generated code file. The datastructures
# are defined in kernel_class_writer.py.
# Run kernel_class_writer.py to regenerate this file.

# These classes are auto-generated so that their sizes and
# offsets don't have to be computed or updated by hand, and
# so that they can be easily accessed in Python with intellisense.
###########################################################
"""
)


class ClassGenerator:
    def __init__(self, struct_dict: dict[str, list[Field]], enums: list[type[Enum]]):
        self.struct_dict = struct_dict
        self.enums = enums

    def generate(self):
        lines = []
        lines.append(f"{auto_gen_message}\n")
        lines.append("from dataclasses import dataclass\n")
        lines.append("from enum import Enum\n")
        lines.append("\n")
        with open("kernel_structs.py", mode="w") as f:
            for e in self.enums:
                lines.append(f"class {e.__name__}(Enum):\n")
                for item in e:
                    lines.append(f"    {item.name} = {item.value}\n")
                lines.append("\n")

            for struct_name, fields in self.struct_dict.items():
                offsets = {}
                offset_count = 0
                lines.append(f"@dataclass\n")
                lines.append(f"class {struct_name}:\n")
                for field in fields:
                    field_name = field.name
                    field_type = field.type
                    offsets[field_name] = offset_count
                    offset_count += field.size
                    field_type_name = field_type.__name__
                    if field.comment:
                        lines.append(f"    # {field.comment}\n")
                    lines.append(f"    {field_name}: {field_type_name} = {field_type_name}(0)\n")
                    if field.comment:
                        lines.append("\n")
                lines.append("\n    @staticmethod\n")
                lines.append("    def length():\n")
                lines.append(f"        return {offset_count}\n\n")
                lines.append("    class Offsets:\n")
                if not len(offsets):
                    lines.append("        pass\n")
                for field_name, offset in offsets.items():
                    lines.append(f"        {field_name}: int = {offset}\n")
                lines.append("\n")
            f.writelines(lines)

ClassGenerator(structs, [PageType, TableEntryType]).generate()
