from enum import Enum
import os

from OSSimulator import PAGE_SIZE



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