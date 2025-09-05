
###########################################################
# This is a generated code file. The datastructures
# are defined in kernel_class_writer.py.
# Run kernel_class_writer.py to regenerate this file.

# These classes are auto-generated so that their sizes and
# offsets don't have to be computed or updated by hand, and
# so that they can be easily accessed in Python with intellisense.
###########################################################

from dataclasses import dataclass
from enum import Enum

class PageType(Enum):
    Unknown = 0
    LeafPage = 1
    PageTable = 2
    PageDirectory = 3

class TableEntryType(Enum):
    Unknown = 0
    PDE = 1
    PTE = 2

@dataclass
class PhysicalPageMapEntry:
    # which page type resides in this physical page
    page_type: PageType = PageType(0)

    # which disk block number backs this page; 0 if there is no disk block
    disk_block_number: int = int(0)

    # number of processes that map a virtual page to this physical page
    share_count: int = int(0)

    # True if this page was modified since it was brought into memory
    dirty: bool = bool(0)

    # True if this page should be ignored when finding a page to evict
    wired: bool = bool(0)

    # True if this page is backed by a file on disk (so the disk block should never change)
    file_backed: bool = bool(0)

    # True if this page has been accessed recently, for use by eviction algorithm
    accessed: bool = bool(0)

    # # number of pages that point to this as their owning table/directory
    # and number of those pages that are shared (point to this as well as other owners)
    # these counts are only updated on demand (by calling update_page_child_counts)
    child_count: int = int(0)

    shared_child_count: int = int(0)

    @staticmethod
    def length():
        return 36

    class Offsets:
        page_type: int = 0
        disk_block_number: int = 4
        share_count: int = 8
        dirty: int = 12
        wired: int = 16
        file_backed: int = 20
        accessed: int = 24
        child_count: int = 28
        shared_child_count: int = 32

@dataclass
class ProcessMapEntry:
    pid: int = int(0)
    page_directory_block: int = int(0)
    page_directory_ppage: int = int(0)

    @staticmethod
    def length():
        return 12

    class Offsets:
        pid: int = 0
        page_directory_block: int = 4
        page_directory_ppage: int = 8

@dataclass
class PhysicalPageReference:
    # physical page being referenced
    ppage: int = int(0)

    # process whose reference this is; 0 if this is an empty reference
    pid: int = int(0)

    # the virtual page this process maps to this physical page
    # unused if the physical page holds a page table or directory
    vpage: int = int(0)

    # the physical page where the table that holds this mapping resides
    table_ppage: int = int(0)

    # PDE number of the table that holds this mapping
    table_number: int = int(0)


    @staticmethod
    def length():
        return 20

    class Offsets:
        ppage: int = 0
        pid: int = 4
        vpage: int = 8
        table_ppage: int = 12
        table_number: int = 16

@dataclass
class DiskBlockReference:
    # process with a page backed by this disk block; 0 if this is an empty reference
    pid: int = int(0)

    # disk block number
    disk_block: int = int(0)


    @staticmethod
    def length():
        return 8

    class Offsets:
        pid: int = 0
        disk_block: int = 4

