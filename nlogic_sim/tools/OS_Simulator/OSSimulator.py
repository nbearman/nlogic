import os

from dataclasses import dataclass
from enum import Enum, auto

ACTIVE_PROCESS_ID_ADDR = 0x0EA6
ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR = 0x0EAA
PROCESS_MAP_ENTRY_SIZE = 0x10
PROCESS_MAP_LENGTH = 0x10
PROCESS_MAP_ADDR = 0x0480
PHYSICAL_MEMORY_PAGES = 0x10
PHYSICAL_PAGE_MAP_ADDR = 0x0100
PHYSICAL_PAGE_MAP_ENTRY_SIZE = 0x1C
MMIO_DISK_BASE_ADDR = 0x8000 # base addr of disk mapped in kernel VA
PAGE_SIZE = 0x1000
MMIO_BASE_PHYSICAL_ADDR = 0xFF000000
MMIO_DISK_BASE_PHYSICAL_ADDR = MMIO_BASE_PHYSICAL_ADDR # these are the same because the disk is the first MMIO device
VALID_DISK_BLOCKS = (64, 65, 66, 100, 101, 102, 103) # files that are available in the OS simulators disk_blocks/ directory

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


@dataclass
class MMU:
    environment_memory: list[int] | bytearray

    # TODO the way this is used, it's actually expected to be the physical page number of the page directory
    ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS: int
    QUEUED_PAGE_DIRECTORY_BASE_ADDRESS: int

    MMU_DIRECTORY_SWAP_BREAKPOINT: int
    FAULTED_PTE: int
    FAULTED_ADDRESS: int
    BREAKPOINT_ENABLED: int
    ENABLED: int
    BREAKPOINT_CYCLE_DELAY_COUNTER: int
    FAULTED_OPERATION: int

    faulted: bool = False

    class ProtectionCheckResult(Enum):
        SAFE = 0
        RETRY_INTERRUPT = 1
        NON_RETRY_INTERRUPT = 2

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

    def write_environment_memory(self, address: int, value: int):
        self.environment_memory[address:address + 0x04] = value.to_bytes(0x04, "big")

    def read_environment_memory(self, address: int):
        return int.from_bytes(self.environment_memory[address:address + 0x04], "big")

    def swap_directories(self):
        swap = self.ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS
        self.ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS = self.QUEUED_PAGE_DIRECTORY_BASE_ADDRESS
        self.QUEUED_PAGE_DIRECTORY_BASE_ADDRESS = swap

    @staticmethod
    def check_protection(pte: PageTableEntry, write: bool):
        if pte.readable:
            if pte.write_protected and write:
                return MMU.ProtectionCheckResult.RETRY_INTERRUPT
            return MMU.ProtectionCheckResult.SAFE
        else:
            if pte.write_protected:
                return MMU.ProtectionCheckResult.RETRY_INTERRUPT
            else:
                return MMU.ProtectionCheckResult.NON_RETRY_INTERRUPT


    @staticmethod
    def get_page_directory_entry_address(page_directory_base_address: int, virtual_address: int) -> int:
        directory_number = (virtual_address & 0xFFC00000) >> 22
        return (page_directory_base_address << 12) | (directory_number << 2)

    @staticmethod
    def get_page_table_entry_address(page_table_physical_page_number: int, virtual_address: int) -> int:
        page_number = (virtual_address & 0x003FF000) >> 12
        return (page_table_physical_page_number << 12) | (page_number << 2)

    @staticmethod
    def get_physical_address(physical_page_number: int, virtual_address: int) -> int:
        offset_into_page = virtual_address & 0x00000FFF
        return (physical_page_number << 12) | offset_into_page

    def translate_address(self, address: int, write: bool) -> int:
        if self.ENABLED == 0x00:
            return address
        translation = 0
        if self.faulted:
            raise Exception("MMU is faulted; cannot translate address.")

        if (
            self.BREAKPOINT_ENABLED
            and (self.BREAKPOINT_CYCLE_DELAY_COUNTER == 0x00)
            and (address == self.MMU_DIRECTORY_SWAP_BREAKPOINT)
        ):
            self.BREAKPOINT_ENABLED = 0x00
            self.swap_directories()

        page_directory_entry_address = MMU.get_page_directory_entry_address(self.ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS, address)
        page_directory_entry_data = self.read_environment_memory(page_directory_entry_address)
        pde = MMU.PageTableEntry(page_directory_entry_data)
        protection = MMU.check_protection(pde, False)

        if protection is not MMU.ProtectionCheckResult.SAFE:
            raise Exception("Page fault while accessing PDE.")

        page_table_entry_address = MMU.get_page_table_entry_address(pde.number, address)
        page_table_entry_data = self.read_environment_memory(page_table_entry_address)
        pte = MMU.PageTableEntry(page_table_entry_data)

        protection = MMU.check_protection(pte, write)
        if protection is not MMU.ProtectionCheckResult.SAFE:
            raise Exception("Page fault while accessing PTE.")

        translation = MMU.get_physical_address(pte.number, address)
        return translation


