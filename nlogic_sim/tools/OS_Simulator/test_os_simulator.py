import unittest
from unittest.mock import patch

from OSSimulator import (
    lite_check_ppage_matches_vpage,
    lite_find_process_map_entry_index_by_id,
    lite_get_pte_kpa,
    lite_number_from_pte,
    lite_get_open_ppage,
    PHYSICAL_MEMORY_PAGES,
    PROCESS_MAP_ENTRY_SIZE,
    PROCESS_MAP_LENGTH,
    PHYSICAL_PAGE_MAP_ENTRY_SIZE,
)

class ProcessMapTestCase(unittest.TestCase):
    fake_process_map_entry_addr = 0x0000A000

    def set_process_ids(self, process_ids_list):
        for i, process_id in enumerate(process_ids_list):
            self.write_memory(self.fake_process_map_entry_addr + (i * PROCESS_MAP_ENTRY_SIZE), process_id)

    def setUp(self):
        self.memory = bytearray(PHYSICAL_MEMORY_PAGES * 0x1000)
        self.read_memory = lambda addr: int.from_bytes(self.memory[addr:addr + 4], "big")
        def write_memory(addr, data):
            self.memory[addr:addr + 4] = data.to_bytes(4, "big")
        self.write_memory = write_memory

    def ProcessMapTest(F):
        @patch("OSSimulator.PROCESS_MAP_ADDR", new=ProcessMapTestCase.fake_process_map_entry_addr)
        @patch("OSSimulator.read_memory")
        def wrapper(self, mock_read_memory):
            mock_read_memory.side_effect = self.read_memory
            F(self)
        return wrapper

class PhysicalPageMapTestCase(unittest.TestCase):
    fake_ppage_map_entry_addr = 0x0000A000

    def set_process_ids(self, process_ids_list):
        for i, process_id in enumerate(process_ids_list):
            self.write_memory(self.fake_ppage_map_entry_addr + (i * PHYSICAL_PAGE_MAP_ENTRY_SIZE), process_id)

    def setUp(self):
        self.memory = bytearray(PHYSICAL_MEMORY_PAGES * 0x1000)
        self.read_memory = lambda addr: int.from_bytes(self.memory[addr:addr + 4], "big")
        def write_memory(addr, data):
            self.memory[addr:addr + 4] = data.to_bytes(4, "big")
        self.write_memory = write_memory

    def ProcessMapTest(F):
        @patch("OSSimulator.PHYSICAL_PAGE_MAP_ADDR", new=PhysicalPageMapTestCase.fake_ppage_map_entry_addr)
        @patch("OSSimulator.read_memory")
        def wrapper(self, mock_read_memory):
            mock_read_memory.side_effect = self.read_memory
            F(self)
        return wrapper

class ProcessMapTestCase(unittest.TestCase):
    fake_process_map_entry_addr = 0x0000A000

    def set_process_ids(self, process_ids_list):
        for i, process_id in enumerate(process_ids_list):
            self.write_memory(self.fake_process_map_entry_addr + (i * PROCESS_MAP_ENTRY_SIZE), process_id)

    def setUp(self):
        self.memory = bytearray(PHYSICAL_MEMORY_PAGES * 0x1000)
        self.read_memory = lambda addr: int.from_bytes(self.memory[addr:addr + 4], "big")
        def write_memory(addr, data):
            self.memory[addr:addr + 4] = data.to_bytes(4, "big")
        self.write_memory = write_memory

    def ProcessMapTest(F):
        @patch("OSSimulator.PROCESS_MAP_ADDR", new=ProcessMapTestCase.fake_process_map_entry_addr)
        @patch("OSSimulator.read_memory")
        def wrapper(self, mock_read_memory):
            mock_read_memory.side_effect = self.read_memory
            F(self)
        return wrapper

