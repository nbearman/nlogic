import unittest
from unittest.mock import MagicMock, patch

from OSSimulator import (
    PHYSICAL_PAGE_MAP_ADDR,
    PROCESS_MAP_ADDR,
    Environment,
    PHYSICAL_MEMORY_PAGES,
    PROCESS_MAP_ENTRY_SIZE,
    PROCESS_MAP_LENGTH,
    PHYSICAL_PAGE_MAP_ENTRY_SIZE,
)

class TestLiteCheckPPageMatchesVPage(unittest.TestCase):
    @unittest.skip("BUG: off by one when checking if page is inbounds")
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
    def test_no_open_ppage_raises_exception(self, **kwargs):
        env = Environment()
        for i in range(PHYSICAL_MEMORY_PAGES):
            offset = PHYSICAL_PAGE_MAP_ADDR + (PHYSICAL_PAGE_MAP_ENTRY_SIZE * i) + 0x00 # offset to process ID field
            env.write_memory(offset, 1)
        with self.assertRaises(Exception):
            env.lite_get_open_ppage()

    def test_returns_first_open_ppage(self, **kwargs):
        env = Environment()
        target_ppage = 0x03
        for i in range(PHYSICAL_MEMORY_PAGES):
            offset = PHYSICAL_PAGE_MAP_ADDR + (PHYSICAL_PAGE_MAP_ENTRY_SIZE * i) + 0x00 # offset to process ID field
            env.write_memory(offset, 1)
        target_ppage_offset = PHYSICAL_PAGE_MAP_ADDR + (PHYSICAL_PAGE_MAP_ENTRY_SIZE * target_ppage)
        env.write_memory(target_ppage_offset, 0x00)

        result = env.lite_get_open_ppage()
        assert result == target_ppage, f"0x{result:08X}, expected 0x{target_ppage:08X}"

