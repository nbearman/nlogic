from dataclasses import dataclass
from enum import Enum

class PageType(Enum):
    LeafPage = 1
    PageTable = 2
    PageDirectory = 3

class PageTableEntry:
    readable: bool
    write_protected: bool
    number: int
    original_pte_data: int

    def __init__(self, data: int):
        self.readable = 0b0 != (data & (0b1 << 31))
        self.write_protected = 0b0 != (data & (0b1 << 30))
        self.number = data & 0x000FFFFF
        self.original_pte_data = data

    def set_readable(self, value: bool):
        pass

    def set_write_protected(self, value: bool):
        pass

    def to_int(self) -> int:
        pass

@dataclass
class ProcessMapEntry:
    pid: int
    num_resident_pages: int
    page_directory_block: int
    page_directory_ppage: int
    disk_blocks: list[int]

@dataclass
class PPageMapEntry:
    pid: int
    page_type: PageType
    accessed: bool
    dirty: bool
    wired: bool
    share_count: int
    disk_block_number: int

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
        self.disk_block_map: list[int] = [0] * 0x20 # tracks the share count of the disk blocks in swap space; a count of 0 is a free block

    def read_memory(self, address: int) -> int:
        pass

    def write_memory(self, address: int, value: int):
        pass

    def find_open_block(self) -> int:
        pass

    def copy_page_to_disk(self, ppage: int, disk_block: int):
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
            # other pages may be pinned, too, for e.g. for an asynchronous operation
            return False

        if ppage_entry.page_type is PageType.LeafPage:
            # TODO can a leaf be evicted if the parent table is write protected?
            # probably yes, because page tables won't be shared; write protection will only be for tracking clean/dirty
            # so as long as we update the page table's dirty, we can update a write protected page table
            return True

        # scan page references for any pages that point back to this one as the owning page or directory
        for reference in self.reference_list:
            if reference.table_ppage == ppage:
                child_page_entry = self.physical_page_map[reference.ppage]
                if child_page_entry.share_count <= 1:
                    # there is a process that references a page in memory whose access is controlled by this table
                    # and that page is not a shared page
                    return False
                # else, we can evict a table it if only points to shared pages (but we must mark PTEs to shared pages as non-resident before evicting this table)

        return True


    def find_evictable_page(self):
        searching = True
        target_ppage = None
        count = 0
        num_pages = len(self.physical_page_map)
        while searching and count < (2 * num_pages):
            count += 1
            self.clock_hand = (self.clock_hand + 1) % num_pages
            if self.check_if_page_is_evictable(self.clock_hand):
                target_ppage = self.clock_hand
                searching = False
            else:
                self.clear_access(self.clock_hand)
        return target_ppage

    def evict_page_directory(self, ppage: int):
        pass


    def evict_page(self, ppage: int):
        ppage_entry = self.physical_page_map[ppage]

        if ppage_entry.page_type is PageType.PageDirectory:
            # evicting a directory has different logic because it has no PTE to update and its backing block is located elsewhere
            # since page tables cannot be shared, we don't need to worry about if the PDEs point to shared pages
            self.evict_page_directory(ppage)
            return

        elif ppage_entry.page_type is PageType.PageTable:
            # if this is a page table, remove any mapped references to shared pages, because they may no longer be resident when
            # this table comes back in. All other PTEs should already be marked non-resident for this table to have been considered evictable
            page_base_addr = 0x1000 * ppage
            for pte_offset in range(0x00, 0x1000, 0x04):
                pte_addr = page_base_addr + pte_offset
                pte = PageTableEntry(self.read_memory(pte_addr))
                if pte.readable: # if the page is readable it must be mapped to a physical page
                    refd_ppage = self.physical_page_map[pte.number]
                    if refd_ppage.share_count <= 1:
                        raise Exception("Attempted to evict a page table before all (non-shared) referenced pages have been paged out")

                    refd_ppage.share_count -= 1 # cannot be more than one, because
                    # page tables aren't shared, so this mapping that we're removing is valid for this single process only

                    pte.set_readable(False)
                    pte.set_write_protected(True)
                    self.write_memory(pte_addr, pte.to_int())
                    ppage_entry.dirty = True # set the table we're about to evict as dirty, since we just modified one of its PTEs

        elif ppage_entry.page_type is PageType.LeafPage:
            # nothing specific to do for evicting a leaf page
            pass

        else:
            raise ValueError()

        if ppage_entry.dirty:
            # physical page is dirty; it needs to be written back to disk
            backing_block_is_shared = self.disk_block_map[ppage_entry.disk_block_number] > ppage_entry.share_count
            # if the backing block is shared by only as many processes as share this physical page, we can continue to reuse that block
            # otherwise, we will need a new block to avoid overwriting shared memory that was paged out
            if backing_block_is_shared:
                if ppage_entry.page_type is not PageType.LeafPage:
                    raise Exception("Only leaf pages should be shared and have shared backing blocks.")
                # we are going to allocate a new disk block to use
                # remove the referencers pointing to this ppage from the disk block share count
                self.disk_block_map[ppage_entry.disk_block_number] -= ppage_entry.share_count
                open_block = self.find_open_block()
                self.disk_block_map[open_block] += ppage_entry.share_count
                ppage_entry.disk_block_number = open_block # store the newly allocated disk block in the ppage map entry; it only needs to be there for write back

        # update all the PDEs/PTEs that point to this ppage to hold the new disk block and be non-resident
        # (those tables' pages must also be marked dirty, since we're updating them)
        references_to_remove = []
        for reference in self.reference_list:
            if reference.ppage == ppage:
                # get the PDE/PTE referring to this ppage
                entry_addr = (reference.table_ppage * 0x1000) + (reference.vpage * 0x04)
                entry = PageTableEntry(self.read_memory(entry_addr))
                # update the disk block and mark it as non-resident (not readable, write protected)
                entry.number = ppage_entry.disk_block_number
                entry.set_readable(False)
                entry.set_write_protected(True)
                self.write_memory(entry_addr, entry.to_int())
                self.physical_page_map[reference.table_ppage].dirty = True
                references_to_remove.append(reference)

        for reference in references_to_remove:
            self.reference_list.remove(reference)

        if ppage_entry.dirty:
            self.copy_page_to_disk(ppage, ppage_entry.disk_block_number)

        return


    def handler(self):
        page_to_evict = self.find_evictable_page()
        self.evict_page(page_to_evict)


