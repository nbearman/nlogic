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
    page_type: PageType = PageType.LeafPage

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

    # number of pages that point to this as their owning table/directory
    # and number of those pages that are shared (point to this as well as other owners)
    # these counts are only updated on demand (by calling update_page_child_counts)
    child_count: int = 0
    shared_child_count: int = 0

@dataclass
class PhysicalPageReference:
    # physical page being referenced
    ppage: int = 0

    # process whose reference this is; 0 if this is an empty reference
    pid: int = 0

    # the virtual page this process maps to this physical page
    # unused if the physical page holds a page table or directory
    vpage: int = 0

    # the physical page where the table that holds this mapping resides
    table_ppage: int = 0

    # PDE number of the table that holds this mapping
    table_number: int = 0

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

    def read_physical_memory(self, addr: int) -> int:
        """
        Reads the given address in physical address space by adjusting
        it to the kernel's virtual address space.
        """
        # TODO replace appropriate read_memory() calls with this
        pass

    def write_memory(self, addr: int, value: int):
        """
        Writes to the given address in physical address space by adjusting
        it to the kernel's virtual address space.
        """
        # TODO replace appropriate write_memory() calls with this
        pass

    def get_active_process_id(self) -> int:
        # return from active process ID variable
        pass

    def get_process_page_count(self, process_id: int) -> int:
        pass

    def get_directory_ppage(self, pid: int) -> int:
        """
        if PID is 0, return from active process dir ppage variable
        otherwise, get directory ppage from process map
        """
        pass


    def get_clock_hand(self) -> int:
        pass

    def increment_clock_hand(self):
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

    def get_ppage_is_wired(self, ppage: int) -> bool:
        pass

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

    def get_ppage_accessed(self, ppage: int) -> bool:
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

    def set_ppage_child_count(self, ppage: int, new_value: int):
        pass

    def set_ppage_shared_child_count(self, ppage: int):
        pass

    def increment_ppage_child_count(self, ppage: int, new_value: int):
        pass

    def increment_ppage_shared_child_count(self, ppage: int):
        pass

    def get_ppage_child_count(self, ppage: int) -> int:
        pass

    def get_ppage_shared_child_count(self, ppage: int):
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
        # we need to know if a disk block is shared before writing a dirty page back to disk
        # and we also need to know which disk blocks are available
        pass

    def remove_page_reference(self, ppage: int, process_id: int, vpage: int, table_ppage: int, table_number: int):
        """
        Remove the given reference from the reference list.
        """
        pass

    def add_page_reference(self, ppage: int, process_id: int, vpage: int, table_ppage: int, table_number: int):
        """
        Add a reference to the reference list.
        """
        pass

    def get_all_references(self) -> list[PhysicalPageReference]:
        """
        Get all the page references to all pages in a single list
        """
        pass

    def get_all_page_references(self, ppage: int) -> list[PhysicalPageReference]:
        """
        Get all the page references to a single page
        """
        pass

    def get_single_page_reference(self, ppage: int) -> PhysicalPageReference:
        # TODO probably not going to be used
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


    def get_open_ppage(self) -> tuple[int, bool]:
        """
        Returns (open ppage, success)
        Success will be false if there are no open ppages, in which case, the open ppage result should be ignored.
        """
        pass

    def load_page_from_disk(self, disk_block: int, target_ppage: int):
        pass

    def check_if_page_is_present(
        self,
        ppage: int, # ppage to check
        process_id: int, # process who we are checking for
        entry_number: int, # number (page table or vpage) from the PDE/PTE
        entry_type: TableEntryType, # the type of entry whose child we're checking (PTE > leaf, PDE > table)
    ) -> bool:
        """
        Return true if the given table or virtual page is present in the given physical page.

        A virtual page is present in the given ppage if there is a reference to that ppage with:
            - matching process ID
            - matching virtual page number
        A page table is present in the given ppage if there is a reference to that ppage with:
            - matching process ID
            - matching table number
        """
        if ppage >= PHYSICAL_MEMORY_PAGES:
            # the given ppage is outside the range of physical memory; it cannot be present
            return False

        refs = self.get_all_page_references(ppage)
        for ref in refs:
            pid_matches = ref.pid == process_id
            if entry_type == TableEntryType.PTE:
                number_matches = ref.vpage == entry_number
                page_type_matches = self.get_ppage_is_leaf(ppage)
            elif entry_type == TableEntryType.PDE:
                number_matches = ref.table_number == entry_number
                page_type_matches = self.get_ppage_is_table(ppage)
            if pid_matches and number_matches and page_type_matches:
                return True
        return False

    def update_page_child_counts(self):
        # reset the counts for all ppages (they are stale)
        for ppage in range(PHYSICAL_MEMORY_PAGES):
            self.set_ppage_child_count(ppage, 0)
            self.set_ppage_shared_child_count(ppage, 0)

        # iterate over all the references (
        for ppage in range(PHYSICAL_MEMORY_PAGES):
            # get all references to this ppage
            refs = self.get_all_page_references(0)
            num_sharers = len(refs)

            # for each table that owns a reference to this page, update its counts
            for ref in refs:
                self.increment_ppage_child_count(ref.table_ppage)
                # if there is more than one ref to this ppage, its a shared page
                # and so its owning tables all increment their shared child counts
                if num_sharers > 1:
                    self.increment_ppage_shared_child_count(ref.table_ppage)


    def access_page_through_table(
            self,
            pid: int,
            directory_ppage: int, # ppage of the grand parent table
            pde_number: int,
            table_ppage: int, # ppage of the parent table
            pte_number: int,
            entry_type: TableEntryType,
        ) -> tuple[int, bool]:
        """
        Returns tuple of PDE/PTE and True if the page was paged in from disk, False if the page was already present

        When accessing a PDE, the PTE parameters will be ignored.
        """
        entry_number = pte_number
        parent_table_ppage = table_ppage
        if entry_type is TableEntryType.PDE:
            entry_number = pde_number
            parent_table_ppage = directory_ppage

        entry_addr = (parent_table_ppage * 0x1000) + (entry_number * 0x04)
        entry = self.read_memory(entry_addr)

        if self.get_entry_is_readable(entry):
            return (entry, False)
        if not self.get_entry_is_write_protected():
            raise Exception("Access to unmapped page.")

        potential_ppage = self.get_entry_ppage(entry)

        page_is_resident = self.check_if_page_is_present(
            potential_ppage,
            pid,
            entry_number,
            entry_type
        )

        if page_is_resident:
            entry = self.set_entry_readable(entry, True)
            actual_ppage = potential_ppage
            page_newly_resident = False
        else:
            # the number in the entry is actually a disk block
            # find an empty physical page and load the page from disk
            (open_ppage, open_ppage_success) = self.get_open_ppage()
            if not open_ppage_success:
                (evictable_ppage, evictable_ppage_success) = self.get_evictable_ppage()
                if not evictable_ppage_success:
                    raise Exception("Out of memory: could not find open or evictable page while handling page access.")
                self.evict_page(evictable_ppage)
                open_ppage = evictable_ppage
            # bring page into memory
            disk_block = potential_ppage
            self.load_page_from_disk(disk_block, open_ppage)
            self.set_ppage_backing_block(open_ppage, disk_block)
            self.set_ppage_clean(open_ppage)
            self.set_ppage_share_count(open_ppage, 1)
            entry = self.set_entry_number(open_ppage)
            entry = self.set_entry_readable(entry, True)
            entry = self.set_entry_write_protected(entry, False)
            actual_ppage = open_ppage
            page_newly_resident = True

        self.update_entry_in_table(
            directory_ppage,
            pde_number,
            table_ppage,
            pte_number,
            entry,
            entry_type
        )

        self.set_ppage_accessed(actual_ppage, True)
        return (entry, page_newly_resident)


    def check_if_page_is_evictable(self, ppage: int) -> bool:
        if self.get_ppage_is_wired(ppage):
            return False

        if self.get_ppage_is_directory(ppage):
            # a directory can only be evicted if it has no resident children
            ref = self.get_all_page_references(ppage)[0]
            resident_page_count = self.get_process_page_count(ref.pid)
            if resident_page_count <= 1: # TODO this isn't updated anywhere, but it should be (during access and eviction?)
                return True

        elif self.get_ppage_is_table(ppage):
            # a table can only be evicted if all its children pages have already been evicted, unless
            # those pages are shared by another process
            child_count = self.get_ppage_child_count(ppage)
            if child_count == 0:
                return True
            if child_count > 0:
                shared_child_count = self.get_ppage_shared_child_count(ppage)
                if shared_child_count == child_count:
                    # all child pages are also shared, so we can evict this table
                    return True

        else:
            # a leaf page is always a candidate for eviction
            return True

        return False


    def get_evictable_ppage(self) -> tuple[int, bool]:
        """
        Returns (evictable ppage, success)
        Success will be false if there are no open ppages, in which case, the open ppage result should be ignored.
        """
        # make up to two full sweeps with the clock hand
        # during each tick, set each encountered page to "not accessed"
        # if the clock makes a full sweep, each page encountered for the second
        # time will be "not accessed," and therefore might be evictable
        clock_ticks = PHYSICAL_MEMORY_PAGES * 0x02

        # first update child counts on ppages, since they may be stale
        # it is faster to update them all in a batch since it requires
        # iterating over all references
        self.update_page_child_counts()


        for i in range(clock_ticks):
            ppage = self.get_clock_hand()
            # check access first because it is cheaper than checking if the table is
            # actually evictable
            if not self.get_ppage_accessed(ppage):
                if self.check_if_page_is_evictable(ppage):
                    # if the page is evictable (not wired, no children, etc.) and has not
                    # been accessed recently, choose the page for eviction
                    return (ppage, True)

            else: # page was recently accessed; clear the access and protect PTEs
                # clear access on the ppage
                self.set_ppage_accessed(ppage, False)

                # mark all PTEs that references this page / table as RW01 so we fault on access
                # to give us an opportunity to mark the page as accessed again
                refs_to_update = self.get_all_page_references(ppage)
                for ref in refs_to_update:
                    entry_type = TableEntryType.PTE
                    entry_number = ref.vpage
                    if self.get_ppage_is_table(ppage):
                        # if we're looking at a table, then the ref we're updating is a PDE
                        entry_type = TableEntryType.PDE
                        entry_number = ref.table_number

                    # get the PTE from its table
                    pte_addr = (ref.table_ppage * 0x1000) + (entry_number * 0x04)
                    pte = self.read_memory(pte_addr)
                    # update the PTE as not readble and write protected (so the page can be marked
                    # as accessed next time the page is read or written)
                    pte = self.set_entry_readable(pte, False)
                    pte = self.set_entry_write_protected(pte, True)

                    self.update_entry_in_table(
                        self.get_directory_ppage(ref.pid),
                        ref.table_number,
                        ref.table_ppage,
                        ref.vpage,
                        pte,
                        entry_type,
                    )
            # advance the clock hand
        # there are no evictable pages
        return (0, False)


    def update_entry_in_table(
        self,
        directory_ppage: int, # ppage of the grand parent table
        pde_number: int,
        table_ppage: int, # ppage of the parent table
        pte_number: int,
        new_entry: int,
        entry_type: TableEntryType,
    ):
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
                self.set_ppage_backing_block(ppage, new_block)

                # to find which process is getting a new disk block, look in the reference list
                refs = self.get_all_page_references(ppage)
                if len(refs) > 1:
                    # this was a dirty page, and not file backed, so there should be exactly 1 ref
                    raise Exception("Dirty, non-file-backed page cannot be shared.")
                self.remove_reference_to_disk_block(backing_block, refs[0].pid)
                backing_block = new_block
            self.copy_ppage_to_disk(ppage, backing_block)

        self.clear_ppage_contents(ppage)
        self.clear_ppage_map_slot(ppage)

        refs_to_update = self.get_all_page_references(ppage)
        if is_table:
            # for leaf pages, there may be multiple references to update
            # for tables, there should be exactly one
            if len(refs_to_update) > 0:
                raise Exception("Table page should not have multiple references because tables cannot be shared.")

        for ref in refs_to_update:
            entry_type = TableEntryType.PTE
            entry_number = ref.vpage
            if self.get_ppage_is_table(ppage):
                # if we're evicting a table, then the ref we're updating is a PDE
                entry_type = TableEntryType.PDE
                entry_number = ref.table_number

            # get the PTE from its table
            pte_addr = (ref.table_ppage * 0x1000) + (entry_number * 0x04)
            pte = self.read_memory(pte_addr)
            # update the PTE with the new block number and mark it as non-resident
            pte = self.set_entry_number(pte, backing_block)
            pte = self.set_entry_non_resident(pte)

            self.update_entry_in_table(
                self.get_directory_ppage(ref.pid),
                ref.table_number,
                ref.table_ppage,
                ref.vpage,
                pte,
                entry_type,
            )


    def handle_leaf_page_cow(self, table_ppage: int, table_number: int, vpage: int, pte: int) -> int:
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
        pid = self.get_active_process_id()
        self.add_page_reference(new_ppage, pid, vpage, table_ppage, table_number)
        self.remove_page_reference(
            source_ppage,
            pid,
            vpage,
            table_ppage,
            table_number,
        )
        self.set_ppage_share_count(new_ppage, 1)
        self.set_ppage_clean(new_ppage)
        updated_pte = self.set_entry_cow(pte, False)
        return updated_pte

    def handle_table_cow(self, directory_ppage: int, pde_number: int, pde: int) -> int:
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
        self.update_entry_in_table(
            directory_ppage,
            pde_number,
            0,
            0,
            updated_pde,
            TableEntryType.PDE,
        )
        return updated_pde

    def handle_page_fault(self, faulted_addr: int, is_write: bool):
        active_process_directory_ppage = self.get_directory_ppage(0)
        active_process_id = self.get_active_process_id()
        pde_number = self.get_table_number_from_addr(faulted_addr)
        (pde, table_is_newly_resident) = self.access_page_through_table(
            active_process_id,
            active_process_directory_ppage,
            pde_number,
            0,
            0,
            TableEntryType.PDE,
        )
        if table_is_newly_resident:
            if self.get_entry_is_cow(pde):
                # handle newly resident page table c-o-w
                pde = self.handle_table_cow(active_process_directory_ppage, pde_number, pde)

        table_ppage = self.get_entry_ppage(pde)
        pte_number = self.get_page_number_from_addr(faulted_addr)
        (pte, page_is_newly_resident) = self.access_page_through_table(
            active_process_id,
            active_process_directory_ppage,
            pde_number,
            table_ppage,
            pte_number,
            TableEntryType.PTE,
        )

        if not is_write:
            # jump back to program
            return

        if not self.get_entry_is_write_protected(pte):
            # jump back to program
            return

        target_ppage = self.get_entry_ppage(pte)

        if self.get_entry_is_cow(pte):
            # handle page is c-o-w
            pte = self.handle_leaf_page_cow(table_ppage, pde_number, pte_number, pte)

        if self.get_ppage_is_clean(target_ppage):
            raise Exception("Physical page cannot be write protected and dirty.")

        self.set_ppage_dirty(target_ppage)

        pte = self.set_entry_write_protected(pte, False)
        self.update_entry_in_table(
            active_process_directory_ppage,
            pde_number,
            table_ppage,
            pte_number,
            pte,
            TableEntryType.PTE,
        )

        # jump back to program
        return

