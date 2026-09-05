using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Storage
{
    public class StorageRootTests
    {
        private readonly FakeFileSystem _fileSystem = new();

        [Fact]
        public void DirectoryPath_WhenNotPortable_IsTheApplicationDataFolder()
        {
            // Act
            var root = new StorageRoot(_fileSystem, isPortable: false);

            // Assert
            Assert.Equal(@"C:\FakeAppData\OLED-Sleeper", root.DirectoryPath);
        }

        [Fact]
        public void DirectoryPath_WhenPortable_IsTheDataFolderBesideTheExecutable()
        {
            // Act
            var root = new StorageRoot(_fileSystem, isPortable: true);

            // Assert
            Assert.Equal(@"C:\FakeApp\Data", root.DirectoryPath);
        }
    }
}
