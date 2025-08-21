from dataclasses import dataclass
from unittest import TestCase, main

from OSSimulator import (
    ACTIVE_PROCESS_ID_ADDR,
    ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR,
    PAGE_SIZE,
    PHYSICAL_MEMORY_PAGES,
    PHYSICAL_PAGE_MAP_ADDR,
    PHYSICAL_PAGE_MAP_ENTRY_SIZE,
    PROCESS_MAP_ADDR,
    PROCESS_MAP_ENTRY_SIZE,
    VALID_DISK_BLOCKS,
    Environment
)

@dataclass
class PTE:
    ppage: int|None # physical page, if resident
    block: int|None # disk block number, if nonresident
    readable: bool  # R bit
    write_protected: bool # W bit

    def to_int(self):
        r = self.readable << 31
        w = self.write_protected << 30
        return r | w | (self.ppage if self.ppage is not None else self.block)


@dataclass
class PDE:
    ppage: int|None # physical page, if resident
    block: int|None # disk block number, if nonresident
    readable: bool  # R bit
    write_protected: bool # W bit
    page_table: dict[int, PTE]

    def to_int(self):
        r = int(self.readable) << 31
        w = int(self.write_protected) << 30
        return r | w | (self.ppage if self.ppage is not None else self.block)


@dataclass
class PPageMapEntry:
    process_id: int
    directory_physical_page: int
    virtual_page_number: int
    number_of_references: int
    disk_block_number: int
    lru_referenced: bool
    dirty: bool


@dataclass
class ProcessMapEntry:
    process_id: int
    num_mapped_pages: int
    num_resident_pages: int
    owning_process_directory_disk_block: int

