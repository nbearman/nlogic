from dataclasses import dataclass, asdict
from enum import Enum

from mmu import MMU
from disk import Disk

PAGE_SIZE = 0x1000
PROCESS_MAP_LENGTH = 0x10
PHYSICAL_MEMORY_PAGES = 0x10
PAGE_REFERENCE_LIST_LENGTH = PHYSICAL_MEMORY_PAGES # there shouldn't be more references to ppages than physical pages?
DISK_BLOCK_REFERENCE_LIST_LENGTH = 0x20 # arbitrary for now
KERNEL_VARIABLE_BASE_ADDR = 0x6000


def dump_memory(memory: list[int], name: str = None):
    file_name = "memory_dump.txt"
    if name:
        file_name = f"memory_dump_{name}.txt"
    with open(file_name, "w") as f:
        lines = []
        for i in range(0, len(memory), 0x04):
            value = memory[i:i+4]
            str_value = " ".join([f"{x:02X}" for x in value])
            lines.append(f"{i:04X}\t{str_value}\n")
        f.writelines(lines)


class KernelVariable(Enum):
    ProcessMap = "PROCESS_MAP"
    PhysicalPageMap = "PHYSICAL_PAGE_MAP"
    PhysicalPageReferenceList = "PHYSICAL_PAGE_REFERENCE_LIST"
    DiskBlockReferenceList = "DISK_BLOCK_REFERENCE_LIST"
    ActiveProcessId = "ACTIVE_PROCESS_ID"
    ActiveProcessPageDirectoryPhysicalPage = "ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE"


class PageType(Enum):
    LeafPage = 1
    PageTable = 2
    PageDirectory = 3


class TableEntryType(Enum):
    PDE = 1
    PTE = 2

@dataclass
class ProcessMapEntry:
    pid: int = 0
    page_directory_block: int = 0
    page_directory_ppage: int = 0

@dataclass
class PhysicalPageMapEntry:
    # which page type resides in this physical page
    page_type: PageType = 0

    # which disk block number backs this page; 0 if there is no disk block
    disk_block_number: int = 0

    # number of processes that map a virtual page to this physical page
    share_count: int = 0

    # True if this page was modified since it was brought into memory
    dirty: bool = False

    # True if this page should be ignored when finding a page to evict
    wired: bool = False

    # True if this page is backed by a file on disk (so the disk block should never change)
    file_backed: bool = False

    # True if this page has been accessed recently, for use by eviction algorithm
    accessed: bool = False

@dataclass
class PhysicalPageReference:
    # process whose reference this is; 0 if this is an empty reference
    pid: int = 0

    # physical page being referenced
    ppage: int = 0

    # the virtual page this process maps to this physical page
    vpage: int = 0

    # the physical page where the table that holds this mapping resides
    table_ppage: int = 0

@dataclass
class DiskBlockReference:
    # process with a page backed by this disk block; 0 if this is an empty reference
    pid: int = 0

    # disk block number
    disk_block: int = 0


def generate_kernel_arrays(base_virtual_address: int):
    process_map_entry_size = 0x04 * len(asdict(ProcessMapEntry()))
    ppage_map_entry_size = 0x04 * len(asdict(PhysicalPageMapEntry()))
    ppage_reference_size = 0x04 * len(asdict(PhysicalPageReference()))
    disk_block_reference_size = 0x04  * len(asdict(DiskBlockReference()))

    process_map_size = process_map_entry_size * PROCESS_MAP_LENGTH
    ppage_map_size = ppage_map_entry_size * PHYSICAL_MEMORY_PAGES
    ppage_reference_list_size = ppage_reference_size * PHYSICAL_MEMORY_PAGES
    disk_block_reference_list_size = disk_block_reference_size * DISK_BLOCK_REFERENCE_LIST_LENGTH

    variables_and_sizes = [
        (KernelVariable.ProcessMap, process_map_size),
        (KernelVariable.PhysicalPageMap, ppage_map_size),
        (KernelVariable.PhysicalPageReferenceList, ppage_reference_list_size),
        (KernelVariable.DiskBlockReferenceList, disk_block_reference_list_size),
        (KernelVariable.ActiveProcessId, 0x04),
        (KernelVariable.ActiveProcessPageDirectoryPhysicalPage, 0x04)
    ]
    addr = base_virtual_address
    kernel_addresses = {}

    for (variable, size) in variables_and_sizes:
        kernel_addresses[variable] = addr
        addr += size

    # return variable start addresses in VA and total size of variables mapped
    return kernel_addresses, (addr - base_virtual_address)

variable_offsets, total_size = generate_kernel_arrays(KERNEL_VARIABLE_BASE_ADDR)
for k, v in variable_offsets.items():
    print(f"{k.value}:\t\t0x{v:08x}")
print(f"0x{total_size:08x} bytes")