class TestLoadLitePteToPpage(unittest.TestCase):
    fake_ppage_map_addr = 0x0000A000
    fake_mmio_disk_addr = 0x0000B000
    fake_process_map_addr = 0x0000C000

    @unittest.skip("BUG: the PTE's page number is not updated with the new ppage")
    def test_returns_new_pte(self):
        process_map_entry_index = 0x01
        virtual_page_number = 0x0123
        process_page_directory_physical_ppage = 0x02
        process_id = 0x0A
        pte = 0x11AAAAAAA
        ppage_index = 0xEDCBA
        expected_updated_pte = 0x00000000
        env = Environment()
        result = env.lite_load_pte_to_ppage(
            process_map_entry_index,
            virtual_page_number,
            process_page_directory_physical_ppage,
            process_id,
            pte,
            ppage_index
        )
        expected_updated_pte = 0xDAAEDCBA
        assert result == expected_updated_pte, f"0x{result:08X}, expected 0x{expected_updated_pte:08X}"

    @patch("OSSimulator.PHYSICAL_PAGE_MAP_ADDR", new=fake_ppage_map_addr)
    def test_updates_ppage_map1(self):

        process_map_entry_index = 0x01
        virtual_page_number = 0x0123
        process_page_directory_physical_ppage = 0x02
        process_id = 0x0A
        pte = 0x87654321
        ppage_index = 0x0E
        env = Environment()
        env.lite_load_pte_to_ppage(
            process_map_entry_index,
            virtual_page_number,
            process_page_directory_physical_ppage,
            process_id,
            pte,
            ppage_index
        )

        ppage_entry_offset = self.fake_ppage_map_addr + (ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
        assert env.read_memory(ppage_entry_offset + 0x00) == process_id
        assert env.read_memory(ppage_entry_offset + 0x04) == process_page_directory_physical_ppage
        assert env.read_memory(ppage_entry_offset + 0x08) == virtual_page_number
        assert env.read_memory(ppage_entry_offset + 0x0C) == 0x01
        assert env.read_memory(ppage_entry_offset + 0x10) == 0x00054321
        assert env.read_memory(ppage_entry_offset + 0x14) == 0x01

    @patch("OSSimulator.MMIO_DISK_BASE_ADDR", new=fake_mmio_disk_addr)
    def test_updates_ppage_map2(self):
        process_map_entry_index = 0x01
        virtual_page_number = 0x0123
        process_page_directory_physical_ppage = 0x02
        process_id = 0x0A
        pte = 0x87654321
        ppage_index = 0x0E
        env = Environment()
        env.lite_load_pte_to_ppage(
            process_map_entry_index,
            virtual_page_number,
            process_page_directory_physical_ppage,
            process_id,
            pte,
            ppage_index
        )

        assert env.read_memory(self.fake_mmio_disk_addr + 0x00) == ppage_index
        assert env.read_memory(self.fake_mmio_disk_addr + 0x04) == 0x00054321
        assert env.read_memory(self.fake_mmio_disk_addr + 0x08) == 0x00
        assert env.read_memory(self.fake_mmio_disk_addr + 0x0C) == 0x01

    def test_updates_ppage_map3(self):
        env = Environment()

        process_map_entry_index = 0x01
        virtual_page_number = 0x0123
        process_page_directory_physical_ppage = 0x02
        process_id = 0x0A
        pte = 0x87654321
        ppage_index = 0x0E

        # set a fake resident page number
        process_map_offset = (
            PROCESS_MAP_ADDR
            + (process_map_entry_index * PROCESS_MAP_ENTRY_SIZE)
            + 0x08
        )
        existing_resident_pages = 0x00001111
        env.write_memory(process_map_offset, existing_resident_pages)

        env.lite_load_pte_to_ppage(
            process_map_entry_index,
            virtual_page_number,
            process_page_directory_physical_ppage,
            process_id,
            pte,
            ppage_index
        )

        expected_resident_pages = existing_resident_pages + 1
        new_resident_pages = env.read_memory(process_map_offset)
        assert new_resident_pages == expected_resident_pages, f"{new_resident_pages:08X}, expected 0x{expected_resident_pages:08X}"

class TestLiteSetPpageField(unittest.TestCase):
    fake_ppage_map_addr = 0x0000A000

    @patch("OSSimulator.PHYSICAL_PAGE_MAP_ADDR", new=fake_ppage_map_addr)
    @patch("OSSimulator.Environment.write_memory")
    def test_sets_value_in_ppage_map(self, mock_write_memory):
        value = 0x12345678
        offset = 0x0C
        index = 0x321
        env = Environment()
        env.lite_set_ppage_field(value, offset, index)
        expected_addr = (index * PHYSICAL_PAGE_MAP_ENTRY_SIZE) + offset + self.fake_ppage_map_addr
        mock_write_memory.assert_called_once_with(expected_addr, value)

class TestLiteSetPteReadable(unittest.TestCase):
    def test_sets_first_bit(self):
        pte = 0b0100_0000_0000_0000_0000_0000_0000_0000
        env = Environment()
        result = env.lite_set_pte_readable(pte)
        expected = 0b1100_0000_0000_0000_0000_0000_0000_0000
        assert result == expected, f"0x{result:08X}, expected 0x{expected:08X}"

class TestLiteSetPteWritable(unittest.TestCase):
    def test_unsets_second_bit(self):
        pte = 0b1110_0000_0000_0000_0000_0000_0000_0000
        env = Environment()
        result = env.lite_set_pte_writable(pte)
        expected = 0b1010_0000_0000_0000_0000_0000_0000_0000
        assert result == expected, f"0x{result:08X}, expected 0x{expected:08X}"

class TestLiteSetPteNumber(unittest.TestCase):
    def test_sets_last_twenty_bits(self):
        pte = 0b0100_0000_0000_0000_0010_1111_1010_0110
        number = 0b1110_0010_0110_0101_1011
        env = Environment()
        result = env.lite_set_pte_number(number, pte)
        expected = 0b0100_0000_0000_1110_0010_0110_0101_1011
        assert result == expected, f"0x{result:08X}, expected 0x{expected:08X}"

class TestLiteMakePpageWritable(unittest.TestCase):
    def test_returns_index_if_page_already_dirty(self):
        env = Environment()
        ppage_index = 0x03
        num_references = 0x01
        ppage_entry_offset = PHYSICAL_PAGE_MAP_ADDR + (ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
        env.write_memory(ppage_entry_offset + 0x18, 0x01) # set dirty field
        env.write_memory(ppage_entry_offset + 0x0C, num_references) # set num references field

        mock_write = MagicMock()
        env.write_memory = mock_write
        result = env.lite_make_ppage_writable(ppage_index)
        assert result == ppage_index
        mock_write.assert_not_called()

    def test_sets_dirty_field_when_page_is_clean(self):
        env = Environment()
        ppage_index = 0x03
        num_references = 0x01
        ppage_entry_offset = PHYSICAL_PAGE_MAP_ADDR + (ppage_index * PHYSICAL_PAGE_MAP_ENTRY_SIZE)
        env.write_memory(ppage_entry_offset + 0x18, 0x00) # set dirty field to 0
        env.write_memory(ppage_entry_offset + 0x0C, num_references) # set num references field

        result = env.lite_make_ppage_writable(ppage_index)
        assert result == ppage_index
        new_dirty = env.read_memory(ppage_entry_offset + 0x18)
        expected_dirty = 0x01
        assert new_dirty == expected_dirty, f"0x{new_dirty:08X}, expected 0x{expected_dirty:08X}"


if __name__ == '__main__':
    unittest.main()
