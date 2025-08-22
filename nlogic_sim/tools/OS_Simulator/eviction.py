from dataclasses import dataclass
from enum import Enum

class PageType(Enum):
    LeafPage = 1
    PageTable = 2
    PageDirectory = 3

@dataclass
class ProcessMapEntry:
    pid: int
    num_resident_pages: int
    page_directory_block: int
    page_directory_ppage: int

@dataclass
class PPageMapEntry:
    pid: int
    page_type: PageType
    accessed: bool
    dirty: bool
    wired: bool

@dataclass
class PageReference:
    ppage: int # ppage this is a reference to
    vpage: int # virtual page number if this is a leaf page; table number if this is a page table
        # if vpage number, ONLY the vpage part of the number (index into page table)
    table_ppage: int # physical page of parent page table (or directory, if this is a page table)
        # the parent is guaranteed to be in memory, because a page will only be evicted if it has no children remaining
    pid: int # process that owns this reference



class Kernel:
    def __init__(self):
        self.process_map: list[ProcessMapEntry] = []
        self.physical_page_map: list[PPageMapEntry] = []
        self.reference_list: list[PageReference] = []
        self.clock_hand: int = 0

    def read_memory(self, address: int) -> int:
        pass

    def write_memory(self, address: int, value: int):
        pass

    def set_not_readable(self, table_ppage: int, entry_number: int):
        # setting a page as not readable must also set it as write protected, otherwise
        # it will have protection bits 00, which indicates unmapped
        pte_address = (table_ppage << 22) | (entry_number << 2)
        pte = self.read_memory(pte_address)
        # clear readable bit
        pte = pte & 0x7FFFFFFF
        # set write protected bit
        pte = pte | 0x40000000
        self.write_memory(pte_address, pte)

    def clear_access(self, ppage: int):
        ppage_entry = self.physical_page_map[ppage]
        ppage_entry.accessed = False
        if ppage_entry.page_type is PageType.PageDirectory:
            return

        # there may be several processes and page tables that refer to this page
        # go through all of them to set their PTEs as not readable
        for reference in self.reference_list:
            if reference.ppage != ppage:
                # this is an unrelated reference
                continue
            # this reference refers to this page
            self.set_not_readable(reference.table_ppage, reference.vpage)


    def check_if_page_is_evictable(self, ppage: int) -> bool:
        ppage_entry = self.physical_page_map[ppage]
        if ppage_entry.accessed:
            # give pages that have been recently accessed a second chance; mark this as not accessed
            # and evict if the clock comes around again and it still hasn't been accessed
            return False

        if ppage_entry.wired:
            # certain kernel pages can never be evicted
            return False

        if ppage_entry.page_type is PageType.LeafPage:
            # any leaf page can always be evicted
            return True

        # scan page references for any pages that point back to this one as the owning page or directory
        for reference in self.reference_list:
            if reference.table_ppage == ppage:
                # there is a process that references a page in memory whose access is controlled by this table
                return False

        return True


    def find_evictable_page(self):
        searching = True
        target_ppage = None
        while searching:
            self.clock_hand = (self.clock_hand + 1) % len(self.physical_page_map)
            if self.check_if_page_is_evictable(self.clock_hand):
                target_ppage = self.clock_hand
                searching = False
            else:
                self.clear_access(self.clock_hand)
        return target_ppage


    def evict_page(self, ppage: int):
        # clear all references to this page
        raise NotImplementedError("TODO implement this this")


    def handler(self):
        page_to_evict = self.find_evictable_page()
        self.evict_page(page_to_evict)
