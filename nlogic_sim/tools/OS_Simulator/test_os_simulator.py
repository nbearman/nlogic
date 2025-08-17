import unittest
from unittest.mock import patch

from OSSimulator import (
    PHYSICAL_MEMORY_PAGES,
    PROCESS_MAP_ENTRY_SIZE,
    PROCESS_MAP_LENGTH,
    PROCESS_MAP_ADDR,
    PHYSICAL_PAGE_MAP_ENTRY_SIZE,
    PHYSICAL_PAGE_MAP_ADDR,
    Environment,
)

class TestLiteCheckPPageMatchesVPage(unittest.TestCase):
    @unittest.skip("TODO: BUG: off by one when checking if page is inbounds")
    @patch("OSSimulator.Environment.read_memory")
    def test_returns_false_when_ppage_out_of_range(self, mock_read_memory):
        # should not be called because we don't need to read memory to determine
        # that the page is out of range
        mock_read_memory.side_effect = AssertionError("Should not be called")
        env = Environment()
        result = env.lite_check_ppage_matches_vpage(0, 0, PHYSICAL_MEMORY_PAGES)
        assert result == 0x00

    @patch("OSSimulator.Environment.read_memory")
    def test_returns_false_when_owner_doesnt_match(self, mock_read_memory):
        process_id = 2
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id + 1,
            # second read is vpage number
            AssertionError("Should not be called"),
        ]
        env = Environment()
        result = env.lite_check_ppage_matches_vpage(process_id, 0, 0)
        assert result == 0x00

    @patch("OSSimulator.Environment.read_memory")
    def test_returns_false_when_vpage_doesnt_match(self, mock_read_memory):
        process_id = 2
        vpage = 3
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id,
            # second read is vpage number
            vpage + 1,
        ]
        env = Environment()
        result = env.lite_check_ppage_matches_vpage(process_id, vpage, 0)
        assert result == 0x00

    @patch("OSSimulator.Environment.read_memory")
    def test_returns_true_when_owner_and_vpage_match(self, mock_read_memory):
        process_id = 2
        vpage = 3
        mock_read_memory.side_effect = [
            # first read is ppage owner (process ID)
            process_id,
            # second read is vpage number
            vpage,
        ]
        env = Environment()
        result = env.lite_check_ppage_matches_vpage(process_id, vpage, 0)
        assert result == 0x01

class TestLiteFindProcessMapEntryIndexById(unittest.TestCase):
    def test_returns_false_when_no_processes_match(self):
        process_id = 8
        env = Environment()
        (found, index) = env.lite_find_process_map_entry_index_by_id(process_id)
        assert found == 0x00

    def test_returns_true_and_first_matched_process(self):
        process_id = 8
        target_index = 4
        process_id_offset = 0x00

        env = Environment()

        for i in range(PROCESS_MAP_LENGTH):
            offset = i * PROCESS_MAP_ENTRY_SIZE
            pid = 0x01 if i != target_index else process_id
            env.write_memory(PROCESS_MAP_ADDR + offset + process_id_offset, pid)

        (found, index) = env.lite_find_process_map_entry_index_by_id(process_id)
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
            env = Environment()
            result = env.lite_get_pte_kpa(vpage_number, table_ppage_number)
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
            env = Environment()
            result = env.lite_number_from_pte(pte)
            assert result == expected, f"0x{pte:08X} -> 0x{result:08X}, expected 0x{expected:08X}"

class TestGetOpenPpage(unittest.TestCase):
    def test_no_open_ppage_raises_exception(self):
        env = Environment()
        for i in range(PHYSICAL_MEMORY_PAGES):
            offset = i * PHYSICAL_PAGE_MAP_ENTRY_SIZE
            process_id_offset = 0x00
            env.write_memory(PHYSICAL_PAGE_MAP_ADDR + offset + process_id_offset, 0x01)
        with self.assertRaises(Exception):
            env.lite_get_open_ppage()

    def test_returns_first_open_ppage(self):
        env = Environment()

        expected_first_free_ppage = 0x03
        for i in range(expected_first_free_ppage):
            offset = i * PHYSICAL_PAGE_MAP_ENTRY_SIZE
            process_id_offset = 0x00
            env.write_memory(PHYSICAL_PAGE_MAP_ADDR + offset + process_id_offset, 0x01)
        result = env.lite_get_open_ppage()
        assert result == expected_first_free_ppage, f"0x{result:08X}, expected 0x{expected_first_free_ppage:08X}"


if __name__ == '__main__':
    unittest.main()