class Disk:
    class Mode(Enum):
        READ = 0
        WRITE = 1

    def __init__(self, environment_memory: list[int] | bytearray, disk_blocks_to_cache: list[int] = None):
        if not disk_blocks_to_cache:
            disk_blocks_to_cache = []
        self.environment_memory = environment_memory
        self.physical_page: int = None
        self.disk_block: int = None
        self.mode: Disk.Mode = None
        self.disk_block_map = {}
        for block in disk_blocks_to_cache:
            file_name = f"{block:05}.txt"
            file_path = os.path.join("disk_blocks", file_name)
            with open(file_path, "r") as file:
                contents = file.read().strip()
            block_data = [0] * PAGE_SIZE
            copied_bytes = [int(x, 16) for x in contents.split(" ")]
            block_data[:len(copied_bytes)] = copied_bytes
            self.disk_block_map[block] = block_data

    def write_memory(self, address: int, value: int):
        if address == 0x00:
            self.physical_page = value
        elif address == 0x04:
            self.disk_block = value
        elif address == 0x08:
            self.mode = Disk.Mode.WRITE if value else Disk.Mode.READ
        elif address == 0x0C:
            self.initiate()
        else:
            raise ValueError(f"Write to unknown disk register: 0x{address:08X}")

    def read_memory(self, address: int):
        raise NotImplementedError()

    def initiate(self):
        target_physical_page_addr = PAGE_SIZE * self.physical_page
        # if write, store the contents of the physical page on disk
        if self.mode is Disk.Mode.WRITE:
            self.disk_block_map[self.disk_block] = self.environment_memory[target_physical_page_addr:target_physical_page_addr + PAGE_SIZE]
        # if read, copy the contents of the disk block into the physical page
        elif self.mode is Disk.Mode.READ:
            self.environment_memory[target_physical_page_addr:target_physical_page_addr + PAGE_SIZE] = self.disk_block_map[self.disk_block]
        else:
            raise NotImplementedError()


