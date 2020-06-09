import pytest
import os
import py
import numpy as np

from lib.base import Base, CLUSTERED_FILENAME_POSFIX, CLUSTER_NAME_COLUMN_LABEL

from .base_test_case import BaseTestCase


CSV_FILES_COUNT = 3

# The number of publications in the input test files.
PUBLICATION_COUNT = 25

# The number of columns in the test files that 
# represent citation counts before a tool was 
# added to the repository---excluding the year
# the tool was added (i.e., the 0 normalized date).
PRE_COL_COUNT = 10

# The number of columns in the tests files that
# represent citation counts after a tool was
# added to the repository---including the year
# the tool was added (i.e., the 0 normalized date).
POST_COL_COUNT = 11


class TestBase(BaseTestCase):

    @pytest.mark.parametrize(
        "input,expected", 
        [("name.csv", "name"),
         ("./a/path/name.csv", "name"),
         (f"./a/path/name{CLUSTERED_FILENAME_POSFIX}.csv", "name")])
    def test_get_repo_name(self, input, expected):
        """
        Asserts if the `get_repo_name` extracts repository name 
        from a given filename.
        """

        # Arrange, Act, Assert
        assert expected == Base.get_repo_name(input)


    def test_get_input_files(self, tmpdir):
        """
        Asserts if the `get_input_files` method reads only **input**
        (i.e., those without the cluster postfix) CSV files the given path.

        For instance, from a directory as the following, it should read 
        only `file_1.csv` and `file_2.csv`:

        ├─── file_1.csv
        ├─── file_2.csv
        ├─── file_1_clustered.csv
        └─── file_3.txt

        :type  tmpdir:  string
        :param tmpdir:  The ‘tmpdir’ fixture is a py.path.local object
                        which will provide a temporary directory unique 
                        to the test invocation.
        """

        # Arrange
        x = "content"
        for i in range(CSV_FILES_COUNT):
            tmpdir.join(f"file_{i}.csv").write(x)
        tmpdir.join(f"file_{i}{CLUSTERED_FILENAME_POSFIX}.csv").write(x)
        tmpdir.join(f"file_n.txt").write(x)

        # Act
        files = Base.get_input_files(tmpdir)

        # Assert
        assert len(files) == CSV_FILES_COUNT


    def test_get_input_files(self, tmpdir):
        """
        TODO: ... 
        """
        pass

    def test_get_citations(self, test_publications):
        """
        Asserts a correct selection of the headers of the columns 
        containing citation count before and after a tool was added
        to the repository.
        """
        # Arrange
        input = test_publications[0]

        # Act
        pre, post = Base.get_citations_headers(input)

        # Assert
        assert len(pre) == 2
        assert len(post) == 3

    def test_get_vectors(self, test_publications):
        """
        This test asserts if various vectors are correctly extracted 
        from a given dataframe of publications (which represents a
        repository). For instance, extract from the dataframe the 
        citations a publication received before its tool was added 
        to the repository.
        """
        # Arrange
        input = test_publications[0]
        expected = test_publications[1]

        # Act
        citations, pre, post, sums, avg_pre, avg_post, deltas = Base.get_vectors(input)

        # Assert
        assert BaseTestCase.assert_lists_equal(citations, expected["citations"])
        assert BaseTestCase.assert_lists_equal(pre, expected["pre"])
        assert BaseTestCase.assert_lists_equal(post, expected["post"])
        assert BaseTestCase.assert_lists_equal(sums, expected["sums"])
        assert BaseTestCase.assert_lists_equal(avg_pre, expected["avg_pre"])
        assert BaseTestCase.assert_lists_equal(avg_post, expected["avg_post"])
        assert BaseTestCase.assert_lists_equal(deltas, expected["deltas"])

    def test_get_sorted_clusters(self, test_publications):

        # Arrange
        input = test_publications[0].groupby(CLUSTER_NAME_COLUMN_LABEL)
        expected = test_publications[1]
        exp_mapping = expected["cluster_avg"]
        exp_keys = sorted(exp_mapping.values())

        # Act
        keys, mapping = Base.get_sorted_clusters(input)

        # Assert
        assert BaseTestCase.assert_lists_equal(keys, exp_keys)
        assert BaseTestCase.assert_lists_equal(list(mapping.values()),
                                               list(exp_mapping.keys()))
