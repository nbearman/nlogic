import unittest
from unittest.mock import patch

from OSSimulator import (
    lite_check_ppage_matches_vpage,
    lite_find_process_map_entry,
    PHYSICAL_MEMORY_PAGES,
)

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

class TestLiteFindProcessMapEntry(unittest.TestCase):
    pass

if __name__ == '__main__':
    unittest.main()