class Environment:
    def __init__(self, size_in_bytes: int = 2**16, valid_disk_blocks: list[int] = None):
        self.memory = [0] * size_in_bytes
        # self.memory = bytearray(size_in_bytes) # both bytearray and list work, but list is easier to read in the debugger
        self.mmu = MMU(self.memory, 0, 0, 0, 0, 0, 0, 0, 0, 0)
        self.disk = Disk(self.memory, valid_disk_blocks)

    def mmio_write_memory(self, address: int, value: int) -> None:
        if address >= MMIO_DISK_BASE_PHYSICAL_ADDR and address <= MMIO_DISK_BASE_PHYSICAL_ADDR + 0x10:
            self.disk.write_memory(address - MMIO_BASE_PHYSICAL_ADDR, value)
        else:
            raise NotImplementedError()


    def mmio_read_memory(self, address: int) -> int:
        raise NotImplementedError()

    def write_memory(self, address: int, value: int) -> None:
        address = self.mmu.translate_address(address, True)

        if address >= MMIO_BASE_PHYSICAL_ADDR:
            self.mmio_write_memory(address, value)
            return

        if address < 0 or (address + 4) > len(self.memory):
            raise ValueError(f"Memory write at 0x{address:08X} out of bounds.")
        self.memory[address:address + 0x04] = value.to_bytes(0x04, "big")

    def read_memory(self, address: int) -> int:
        address = self.mmu.translate_address(address, False)

        if address >= MMIO_BASE_PHYSICAL_ADDR:
            return self.mmio_read_memory(address)

        if address < 0 or (address + 4) > len(self.memory):
            raise ValueError(f"Memory read at 0x{address:08X} out of bounds.")
        return int.from_bytes(self.memory[address:address + 0x04], "big")

    def read_register(self, register):
        pass

    def read_mmu_register(self, mmu_register: MMURegister):
        if mmu_register is MMURegister.FAULTED_PTE:
            return self.mmu.FAULTED_PTE
        elif mmu_register is MMURegister.FAULTED_VA:
            return self.mmu.FAULTED_ADDRESS
        elif mmu_register is MMURegister.FAULTED_OPERATION:
            return self.mmu.FAULTED_OPERATION
        raise Exception("Unimplemented MMU register read.")


    def lite_find_process_map_entry_index_by_id(self, process_id):
        tmp_stop_offset = PROCESS_MAP_ENTRY_SIZE * PROCESS_MAP_LENGTH
        tmp_offset = 0x00
        while tmp_offset != tmp_stop_offset:
            tmp_read_process_id = self.read_memory(PROCESS_MAP_ADDR + tmp_offset)
            if not tmp_read_process_id == process_id:
                tmp_offset += PROCESS_MAP_ENTRY_SIZE
                continue
            else:
                tmp_index = int(tmp_offset / PROCESS_MAP_ENTRY_SIZE)
                return (0x01, tmp_index)
        return (0x00, None)

    def lite_get_pte_kpa(self, virtual_page_number, table_physical_page_number):
        tmp_shifted_ppage_number = table_physical_page_number << 0x0C
        tmp_shifted_vpage_number = virtual_page_number << 0x02
        tmp_combined_pte_physical_address = tmp_shifted_ppage_number | tmp_shifted_vpage_number
        tmp_kpa = tmp_combined_pte_physical_address + 0x003F0000
        return tmp_kpa

    def lite_number_from_pte(self, pte):
        return pte & 0x000FFFFF

    def lite_check_ppage_matches_vpage(self, active_process_id, virtual_page_number, physical_page_number):
        # TODO bug: (PHYSICAL_MEMORY_PAGES - 1) is the max, not PHYSICAL_MEMORY_PAGES
        tmp_negative_number = (PHYSICAL_MEMORY_PAGES - physical_page_number) & 0x80000000
        if not tmp_negative_number == 0x00:
            return 0x00
        tmp_ppage_map_offset = PHYSICAL_PAGE_MAP_ENTRY_SIZE * physical_page_number
        tmp_read_ppage_map_entry_process_id = self.read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset)
        if not tmp_read_ppage_map_entry_process_id == active_process_id:
            return 0x00
        tmp_read_ppage_map_entry_vpage_number = self.read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x08)
        if tmp_read_ppage_map_entry_vpage_number == virtual_page_number:
            return 0x01
        return 0x00

    def lite_get_open_ppage(self):
        tmp_index = 0x00
        while tmp_index != PHYSICAL_MEMORY_PAGES:
            tmp_offset = tmp_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
            tmp_read_process_id = self.read_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_offset)
            if tmp_read_process_id == 0x00:
                return tmp_index
            tmp_index += 0x01
        raise Exception("No open pages (not yet implemented)")

    def lite_load_pte_to_ppage(self, process_map_entry_index, virtual_page_number, process_page_directory_physical_ppage, process_id, pte, ppage_index):
        tmp_ppage_map_offset = ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x00, process_id)
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x04, process_page_directory_physical_ppage)
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x08, virtual_page_number)
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x0C, 0x01)
        tmp_disk_block_number = pte & 0x000FFFFF
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x10, tmp_disk_block_number)
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + 0x14, 0x01)
        self.write_memory(MMIO_DISK_BASE_ADDR + 0x00, ppage_index)
        self.write_memory(MMIO_DISK_BASE_ADDR + 0x04, tmp_disk_block_number)
        self.write_memory(MMIO_DISK_BASE_ADDR + 0x08, 0x00)
        self.write_memory(MMIO_DISK_BASE_ADDR + 0x0C, 0x01)
        tmp_process_map_entry_offset = PROCESS_MAP_ADDR + (process_map_entry_index * PROCESS_MAP_ENTRY_SIZE)
        tmp_process_previous_resident_pages = self.read_memory(tmp_process_map_entry_offset + 0x08)
        self.write_memory(tmp_process_map_entry_offset + 0x08, tmp_process_previous_resident_pages + 0x01)
        tmp_pte_unset_number = pte & 0xFFF00000

        #TODO appears to be unused, looks like the new page number is not written to the PTE
        tmp_pte_new_number = tmp_pte_unset_number | ppage_index
        tmp_pte_unset_protection_bits = pte & 0x3FFFFFFF
        # likely fix
        # tmp_pte_unset_protection_bits = tmp_pte_new_number & 0x3FFFFFFF

        tmp_pte_rw_set = tmp_pte_unset_protection_bits | 0xC0000000
        return tmp_pte_rw_set

    def lite_set_ppage_field(self, value, offset, ppage_index):
        tmp_ppage_map_offset = ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE
        self.write_memory(PHYSICAL_PAGE_MAP_ADDR + tmp_ppage_map_offset + offset, value)


    def lite_set_pte_readable(self, pte):
        return pte | 0x80000000

    def lite_make_ppage_writable(self, ppage_index):
        tmp_ppage_map_offset = PHYSICAL_PAGE_MAP_ADDR + (ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
        tmp_ppage_dirty_field = self.read_memory(tmp_ppage_map_offset + 0x18)
        tmp_ppage_num_references = self.read_memory(tmp_ppage_map_offset + 0x0C)
        if tmp_ppage_dirty_field == 0x01:
            return ppage_index
        if not tmp_ppage_num_references == 0x01:
            raise Exception("Page is shared (not yet implemented)")
        self.write_memory(tmp_ppage_map_offset + 0x18, 0x01)
        return ppage_index

    def lite_set_pte_number(self, number, pte):
        tmp_unset_number = pte & 0xFFF00000
        return tmp_unset_number | number

    def lite_set_pte_writable(self, pte):
        return pte & 0xBFFFFFFF


    def page_fault_handler(self):
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

        return_location = self.read_register(Register.LINK)
        # store faulted PTE and faulted virtual address in local variables
        faulted_pte = self.read_mmu_register(MMURegister.FAULTED_PTE)
        faulted_va = self.read_mmu_register(MMURegister.FAULTED_VA)
        faulted_operation = self.read_mmu_register(MMURegister.FAULTED_OPERATION)

        # break the VA into its parts and store in local variables
        tmp_table_number_mask = 0xFFC00000
        faulted_va_table_num = (faulted_va & tmp_table_number_mask) >> 0x16

        tmp_virtual_page_number_mask = 0x003FF000
        faulted_va_vpage_num = (faulted_va & tmp_virtual_page_number_mask) >> 0x0C

        # get the active process's ID and page directory physical page number from kernel memory
        # and store them in local variables
        active_process_id = self.read_memory(ACTIVE_PROCESS_ID_ADDR)
        active_process_page_directory_ppage = self.read_memory(ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR)

        # store active process entry index in local variable
        (tmp_find_process_map_entry_success, tmp_find_process_map_entry_result) = self.lite_find_process_map_entry_index_by_id(active_process_id)
        if not tmp_find_process_map_entry_success:
            raise Exception("No process map entry found for active process")
        active_process_map_entry_index = tmp_find_process_map_entry_result

        # get PDE
        fetched_pde_kpa = self.lite_get_pte_kpa(faulted_va_table_num, active_process_page_directory_ppage)
        tmp_read_pde = self.read_memory(fetched_pde_kpa)
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


        virtual_table_ppage = self.lite_number_from_pte(fetched_pde)
        tmp_check_table_is_resident_result = self.lite_check_ppage_matches_vpage(active_process_id, faulted_va_table_num, virtual_table_ppage)

        # page table not resident
        if not tmp_check_table_is_resident_result == 0x01:
            virtual_table_ppage = self.lite_get_open_ppage()
            updated_pde = self.lite_load_pte_to_ppage(active_process_map_entry_index, faulted_va_table_num, active_process_page_directory_ppage, active_process_id, fetched_pde, virtual_table_ppage)

        # page table is resident
        # update physical page map to show page is referenced because we're reading this table now
        self.lite_set_ppage_field(0x01, 0x01, virtual_table_ppage)

        # set R to 1 on PDE since the table is loaded and referenced now
        updated_pde = self.lite_set_pte_readable(updated_pde)
        # also store updated PDE back into page directory
        self.write_memory(fetched_pde_kpa, updated_pde)

        # mark the directory ppage as dirty, since we updated the PDE
        # TODO only if the PDE was actually updated
        self.lite_set_ppage_field(0x01, 0x18, active_process_page_directory_ppage)

        # get PTE
        # calculate PTE physical address
        fetched_pte_kpa = self.lite_get_pte_kpa(faulted_va_vpage_num, virtual_table_ppage)
        tmp_read_pte = self.read_memory(fetched_pte_kpa)
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
        tmp_check_page_is_resident_result = self.lite_check_ppage_matches_vpage(active_process_id, faulted_va_vpage_num, tmp_read_pte)

        # page not resident
        if not tmp_check_page_is_resident_result == 0x01:
            # page is not resident, which means the number in the PTE actually represents
            # the disk block where the page was evicted to
            virtual_page_ppage = self.lite_get_open_ppage()
            updated_pte = self.lite_load_pte_to_ppage(active_process_map_entry_index, faulted_va_vpage_num, active_process_page_directory_ppage, active_process_id, fetched_pte, virtual_page_ppage)

        # page is resident
        # update physical page map to show ppage is referenced because we're accessing this page now
        self.lite_set_ppage_field(0x01, 0x14, virtual_page_ppage)

        # set R to 1 on PTE since the page is loaded and referenced
        updated_pte = self.lite_set_pte_readable(updated_pte)

        # if this was a write operation, make sure the page is writable
        if not faulted_operation == 0x00:
            # the operation was a write operation, so make the page writable
            tmp_w_bit_mask = 0x40000000
            tmp_page_w_bit = (updated_pte & tmp_w_bit_mask)
            if not tmp_page_w_bit == 0x00:
                # page is write protected
                tmp_new_writable_ppage_result = self.lite_make_ppage_writable(virtual_page_ppage)
                # set the PTE page number in case it changed
                tmp_updated_pte_result = self.lite_set_pte_number(tmp_new_writable_ppage_result, updated_pte)
                updated_pte = self.lite_set_pte_writable(tmp_updated_pte_result)

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
                virtual_table_ppage = self.lite_make_ppage_writable(virtual_table_ppage)

                # set updated PDE's number to new ppage
                tmp_set_pde_number_result = self.lite_set_pte_number(virtual_table_ppage, updated_pde)
                tmp_set_pde_writable_result = self.lite_set_pte_writable(tmp_set_pde_number_result)

                # write PDE back to page directory
                self.write_memory(fetched_pde_kpa, tmp_set_pde_writable_result)

                # recalculate PTE KPA
                # TODO load the page table ppage number into GPH
                #  (currently GPH holds the PDE entry, has the table ppage at the end, and
                #      so it happens to work when we shift the PDE left)
                fetched_pte_kpa = self.lite_get_pte_kpa(faulted_va_vpage_num, tmp_set_pde_number_result)

            # page table not write protected
            # store updated PTE into page table
            self.write_memory(fetched_pte_kpa, updated_pte)

        # PTE not dirty
        return

if __name__ == "__main__":
    # physical memory
    # 0     boot sequence
    # 1     kernel page directory
    # 2     kernel page table
    # 3     kernel program
    # 4     kernel program (cont'd)
    # 5     user page directory
    # 6     user page table
    # 7     user program
    # 8
    # ...   empty
    # 15

    environment = Environment(PHYSICAL_MEMORY_PAGES * 0x1000, VALID_DISK_BLOCKS)

    # set up kernel page directory
    kernel_page_directory_base_addr = 0x01 * 0x1000
    environment.write_memory(kernel_page_directory_base_addr + 0x00, 0xC0000002) # R !W (11), readable, write protected

    # set up kernel page table
    kernel_page_table_base_addr = 0x02 * 0x1000
    environment.write_memory(kernel_page_table_base_addr + 0x00 * 0x04, 0x80000003) # R W (10) (read and write allowed) physical page 3 (kernel program page 0)
    environment.write_memory(kernel_page_table_base_addr + 0x01 * 0x04, 0x80000004) # R W (10) (read and write allowed) physical page 4 (kernel program page 1)
    environment.write_memory(kernel_page_table_base_addr + 0x08 * 0x04, 0x800FF000) # R W (10) (read and write allowed) physical page 0xFF000 (MMIO devices)
    # map all physical pages to kernel VA starting at page number 0x3F0 (address 0xFC0 -- 0x3F0 * 0x04 bytes per entry)
    for i in range(0x10):
        environment.write_memory(kernel_page_table_base_addr + 0xFC0 + (i * 0x04), 0x80000000 + i)
        # 80 00 00 00
        # 80 00 00 01
        # 80 00 00 02...

    # set up kernel constants
    kernel_program_base_addr = 0x03 * 0x1000
    environment.write_memory(kernel_program_base_addr + ACTIVE_PROCESS_ID_ADDR, 0x02)
    environment.write_memory(kernel_program_base_addr + ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR, 0x05)

    # set up process map
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x00, 0x01)
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x04, 0x03)
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x08, 0x03)
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x10, 0x02)
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x14, 0x03)
    environment.write_memory(kernel_program_base_addr + PROCESS_MAP_ADDR + 0x18, 0x03)

    # set up physical page map
    def set_physical_page_map_entry(ppage, pid, owning_directory_ppage, vpage_num, num_references, disk_block, lru, dirty):
        physical_page_map_base_addr = kernel_program_base_addr + PHYSICAL_PAGE_MAP_ADDR
        entry_base_addr = physical_page_map_base_addr + (ppage * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
        vals = [pid, owning_directory_ppage, vpage_num, num_references, disk_block, lru, dirty]
        for i, v in enumerate(vals):
            environment.write_memory(entry_base_addr + (i * 0x04), v)
    set_physical_page_map_entry(1, 1, 1, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(2, 1, 1, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(3, 1, 1, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(4, 1, 1, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(5, 2, 5, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(6, 2, 5, 0, 1, 0, 0, 0)
    set_physical_page_map_entry(0x7, 0x2, 0x5, 0, 0x1, 0x64, 0, 0)



    # set up user page directory
    user_page_directory_base_addr = 0x05 * 0x1000
    environment.write_memory(user_page_directory_base_addr, 0xC0000006) # Readable, write protected (11) (resident, referenced, clean) physical page 6 (user page table 0)

    # set up user page table
    user_page_table_base_addr = 0x06 * 0x1000
    environment.write_memory(user_page_table_base_addr + 0x00, 0xC0000007) # R !W (11) (resident, referenced, clean) physical page 7 (user program page 0)
    environment.write_memory(user_page_table_base_addr + 0x04, 0x40000067) # !R W (01) (mapped, evicted) disk block 103 (user program page 1)


    ########################################
    # configure the MMU

    environment.mmu.FAULTED_ADDRESS = 0x1000
    environment.mmu.FAULTED_PTE = 0x40000067 # !R W (01) (mapped, evicted) disk block 103 (user program page 1)
    environment.mmu.FAULTED_OPERATION = 0x00
    environment.mmu.ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS = 0x01 # kernel page directory physical page number
    environment.mmu.ENABLED = 0x01
    environment.page_fault_handler()
    print(environment)
