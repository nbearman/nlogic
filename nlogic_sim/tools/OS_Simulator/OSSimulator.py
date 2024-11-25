from enum import Enum, auto

ACTIVE_PROCESS_ID_ADDR = 0xAAAA0000
ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR = 0xBBBB0000
PROCESS_MAP_ENTRY_SIZE = 0x10
PROCESS_MAP_LENGTH = 0x10
PROCESS_MAP_ADDR = 0xCCCC0000
PHYSICAL_MEMORY_PAGES = 0x10
PHYSICAL_PAGE_MAP_ADDR = 0xDDDD0000
PHYSICAL_PAGE_MAP_ENTRY_SIZE = 0x1C
MMIO_DISK_BASE_ADDR = 0xEEEE0000

MEMORY = {
    ACTIVE_PROCESS_ID_ADDR: 0x02,
    ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR: 0x00005000,
}

class Register(Enum):
    LINK = auto()
    PC = auto()
    FLAG = auto()

class MMURegister(Enum):
    FAULTED_PTE = auto()
    FAULTED_VA = auto()
    FAULTED_OPERATION = auto()

def write_memory(address, value):
    pass

def read_memory(address):
    pass

def read_register(register):
    pass

def read_mmu_register(mmu_register):
    pass

def lite_find_process_map_entry_index_by_id(process_id):
    tmp_stop_offset = PROCESS_MAP_ENTRY_SIZE * PROCESS_MAP_LENGTH
    tmp_offset = 0x00
    while tmp_offset != tmp_stop_offset:
        tmp_read_process_id = read_memory(PROCESS_MAP_ADDR + tmp_offset)
        if not tmp_read_process_id == process_id:
            tmp_offset += PROCESS_MAP_ENTRY_SIZE
            continue
        else:
            tmp_index = tmp_offset / PROCESS_MAP_ENTRY_SIZE
            return (0x01, tmp_index)
    return (0x00, None)

def lite_get_pte_kpa(virtual_page_number, table_physical_page_number):
    tmp_shifted_ppage_number = table_physical_page_number << 0x0C
    tmp_shifted_vpage_number = virtual_page_number << 0x02
    tmp_combined_pte_physical_address = tmp_shifted_ppage_number | tmp_shifted_vpage_number
    tmp_kpa = tmp_combined_pte_physical_address + 0x003F0000
    return tmp_kpa

def lite_number_from_pte(pte):
    return pte & 0x000FFFFF

def lite_check_ppage_matches_vpage(active_process_id, virtual_page_number, physical_page_number):
    # TODO bug: (PHYSICAL_MEMORY_PAGES - 1) is the max, not PHYSICAL_MEMORY_PAGES
    tmp_negative_number = (PHYSICAL_MEMORY_PAGES - physical_page_number) & 0x80000000
    if not tmp_negative_number == 0x00:
        return 0x00
    tmp_ppage_map_offset = PHYSICAL_PAGE_MAP_ENTRY_SIZE * physical_page_number
    tmp_read_ppage_map_entry_process_id = read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset)
    if not tmp_read_ppage_map_entry_process_id == active_process_id:
        return 0x00
    tmp_read_ppage_map_entry_vpage_number = read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x08)
    if tmp_read_ppage_map_entry_vpage_number == virtual_page_number:
        return 0x01
    return 0x00

def lite_get_open_ppage():
    tmp_index = 0x00
    while tmp_index != PHYSICAL_MEMORY_PAGES:
        tmp_offset = tmp_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
        tmp_read_process_id = read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_offset)
        if tmp_read_process_id == 0x00:
            return tmp_index
        tmp_index += 0x01
    raise Exception("No open pages (not yet implemented)")

def lite_load_pte_to_ppage(process_map_entry_index, virtual_page_number, process_page_directory_physical_ppage, process_id, pte, ppage_index):
    tmp_ppage_map_offset = ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x00, process_id)
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x04, process_page_directory_physical_ppage)
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x08, virtual_page_number)
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x0C, 0x01)
    tmp_disk_block_number = pte & 0x000FFFFF
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x10, tmp_disk_block_number)
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x14, 0x01)
    write_memory(MMIO_DISK_BASE_ADDR + 0x00, ppage_index)
    write_memory(MMIO_DISK_BASE_ADDR + 0x04, tmp_disk_block_number)
    write_memory(MMIO_DISK_BASE_ADDR + 0x08, 0x00)
    write_memory(MMIO_DISK_BASE_ADDR + 0x0C, 0x01)
    tmp_process_map_entry_offset = PROCESS_MAP_ADDR + process_map_entry_index * PROCESS_MAP_ENTRY_SIZE
    tmp_process_previous_resident_pages = read_memory(tmp_process_map_entry_offset + 0x08)
    write_memory(tmp_process_map_entry_offset + 0x08, tmp_process_previous_resident_pages + 0x01)
    tmp_pte_unset_number = pte & 0xFFF00000

    #TODO appears to be unused, looks like the new page number is not written to the PTE
    tmp_pte_new_number = tmp_pte_unset_number | ppage_index
    tmp_pte_unset_protection_bits = pte & 0x3FFFFFFF
    # likely fix
    # tmp_pte_unset_protection_bits = tmp_pte_new_number & 0x3FFFFFFF

    tmp_pte_rw_set = tmp_pte_unset_protection_bits | 0xC0000000
    return tmp_pte_rw_set