class TestPageFaultNew(TestCase):
    def setUp(self):
        self.environment = Environment(PHYSICAL_MEMORY_PAGES * 0x1000, VALID_DISK_BLOCKS)
        return super().setUp()

    def build_pages_on_disk_from_page_directory(
        self,
        disk_pages_by_block: dict[int, list[int]],
        page_directory_disk_block, page_directory: dict[int, PDE]
    ):
        directory_page = [0] * 0x1000
        for table_num, pde in page_directory.items():
            pde_offset = table_num * 0x04
            directory_page[pde_offset:pde_offset + 0x04] = pde.to_int().to_bytes(0x04, "big")
            table_page = [0] * 0x1000
            for page_num, pte in pde.page_table.items():
                pte_offset = page_num * 0x04
                value = pte.to_int().to_bytes(0x04, "big")
                table_page[pte_offset:pte_offset + 0x04] = value
            if len(table_page) != 0x1000:
                raise ValueError(f"Page must contain exactly 0x1000 bytes, got {len(table_page)}")
            disk_pages_by_block[pde.block] = table_page
        if len(directory_page) != 0x1000:
            raise ValueError(f"Page must contain exactly 0x1000 bytes, got {len(directory_page)}")
        disk_pages_by_block[page_directory_disk_block] = directory_page
        return disk_pages_by_block

    def set_physical_page_map(self, physical_page_map_base_addr, ppage_map: list[PPageMapEntry]):
            for i, ppage in enumerate(ppage_map):
                entry_base_addr = physical_page_map_base_addr + (i * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
                vals = [
                    ppage.process_id, ppage.directory_physical_page, ppage.virtual_page_number,
                    ppage.number_of_references, ppage.disk_block_number, ppage.lru_referenced, ppage.dirty
                ]
                for i, v in enumerate(vals):
                    addr = entry_base_addr + (i * 0x04)
                    self.environment.memory[addr:addr + 0x04] = v.to_bytes(0x04, "big")


    def set_process_map(self, process_map_base_addr: int, process_map: list[ProcessMapEntry]):
        for i, entry in enumerate(process_map):
            entry_addr = process_map_base_addr + (i * PROCESS_MAP_ENTRY_SIZE)
            self.environment.memory[entry_addr + 0x00:entry_addr + 0x04] = entry.process_id.to_bytes(0x04, "big")
            self.environment.memory[entry_addr + 0x04:entry_addr + 0x08] = entry.num_mapped_pages.to_bytes(0x04, "big")
            self.environment.memory[entry_addr + 0x08:entry_addr + 0x0C] = entry.num_resident_pages.to_bytes(0x04, "big")
            self.environment.memory[entry_addr + 0x0C:entry_addr + 0x10] = entry.owning_process_directory_disk_block.to_bytes(0x04, "big")


    def copy_page_to_physical_page(self, ppage: int, page: list[int]):
            for i in range(0x1000):
                self.environment.memory[(ppage * 0x1000) + i] = page[i]


    def test_read_from_unmapped_page_with_open_physical_page(self):
        self.environment.name = "test_read_from_unmapped_page_with_open_physical_page"
        KERNEL_PAGE_DIR_BLOCK = 0x40
        KERNEL_PAGE_TABLE_BLOCK = 0x41
        KERNEL_PAGE_TABLE_PPAGE = 0x02
        USER_PAGE_DIR_BLOCK = 0x64
        USER_PAGE_TABLE_BLOCK = 0x65
        USER_PAGE_TABLE_PPAGE = 0x06

        KERNEL_PID = 1
        USER_PID = 2

        KERNEL_DIR_PPAGE = 0x01
        KERNEL_PROGRAM_PPAGE = 0x03
        USER_DIR_PPAGE = 0x05
        USER_PROGRAM_PPAGE = 0x07

        MMIO_PPAGE = 0xFF000

        kernel_program_base_addr = KERNEL_PROGRAM_PPAGE * 0x1000

        kernel_page_directory = {
            0: PDE(KERNEL_PAGE_TABLE_PPAGE, KERNEL_PAGE_TABLE_BLOCK, True, True, {
                0x00: PTE(KERNEL_PROGRAM_PPAGE, None, True, False),
                0x01: PTE(KERNEL_PROGRAM_PPAGE + 0x01, None, True, False),
                0x08: PTE(MMIO_PPAGE, None, True, False),

                0x3F0: PTE(0x00, None, True, False),
                0x3F1: PTE(0x01, None, True, False),
                0x3F2: PTE(0x02, None, True, False),
                0x3F3: PTE(0x03, None, True, False),
                0x3F4: PTE(0x04, None, True, False),
                0x3F5: PTE(0x05, None, True, False),
                0x3F6: PTE(0x06, None, True, False),
                0x3F7: PTE(0x07, None, True, False),
                0x3F8: PTE(0x08, None, True, False),
                0x3F9: PTE(0x09, None, True, False),
                0x3FA: PTE(0x0A, None, True, False),
                0x3FB: PTE(0x0B, None, True, False),
                0x3FC: PTE(0x0C, None, True, False),
                0x3FD: PTE(0x0D, None, True, False),
                0x3FE: PTE(0x0E, None, True, False),
                0x3FF: PTE(0x0F, None, True, False),
            }),
        }

        user_page_directory = {
            0: PDE(USER_PAGE_TABLE_PPAGE, USER_PAGE_TABLE_BLOCK, True, True, {
                0x00: PTE(USER_PROGRAM_PPAGE, None, True, True),
                0x01: PTE(None, 0x67, False, True),
            }),
        }

        physical_page_map = [
            PPageMapEntry(0, 0, 0, 0, 0, 0, 0),
            PPageMapEntry(KERNEL_PID, KERNEL_DIR_PPAGE,   0x00, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(KERNEL_PID, KERNEL_DIR_PPAGE,   0x00, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(KERNEL_PID, KERNEL_DIR_PPAGE,   0x00, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(KERNEL_PID, KERNEL_DIR_PPAGE,   0x01, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(USER_PID,   USER_DIR_PPAGE,     0x00, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(USER_PID,   USER_DIR_PPAGE,     0x00, 0x01, 0x00, 0x00, 0x00),
            PPageMapEntry(USER_PID,   USER_DIR_PPAGE,     0x00, 0x01, 0x64, 0x00, 0x00),
        ]

        process_map = [
            ProcessMapEntry(KERNEL_PID, 0x03, 0x03, 0x00), # kernel
            ProcessMapEntry(USER_PID,   0x02, 0x03, USER_PAGE_DIR_BLOCK), # user program
        ]

        disk = {}
        self.build_pages_on_disk_from_page_directory(disk, KERNEL_PAGE_DIR_BLOCK, kernel_page_directory)
        self.build_pages_on_disk_from_page_directory(disk, USER_PAGE_DIR_BLOCK, user_page_directory)

        self.copy_page_to_physical_page(KERNEL_DIR_PPAGE, disk[KERNEL_PAGE_DIR_BLOCK])
        self.copy_page_to_physical_page(KERNEL_PAGE_TABLE_PPAGE, disk[KERNEL_PAGE_TABLE_BLOCK])
        self.copy_page_to_physical_page(USER_DIR_PPAGE, disk[USER_PAGE_DIR_BLOCK])
        self.copy_page_to_physical_page(USER_PAGE_TABLE_PPAGE, disk[USER_PAGE_TABLE_BLOCK])

        physical_page_map_base_addr = kernel_program_base_addr + PHYSICAL_PAGE_MAP_ADDR
        self.set_physical_page_map(physical_page_map_base_addr, physical_page_map)

        process_map_base_addr = kernel_program_base_addr + PROCESS_MAP_ADDR
        self.set_process_map(process_map_base_addr, process_map)

        # set up kernel constants
        self.environment.write_memory(kernel_program_base_addr + ACTIVE_PROCESS_ID_ADDR, 0x02)
        self.environment.write_memory(kernel_program_base_addr + ACTIVE_PROCESS_PAGE_DIRECTORY_PHYSICAL_PAGE_ADDR, 0x05)

        self.environment.mmu.FAULTED_ADDRESS = 0x1000
        self.environment.mmu.FAULTED_PTE = 0x40000067 # !R W (01) (mapped, evicted) disk block 103 (user program page 1)
        self.environment.mmu.FAULTED_OPERATION = 0x00
        self.environment.mmu.ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS = 0x01 # kernel page directory physical page number
        self.environment.mmu.ENABLED = 0x01

        # the first page should start empty
        for i in range(PAGE_SIZE):
            self.assertEqual(self.environment.memory[i], 0x00)

        self.environment.page_fault_handler()

        # after the page fault handler runs, the nonresident page should have been moved into the first phsyical page
        expected_values = [0x12, 0x96, 0x13, 0x95, 0x7B, 0x7B, 0x01, 0x91, 0x9D, 0x92, 0x9F]
        for i, v in enumerate(expected_values):
            self.assertEqual(self.environment.memory[i], v)


if __name__ == "__main__":
    main()