class TestLiteCheckPPageMatchesVPage(unittest.TestCase):
    @unittest.skip("BUG: off by one when checking if page is inbounds")
    @patch("OSSimulator.read_memory")
    def test_returns_false_when_ppage_out_of_range(self, mock_read_memory):
        # should not be called because we don't need to read memory to determine
        # that the page is out of range
        mock_read_memory.side_effect = AssertionError("Should not be called")
        result = lite_check_ppage_matches_vpage(0, 0, PHYSICAL_MEMORY_PAGES)
        assert result == 0x00

    @patch("OSSimulator.read_memory")
    def test_returns_false_when_owner_doesnt_match(self, mock_read_memory):
        process_id = 2
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id + 1,
            # second read is vpage number
            AssertionError("Should not be called"),
        ]
        result = lite_check_ppage_matches_vpage(process_id, 0, 0)
        assert result == 0x00

    @patch("OSSimulator.read_memory")
    def test_returns_false_when_vpage_doesnt_match(self, mock_read_memory):
        process_id = 2
        vpage = 3
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id,
            # second read is vpage number
            vpage + 1,
        ]
        result = lite_check_ppage_matches_vpage(process_id, vpage, 0)
        assert result == 0x00

    @patch("OSSimulator.read_memory")
    def test_returns_true_when_owner_and_vpage_match(self, mock_read_memory):
        process_id = 2
        vpage = 3
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id,
            # second read is vpage number
            vpage,
        ]
        result = lite_check_ppage_matches_vpage(process_id, vpage, 0)
        assert result == 0x01

class TestLiteFindProcessMapEntryIndexById(ProcessMapTestCase):
    @ProcessMapTestCase.ProcessMapTest
    def test_returns_false_when_no_processes_match(self):
        process_id = 8
        self.set_process_ids([process_id + 1] * PROCESS_MAP_LENGTH)
        (found, index) = lite_find_process_map_entry_index_by_id(process_id)
        assert found == 0x00

    @ProcessMapTestCase.ProcessMapTest
    def test_returns_true_and_first_matched_process(self):
        process_id = 8
        target_index = 4
        entry_ids = [process_id + 1] * PROCESS_MAP_LENGTH
        entry_ids[target_index] = process_id
        entry_ids[target_index + 1] = process_id

        self.set_process_ids(entry_ids)
        (found, index) = lite_find_process_map_entry_index_by_id(process_id)
        assert found == 0x01
        assert index == target_index

class TestLiteGetPteKpa(unittest.TestCase):
    def test_returns_correct_kpa(self):
        test_cases = [
            (0x00, 0x00, 0x003F0000),
            (0x00, 0x01, 0b00000000000000000001000000000000 + 0x003F0000),
            (0x01, 0x02, 0b00000000000000000010000000000100 + 0x003F0000),
            (0x22, 0x03, 0b00000000000000000011000010001000 + 0x003F0000),
        ]

        for (vpage_number, table_ppage_number, expected) in test_cases:
            result = lite_get_pte_kpa(vpage_number, table_ppage_number)
            assert result == expected, f"(0x{vpage_number:08X}, 0x{table_ppage_number:08X}); was 0x{result:08X}, expected 0x{expected:08X}"

class TestLiteNumberFromPte(unittest.TestCase):
    def test_returns_number_from_pte(self):
        test_cases = [
            (0x00000000, 0x00000),
            (0x00000001, 0x00001),
            (0x0000ABCD, 0x0ABCD),
            (0x98765432, 0x65432),
            (0x987A0000, 0xA0000),
            (0xFFF00000, 0x00000),
        ]
        for (pte, expected) in test_cases:
            result = lite_number_from_pte(pte)
            assert result == expected, f"0x{pte:08X} -> 0x{result:08X}, expected 0x{expected:08X}"

class TestGetOpenPpage(PhysicalPageMapTestCase):
    @PhysicalPageMapTestCase.ProcessMapTest
    def test_no_open_ppage_raises_exception(self):
        self.set_process_ids([0x01] * PHYSICAL_MEMORY_PAGES)
        with self.assertRaises(Exception):
            lite_get_open_ppage()

    @PhysicalPageMapTestCase.ProcessMapTest
    def test_returns_first_open_ppage(self):
        process_ids = [0x01] * PHYSICAL_MEMORY_PAGES
        target_ppage = 0x03
        process_ids[target_ppage] = 0x00
        process_ids[target_ppage + 1] = 0x00
        self.set_process_ids(process_ids)

        result = lite_get_open_ppage()
        assert result == target_ppage, f"0x{result:08X}, expected 0x{target_ppage:08X}"


if __name__ == '__main__':
    unittest.main()
