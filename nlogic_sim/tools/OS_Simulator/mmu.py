from dataclasses import dataclass
from enum import Enum


@dataclass
class MMU:
    environment_memory: list[int] | bytearray

    # TODO the way this is used, it's actually expected to be the physical page number of the page directory
    ACTIVE_PAGE_DIRECTORY_BASE_ADDRESS: int = 0
    QUEUED_PAGE_DIRECTORY_BASE_ADDRESS: int = 0

    MMU_DIRECTORY_SWAP_BREAKPOINT: int = 0
    FAULTED_PTE: int = 0
    FAULTED_ADDRESS: int = 0
    BREAKPOINT_ENABLED: int = 0
    ENABLED: int = 0
    BREAKPOINT_CYCLE_DELAY_COUNTER: int = 0
    FAULTED_OPERATION: int = 0

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