def lite_set_ppage_field(value, offset, ppage_index):
    tmp_ppage_map_offset = ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
    write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + offset, value)


def lite_set_pte_readable(pte):
    return pte | 0x80000000

def lite_make_ppage_writable(ppage_index):
    tmp_ppage_map_offset = ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
    tmp_ppage_dirty_field = read_memory(tmp_ppage_map_offset + 0x18)
    tmp_ppage_num_references = read_memory(tmp_ppage_map_offset + 0x0C)
    if tmp_ppage_dirty_field == 0x01:
        return ppage_index
    if not tmp_ppage_num_references == 0x01:
        raise Exception("Page is shared (not yet implemented)")
    write_memory(tmp_ppage_map_offset + 0x18, 0x01)
    return ppage_index

def lite_set_pte_number(number, pte):
    tmp_unset_number = pte & 0xFFF00000
    return tmp_unset_number | number

def lite_set_pte_writable(pte):
    return pte & 0xBFFFFFFF


def page_fault_handler():
    return_location = None        # address to return to from function call (in LINK when called)
    pc_val = None                 # PC contents
    flag_val = None               # FLAG contents
    faulted_pte = None            # faulted PTE
    faulted_va = None             # faulted virtual address
    faulted_operation = None      # faulted operation (0 if read, 1 if write)
    faulted_pte_block = None      # disk block from faulted PTE
    faulted_va_vpage_num = None   # virtual page number from faulted virtual address
    faulted_va_table_num = None   # virtual table number from faulted virtual address
    active_process_id = None      # process ID of the active process
    active_process_page_directory_ppage = None    # physical page of the active process's page directory
    active_process_map_entry_index = None         # index of active process in the process map
    target_ppage = None           # physical page new page is moved into (TODO: unused)
    fetched_pde = None            # possibly different than faulted PTE if the leaf page faulted
    fetched_pte = None            # retrieved by following the PDE if leaf page faulted
                                   #   should be the same as faulted PTE in that case
    fetched_pde_kpa = None        # VA in kernel space of fetched PDE's physical address
    fetched_pte_kpa = None        # VA in kernel space of fetched PTE's physical address

    #lower 20 bits of fetched PDE and PTE; if the page it points
    #to is resident, it's a physical page number, else it's a disk
    #block number where the page was evicted to
    fetched_pde_number = None
    fetched_pte_number = None

    updated_pde = None            # PDE data after updating tables
    updated_pte = None            # PTE data after updating tables
    virtual_table_ppage = None    # physical page number of the VA's page table
    virtual_page_ppage = None     # physical page number of the VA's page

    # ====================================================

    return_location = read_register(Register.LINK)
    # store faulted PTE and faulted virtual address in local variables
    faulted_pte = read_mmu_register(MMURegister.FAULTED_PTE)
    faulted_va = read_mmu_register(MMURegister.FAULTED_VA)
    faulted_operation = read_mmu_register(MMURegister.FAULTED_OPERATION)

    ###############################
    faulted_va = 0x00001000
    faulted_operation = 0x00
    ###############################

    # break the VA into its parts and store in local variables
    tmp_table_number_mask = 0xFFC00000
    faulted_va_table_num = (faulted_va & tmp_table_number_mask) >> 0x16

    tmp_virtual_page_number_mask = 0x003FF000
    faulted_va_vpage_num = (faulted_va & tmp_virtual_page_number_mask) >> 0x0C

    # get the active process's ID and page directory physical page number from kernel memory
    # and store them in local variables
    active_process_id = read_memory(ACTIVE_PROCESS_ID_ADDR)
    active_process_page_directory_ppage = read_memory(ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR)

    # store active process entry index in local variable
    (tmp_find_process_map_entry_success, tmp_find_process_map_entry_result) = lite_find_process_map_entry_index_by_id(active_process_id)
    if not tmp_find_process_map_entry_success:
        raise Exception("No process map entry found for active process")
    active_process_map_entry_index = tmp_find_process_map_entry_result

    # get PDE
    fetched_pde_kpa = lite_get_pte_kpa(faulted_va_table_num, active_process_page_directory_ppage)
    tmp_read_pde = read_memory(fetched_pde_kpa)
    fetched_pde = tmp_read_pde
    updated_pde = tmp_read_pde

    # save the ppage/disk block number from the PDE in local variable
    tmp_directory_number_mask = 0x000FFFFF
    fetched_pde_number = tmp_read_pde & tmp_directory_number_mask

    # check PDE protection bits to see if page table is mapped
    tmp_rw_bit_mask = 0xC0000000
    tmp_pde_rw_bits = (tmp_read_pde & tmp_rw_bit_mask) >> 0x1E
    if tmp_pde_rw_bits == 0x00:
        raise Exception("Page table is not mapped")


    virtual_table_ppage = lite_number_from_pte(fetched_pde)
    tmp_check_table_is_resident_result = lite_check_ppage_matches_vpage(active_process_id, faulted_va_table_num, virtual_table_ppage)

    # page table not resident
    if tmp_check_table_is_resident_result == 0x01:
        virtual_table_ppage = lite_get_open_ppage()
        updated_pde = lite_load_pte_to_ppage(active_process_map_entry_index, faulted_va_table_num, active_process_page_directory_ppage, active_process_id, fetched_pde, virtual_table_ppage)

    # page table is resident
    # update physical page map to show page is referenced because we're reading this table now
    lite_set_ppage_field(0x01, 0x01, virtual_table_ppage)

    # set R to 1 on PDE since the table is loaded and referenced now
    updated_pde = lite_set_pte_readable(updated_pde)
    # also store updated PDE back into page directory
    write_memory(fetched_pde_kpa, updated_pde)

    # mark the directory ppage as dirty, since we updated the PDE
    # TODO only if the PDE was actually updated
    lite_set_ppage_field(0x01, 0x18, active_process_page_directory_ppage)

    # get PTE
    # calculate PTE physical address
    fetched_pte_kpa = lite_get_pte_kpa(faulted_va_vpage_num, virtual_table_ppage)
    tmp_read_pte = read_memory(fetched_pte_kpa)
    fetched_pte = tmp_read_pte
    updated_pte = tmp_read_pte

    tmp_pte_page_number_mask = 0x000FFFFF
    tmp_pte_page_number_result = tmp_read_pte & tmp_pte_page_number_mask
    fetched_pte_number = tmp_pte_page_number_result
    virtual_page_ppage = tmp_pte_page_number_result

    # check PTE protection bits to see if page is mapped
    tmp_rw_bit_mask = 0xC0000000
    tmp_pte_rw_bits = (tmp_read_pte & tmp_rw_bit_mask)
    if tmp_pte_rw_bits == 0x00:
        raise Exception("Page is not mapped")

    # check page is resident
    tmp_check_page_is_resident_result = lite_check_ppage_matches_vpage(active_process_id, faulted_va_vpage_num, tmp_read_pte)

    # page not resident
    if tmp_check_page_is_resident_result == 0x01:
        # page is not resident, which means the number in the PTE actually represents
        # the disk block where the page was evicted to
        virtual_page_ppage = lite_get_open_ppage()
        updated_pte = lite_load_pte_to_ppage(active_process_map_entry_index, faulted_va_vpage_num, active_process_page_directory_ppage, active_process_id, fetched_pte, virtual_page_ppage)

    # page is resident
    # update physical page map to show ppage is referenced because we're accessing this page now
    lite_set_ppage_field(0x01, 0x14, virtual_page_ppage)

    # set R to 1 on PTE since the page is loaded and referenced
    updated_pte = lite_set_pte_readable(updated_pte)

    # if this was a write operation, make sure the page is writable
    if not faulted_operation == 0x00:
        # the operation was a write operation, so make the page writable
        tmp_w_bit_mask = 0x40000000
        tmp_page_w_bit = (updated_pte & tmp_w_bit_mask)
        if not tmp_page_w_bit == 0x00:
            # page is write protected
            tmp_new_writable_ppage_result = lite_make_ppage_writable(virtual_page_ppage)
            # set the PTE page number in case it changed
            tmp_updated_pte_result = lite_set_pte_number(tmp_new_writable_ppage_result, updated_pte)
            updated_pte = lite_set_pte_writable(tmp_updated_pte_result)

    # check if the PTE needs to be written back
    if not fetched_pte == updated_pte:
        # PTE is dirty
        # PTE has been changed, so it will need to be written back
        # the page table is definitely mapped, resident, and referenced,
        # since we loaded and read it to get to this point; i.e. R on
        # the PDE has been set to 1

        # check if W is 0 or 1; if it's 1, the page table is clean, shared, or both
        tmp_w_bit_mask = 0x40000000
        tmp_pde_w_bit = (updated_pde & tmp_w_bit_mask)
        if not tmp_pde_w_bit == 0x00:
            # page table is write protected
            virtual_table_ppage = lite_make_ppage_writable(virtual_table_ppage)

            # set updated PDE's number to new ppage
            tmp_set_pde_number_result = lite_set_pte_number(virtual_table_ppage, updated_pde)
            tmp_set_pde_writable_result = lite_set_pte_writable(tmp_set_pde_number_result)

            # write PDE back to page directory
            write_memory(fetched_pde_kpa, tmp_set_pde_writable_result)

            # recalculate PTE KPA
            # TODO load the page table ppage number into GPH
            #  (currently GPH holds the PDE entry, has the table ppage at the end, and
            #      so it happens to work when we shift the PDE left)
            fetched_pte_kpa = lite_get_pte_kpa(faulted_va_vpage_num, tmp_set_pde_number_result)

        # page table not write protected
        # store updated PTE into page table
        write_memory(fetched_pte_kpa, updated_pte)

    # PTE not dirty
    return

if __name__ == "__main__":
    page_fault_handler()