"""

Tables can only be evicted if all their entries are invalid (nonresident)
For pages they own solely, those pages must have been paged out first
For shared pages pointed to by the table, mark their PTEs as nonresident

Check if a table is evictable:
- If any references consider this table a parent:
    - If it is not a shared ppage, this table cannot be evicted
    - If it is a shared ppage, continue looking through other references
- If all references that consider this table a parent are shared ppages, this table can be evicted
    - Before evicting, mark all PTEs in this table that point to shared ppages as non-resident
    - When this table is brought back in, those shared ppages will need to be remapped (page fault, since PTE marked non-resident)


When updating a PTE during eviction:
- If the page table's PDE is write protected
    - If the PDE is copy-on-write


A page cannot be evicted if its owning table's PDE is write protected
- This prevents needing to resolve copy-on-write when updating the table of the page we're evicting
- Copy-on-write requires finding a free ppage; since we were already looking for a free page, we will now need to evict an additional page
- The second eviction might follow the same course: the owning page table may need to resolve a copy-on-write when we update it
- In theory, this doesn't need to result in a copy-on-write resolution when we are updating shared page tables just for evictions
    - The shared tables will all still point to the same disk block (whereas before they pointed to the same ppage)
    - The reference count on the new disk block must include all the processes that shared this page table
    - The table must remain copy-on-write, and also remain write protected
- In Linux, it turns out that page tables are copied during fork, not just the page directory.
    - This prevents the case where evicting a page would require updating a shared page table, which avoids the copy-on-write table situation described above
    - https://lwn.net/Articles/919143/#:~:text=A%20different%2C%20and%20somewhat%20more,the%20sharing%20will%20be%20broken.
    - "While the parent's memory is not copied into the child on fork(), the parent's page tables are copied. If the parent process has a large address space, that copying can still create a significant cost, and it may be entirely useless if the child does not access that memory"
    - https://kernel.org/doc/ols/2003/ols2003-pages-315-320.pdf

Kernel has a datastructure that tracks number of references to disk blocks

On fork:
- Increment the number of references to all disk blocks pointed to by this process
    - This will include updating counts for disk blocks that are only referenced by non-resident pages and page tables
- Increment the share count of all ppages referenced by this process (except the page directory's ppage)
- Mark all PDEs as copy-on-write (page directory will be present in memory when forking)
- Mark all PTEs in all resident tables as copy-on-write
- Copy the page directory to a new page
- Copy all page tables to new pages
    - Not using shared page tables will simplify management (Linux doesn't), and this could be added in the future

During read fault on page table, bring in page table:
- If PDE was copy-on-write, mark all PTEs in the newly resident table as copy-on-write
- Set the PDE to not copy-on-write, because now that this table is in memory, it is not shared
- (its backing disk block may still be shared, but that will be resolved during eviction and uses the disk block reference counter, not the PDE copy-on-write bit)

During any read fault, bringing in any page:
- Mark page as write-protected (because it is clean)

During eviction:
- If page is a page table, mark any PTEs still pointing to shared ppages as non-resident
    - Decrement the share count of those ppages by 1
    - Mark page table as dirty if there are changes from this
    - This prevents needing to update non-resident page tables when a shared page gets evicted
- If page is clean, discard page and update PTE to be non-resident (leave disk block if present)
- If page is dirty, check the number of references to the backing disk block
- If the number of references equals the share count of this ppage, write back to that disk block, mark PTE as non-resident (leave disk block on PTE)
    - (all PTEs referencing this page must be updated)
- If the number of references is > the share count of this ppage
    - Decrement the number of references to the disk block
    - Find a free disk block
    - Increment the number of references on the new disk block to equal the share count of this ppage
    - Store the new disk block number on the PTE
        - (all PTEs referencing this page must be updated)
    - Mark the PTE as non-resident
        - (all PTEs referencing this page must be updated)
    - Save the page to the new disk block
- Remove all references owned by this page

During write fault on any page:
- If owning entry (PTE or PDE) is not marked copy-on-write, mark page as dirty
- If owning entry (PTE or PDE) is marked copy-on-write, find a free page to allocate a new copy
- Copy this page to the new page
- Mark the new page as dirty
- Remove matching references to the old page, and add new references to the new page
- Update the owning entry (PTE or PDE) to mark this page as not copy-on-write, nor write protected
- When updating the PTE, if the table's own PDE is marked copy-on-write, the same process will need to be followed for that table
- Therefore, resolving a write fault may need as many as two open or evictable pages
- If we cannot find room for the new pages, abort


"""