PROCESS_MAP_ADDR = variable_offsets[KernelVariable.ProcessMap]
PHYSICAL_PAGE_MAP_ADDR = variable_offsets[KernelVariable.PhysicalPageMap]
PHYSICAL_PAGE_REFERENCE_LIST_ADDR = variable_offsets[KernelVariable.PhysicalPageReferenceList]
DISK_BLOCK_REFERENCE_LIST_ADDR = variable_offsets[KernelVariable.DiskBlockReferenceList]
ACTIVE_PROCESS_ID_ADDR = variable_offsets[KernelVariable.ActiveProcessId]
ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR = variable_offsets[KernelVariable.ActiveProcessPageDirectoryPhysicalPage]


class Environment:
    def __init__(self, size_in_bytes: int = 2**16):
        self.memory = [0] * size_in_bytes
        self.mmu = MMU(self.memory)
        self.disk = Disk(self.memory)
        self.page_reference_hash_table = {} # TODO replace this with a custom hash table
            # (collision resolution: k-step, linked list, linear probing?)
            # (probably linear probing to leverage cache the best)

    def read_memory(self, addr: int) -> int:
        pass

    def write_memory(self, addr: int, value: int):
        pass

    def get_active_process_id(self) -> int:
        # return from active process ID variable
        pass

    def get_directory_ppage(self, pid: int) -> int:
        """
        if PID is 0, return from active process dir ppage variable
        otherwise, get directory ppage from process map
        """
        pass

    def access_page_through_table(self, parent_table_ppage: int, entry_number: int) -> tuple[int, bool]:
        """
        Returns tuple of PDE/PTE and True if the page was paged in from disk, False if the page was already present
        """
        pass

    def get_table_number_from_addr(self, addr: int) -> int:
        pass

    def get_page_number_from_addr(self, addr: int) -> int:
        pass

    def get_offset_from_addr(self, addr: int) -> int:
        pass

    def get_entry_is_cow(self, entry: int) -> bool:
        pass

    def get_entry_is_readable(self, entry: int) -> bool:
        pass

    def get_entry_is_write_protected(self, entry: int) -> bool:
        pass

    def get_entry_number(self, entry: int) -> int:
        pass

    def get_entry_is_mapped(self, entry: int) -> bool:
        pass

    def set_entry_write_protected(self, entry: int, new_value: bool) -> int:
        pass

    def set_entry_readable(self, entry: int, new_value: bool) -> int:
        pass

    def set_entry_non_resident(self, entry: int) -> int:
        entry = self.set_entry_readable(entry, False)
        entry = self.set_entry_write_protected(entry, True)
        return entry

    def set_entry_cow(self, entry: int, new_value: bool) -> int:
        pass

    def set_entry_number(self, entry: int, new_value: int) -> int:
        pass

    def get_entry_disk_block(self, entry: int) -> int:
        return self.get_entry_number(entry)

    def get_entry_ppage(self, entry: int) -> int:
        return self.get_entry_number(entry)

    def get_ppage_is_clean(self, ppage: int) -> bool:
        pass

    def get_ppage_is_dirty(self, ppage: int) -> bool:
        return not self.get_ppage_is_clean(ppage)

    def get_ppage_is_table(self, ppage: int) -> bool:
        pass

    def get_ppage_is_leaf(self, ppage: int) -> bool:
        pass

    def get_ppage_is_file_backed(self, ppage: int) -> bool:
        pass

    def get_ppage_is_directory(self, ppage: int) -> bool:
        pass

    def get_ppage_backing_block(self, ppage: int) -> int:
        pass

    def get_ppage_share_count(self, ppage: int) -> int:
        pass

    def set_ppage_clean(self, ppage: int):
        pass

    def set_ppage_dirty(self, ppage: int):
        pass

    def set_ppage_backing_block(self, ppage: int, new_value: int):
        pass

    def set_ppage_share_count(self, ppage: int, new_value: int):
        pass

    def set_ppage_accessed(self, ppage: int, new_value: bool):
        pass

    def decrement_ppage_share_count(self, ppage: int):
        pass

    def clear_ppage_map_slot(self, ppage: int):
        pass

    def get_disk_block_share_count(self, disk_block: int) -> int:
        pass

    def decrement_disk_block_share_count(self, disk_block: int):
        pass

    def increment_disk_block_share_count(self, disk_block: int):
        pass

    def remove_reference_to_disk_block(self, disk_block: int, pid: int):
        # TODO what is this supposed to do? What do the disk block references track?
        pass

    def remove_page_reference(self, process_id: int, parent_table_ppage: int, vpage: int):
        """
        Remove the given reference from the reference list.
        """
        pass

    def add_page_reference(self, process_id: int, parent_table_ppage: int, vpage: int):
        """
        Add a reference to the reference list.
        """
        pass

    def get_all_page_references(self, ppage: int) -> list[PhysicalPageReference]:
        pass

    def get_single_page_reference(self, ppage: int) -> PhysicalPageReference:
        pass

    def get_open_disk_block(self) -> int:
        """
        Returns 0 if there are no open blocks
        """

    def copy_ppage_to_disk(self, ppage: int, disk_block: int):
        pass

    def copy_ppage_contents(self, source_ppage: int, dest_ppage: int):
        """
        Copies data from source physical page to destination physical page.
        Does not modify the physical page map.
        """
        pass

    def clear_ppage_contents(self, ppage: int):
        pass

    def get_evictable_ppage(self) -> tuple[int, bool]:
        """
        Returns (evictable ppage, success)
        Success will be false if there are no open ppages, in which case, the open ppage result should be ignored.
        """
        pass

    def get_open_ppage(self) -> tuple[int, bool]:
        """
        Returns (open ppage, success)
        Success will be false if there are no open ppages, in which case, the open ppage result should be ignored.
        """
        pass

    def update_pde_in_directory(self, directory_ppage, pde_number, new_pde) -> bool:
        pass


    def update_entry_in_table(
        self,
        directory_ppage: int, # ppage of the grand parent table
        pde_number: int,
        table_ppage: int, # ppage of the parent table
        pte_number: int, #
        new_entry: int,
        entry_type: TableEntryType,
    ) -> int:
        """
        This function will update a PDE or PTE. If a PTE is updated, its PDE might need to be updated, too.

        When updating a PDE, the PTE parameters will be ignored.
        """
        # TODO do we need to set tables to accessed when updating entries? probably not, since
        # accessed should generally refer to access via user process? or should writing to the
        # table count as recent access?

        # if we're updating a PTE, fetch the previous version and compare it
        if entry_type is TableEntryType.PTE:
            pte_addr = (table_ppage * 0x1000) + (pte_number * 0x04)
            pte = self.read_memory(pte_addr)
            if new_entry == pte:
                # the existing entry matches, so no changes are needed
                return
            # else the entry has changed; update the table and its ppage entry
            self.set_ppage_dirty(table_ppage)
            self.set_ppage_accessed(table_ppage, True)
            self.write_memory(pte_addr, new_entry)

        # whether this was a PTE update that caused a table change or a PDE update,
        # get the existing PDE
        pde_addr = (directory_ppage * 0x1000) + (pde_number * 0x04)
        pde = self.read_memory(pde_addr)

        if entry_type is TableEntryType.PTE:
            # if this was a PTE update, the updated PDE is the one from the table with W set to 0
            updated_pde = self.set_entry_write_protected(pde, False)
        else:
            # if this was a PDE update, the updated PDE was passed as new_entry
            updated_pde = new_entry

        if updated_pde == pde:
            # if the existing entry matches, no changes are needed
            return

        self.set_ppage_dirty(directory_ppage)
        self.set_ppage_accessed(directory_ppage, True)
        self.write_memory(pde_addr, updated_pde)


    def evict_page(self, ppage: int):
        if self.get_ppage_is_directory(ppage):
            # evicting directory
            raise NotImplementedError("TODO haven't planned out directory eviction yet")

        is_table = self.get_ppage_is_table(ppage)
        if is_table:
            # when evicting a table, we need to mark any shared pages it has mapped as non-resident, in case they get paged out
            # while this table is not resident
            page_table_base_addr = (ppage * 0x1000)
            table_was_updated = False
            for pte_offset in range(0x00, 0x1000, 0x04):
                pte_addr = page_table_base_addr + pte_offset
                pte = self.read_memory(pte_addr)
                # a table should only be evicted if all its mapped pages are non-resident already EXCEPT for shared pages
                # therefore, any remaining readable page must be shared; mark only those as non-resident now and decerement
                # the share count of the ppages they point to
                if self.get_entry_is_readable(pte):
                    table_was_updated = True
                    # decrease share count of ppage pointed to by this PTE
                    ppage = self.get_entry_ppage(pte)
                    self.decrement_ppage_share_count(ppage)
                    backing_block = self.get_ppage_backing_block(ppage)

                    # update the PTE to be non-resident and point to the backing block of the ppage
                    pte = self.set_entry_non_resident(pte)
                    pte = self.set_entry_number(pte, backing_block)
                    self.write_memory(pte_addr, pte)
            if table_was_updated:
                self.set_ppage_dirty(ppage)

        backing_block = self.get_ppage_backing_block(ppage)
        is_dirty = self.get_ppage_is_dirty(ppage)
        is_file_backed = self.get_ppage_is_file_backed(ppage)
        if is_dirty:
            block_share_count = self.get_disk_block_share_count(backing_block)
            if not is_file_backed:
                if block_share_count > 1:
                    if is_table:
                        raise Exception("Tables cannot be shared.")
                    # split into new backing block
                self.decrement_disk_block_share_count(backing_block)
                new_block = self.get_open_disk_block()
                if not new_block:
                    raise Exception("No open disk blocks; out of swap space.")
                self.increment_disk_block_share_count(new_block)
                self.set_ppage_backing_block(new_block)

                # to find which process is getting a new disk block, look in the reference list
                refs = self.get_all_page_references(ppage)
                if len(refs) > 1:
                    # this was a dirty page, and not file backed, so there should be exactly 1 ref
                    raise Exception("Dirty, non-file-backed page cannot be shared.")
                self.remove_reference_to_disk_block(backing_block, refs[0].pid)
                backing_block = new_block
            self.copy_ppage_to_disk(ppage, new_block)

        self.clear_ppage_contents(ppage)
        self.clear_ppage_map_slot(ppage)

        refs_to_update = self.get_all_page_references(ppage)
        if is_table:
            # for leaf pages, there may be multiple references to update
            # for tables, there should be exactly one
            if len(refs_to_update) > 0:
                raise Exception("Table page should not have multiple references because tables cannot be shared.")

        for ref in refs_to_update:
            # get the PTE from its table
            pte_addr = (ref.table_ppage * 0x1000) + (ref.vpage * 0x04)
            pte = self.read_memory(pte_addr)
            # update the PTE with the new block number and mark it as non-resident
            pte = self.set_entry_number(backing_block)
            pte = self.set_entry_non_resident(pte)
            self.update_entry_in_table(
                self.get_directory_ppage(ref.pid),
                None, # PDE number; where to get this? probably store it on the ref
                ref.table_ppage,
                ref.vpage,
                pte,
                None, # entry type, where to get this? probably store it on the ref
            )


    def handle_leaf_page_cow(self, table_ppage: int, vpage: int, pte: int) -> int:
        """
        Returns the updated PTE.
        """
        (open_ppage, success_open_page) = self.get_open_ppage()
        if not success_open_page:
            (evictable_ppage, success_evictable_page) = self.get_evictable_ppage()
            if not success_evictable_page:
                raise Exception("Out of memory: could not find open or evictable page while handling copy-on-write.")
            self.evict_page(evictable_ppage)
            open_ppage = evictable_ppage

        new_ppage = open_ppage
        source_ppage = self.get_entry_ppage(pte)

        self.copy_ppage_contents(source_ppage, new_ppage)
        self.decrement_ppage_share_count(new_ppage)
        self.add_page_reference(self.get_active_process_id(), table_ppage, vpage)
        self.remove_page_reference()
        self.set_ppage_share_count(new_ppage, 1)
        self.set_ppage_clean(new_ppage)
        updated_pte = self.set_entry_cow(pte, False)
        return updated_pte

    def handle_table_cow(self, directory_ppage: int, pde: int) -> int:
        """
        Returns the updated PDE.
        """
        page_table_base_addr = (self.get_entry_ppage(pde) * 0x1000)
        for pte_offset in range(0x00, 0x1000, 0x04):
            pte_addr = page_table_base_addr + pte_offset
            pte = self.read_memory(pte_addr)
            if self.get_entry_is_mapped(pte):
                pte = self.set_entry_cow(pte, True)
                self.write_memory(pte_addr, pte)

        updated_pde = self.set_entry_cow(pde, False)
        self.update_entry_in_table(directory_ppage, directory_ppage, updated_pde)
        return updated_pde

    def handle_page_fault(self, faulted_addr: int, is_write: bool):
        active_process_directory_ppage = self.get_directory_ppage()
        pde_number = self.get_table_number_from_addr(faulted_addr)
        (pde, table_is_newly_resident) = self.access_page_through_table(active_process_directory_ppage, pde_number)
        if table_is_newly_resident:
            if self.get_entry_is_cow(pde):
                # handle newly resident page table c-o-w
                pde = self.handle_table_cow(active_process_directory_ppage, pde)

        table_ppage = self.get_entry_ppage(pde)
        pte_number = self.get_page_number_from_addr(faulted_addr)
        (pte, page_is_newly_resident) = self.access_page_through_table(table_ppage, pte_number)

        if not is_write:
            # jump back to program
            return

        if not self.get_entry_is_write_protected(pte):
            # jump back to program
            return

        target_ppage = self.get_entry_ppage(pte)

        if self.get_entry_is_cow(pte):
            # handle page is c-o-w
            pte = self.handle_leaf_page_cow(table_ppage, pte_number, pte)

        if self.get_ppage_is_clean(target_ppage):
            raise Exception("Physical page cannot be write protected and dirty.")

        self.set_ppage_dirty(target_ppage)

        pte = self.set_entry_write_protected(pte, False)
        self.update_entry_in_table(active_process_directory_ppage, table_ppage, pte)

        # jump back to program
        return



