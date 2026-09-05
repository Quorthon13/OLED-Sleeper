using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Storage
{
    public class StorageRootPreparerTests
    {
        private const string StoreDirectory = @"C:\FakeAppData\OLED-Sleeper";

        private readonly FakeFileSystem _fileSystem;
        private readonly StorageRootPreparer _preparer;

        public StorageRootPreparerTests()
        {
            _fileSystem = new FakeFileSystem();
            _preparer = new StorageRootPreparer(_fileSystem, new StorageRoot(_fileSystem, isPortable: false));
        }

        [Fact]
        public void TryPrepare_WhenTheDirectoryAcceptsAWrite_SucceedsAndCreatesIt()
        {
            // Act
            var prepared = _preparer.TryPrepare(out var error);

            // Assert
            Assert.True(prepared);
            Assert.Null(error);
            Assert.Equal(new[] { StoreDirectory }, _fileSystem.CreatedDirectories);
        }

        [Fact]
        public void TryPrepare_WhenItSucceeds_LeavesNoProbeFileBehind()
        {
            // Act
            _preparer.TryPrepare(out _);

            // Assert
            Assert.Null(_fileSystem.Contents(StoreDirectory + @"\.write-probe"));
        }

        [Fact]
        public void TryPrepare_WhenTheDirectoryCannotBeCreated_FailsAndNamesIt()
        {
            // Arrange
            _fileSystem.DirectoryCreationFails = true;

            // Act
            var prepared = _preparer.TryPrepare(out var error);

            // Assert
            Assert.False(prepared);
            Assert.Contains(StoreDirectory, error);
        }

        [Fact]
        public void TryPrepare_WhenTheDirectoryRejectsAWrite_FailsAndNamesIt()
        {
            // Arrange
            _fileSystem.WritesFail = true;

            // Act
            var prepared = _preparer.TryPrepare(out var error);

            // Assert
            Assert.False(prepared);
            Assert.Contains(StoreDirectory, error);
        }

        [Fact]
        public void TryPrepare_WhenTheProbeCannotBeDeleted_Fails()
        {
            // Arrange
            _fileSystem.DeletesFail = true;

            // Act
            var prepared = _preparer.TryPrepare(out var error);

            // Assert
            Assert.False(prepared);
            Assert.NotNull(error);
        }

        [Fact]
        public void TryPrepare_WhenPortable_PreparesTheFolderBesideTheExecutable()
        {
            // Arrange
            var portableRoot = new StorageRoot(_fileSystem, isPortable: true);
            IStorageRootPreparer preparer = new StorageRootPreparer(_fileSystem, portableRoot);

            // Act
            var prepared = preparer.TryPrepare(out _);

            // Assert
            Assert.True(prepared);
            Assert.Equal(new[] { @"C:\FakeApp\Data" }, _fileSystem.CreatedDirectories);
        }
    }
}
