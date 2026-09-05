using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Storage
{
    public class AppDataFileStoreTests
    {
        private const string FileName = "state.json";
        private const string StoreDirectory = @"C:\FakeAppData\OLED-Sleeper";
        private const string TargetPath = StoreDirectory + @"\" + FileName;
        private const string BackupPath = TargetPath + ".bak";
        private const string TempPath = TargetPath + ".tmp";

        private readonly FakeFileSystem _fileSystem;
        private readonly AppDataFileStore _store;

        public AppDataFileStoreTests()
        {
            _fileSystem = new FakeFileSystem();
            _store = new AppDataFileStore(_fileSystem, new StorageRoot(_fileSystem, isPortable: false));
        }

        [Fact]
        public void Read_WhenTheTargetParses_ReturnsItsContents()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": 40 }""");

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(40u, state["A"]);
        }

        [Fact]
        public void Read_WhenTheTargetIsMissing_ReadsTheBackup()
        {
            // Arrange
            _fileSystem.AddFile(BackupPath, """{ "A": 40 }""");

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(40u, state["A"]);
        }

        [Fact]
        public void Read_WhenTheTargetIsTruncated_ReadsTheBackup()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": """);
            _fileSystem.AddFile(BackupPath, """{ "A": 40 }""");

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(40u, state["A"]);
        }

        [Fact]
        public void Read_WhenNeitherFileParses_ReturnsNull()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": """);
            _fileSystem.AddFile(BackupPath, """{ "A": """);

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void Read_WhenNeitherFileExists_ReturnsNull()
        {
            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void Read_WhenTheTargetHoldsJsonNull_ReturnsNullWithoutReadingTheBackup()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, "null");
            _fileSystem.AddFile(BackupPath, """{ "A": 40 }""");

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void TryWrite_WhenNoTargetExists_MovesTheTemporaryFileOntoIt()
        {
            // Act
            var written = _store.TryWrite(FileName, new Dictionary<string, uint> { ["A"] = 40 });

            // Assert
            Assert.True(written);
            Assert.Contains("\"A\": 40", _fileSystem.Contents(TargetPath)!);
            Assert.Null(_fileSystem.Contents(BackupPath));
            Assert.Null(_fileSystem.Contents(TempPath));
        }

        [Fact]
        public void TryWrite_WhenTheTargetExists_ReplacesItAndKeepsTheOldContentsAsBackup()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": 40 }""");

            // Act
            var written = _store.TryWrite(FileName, new Dictionary<string, uint> { ["A"] = 5 });

            // Assert
            Assert.True(written);
            Assert.Contains("\"A\": 5", _fileSystem.Contents(TargetPath)!);
            Assert.Equal("""{ "A": 40 }""", _fileSystem.Contents(BackupPath));
            Assert.Null(_fileSystem.Contents(TempPath));
        }

        [Fact]
        public void TryWrite_WhenTheTemporaryWriteFails_ReturnsFalseAndLeavesTheTargetAlone()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": 40 }""");
            _fileSystem.WritesFail = true;

            // Act
            var written = _store.TryWrite(FileName, new Dictionary<string, uint> { ["A"] = 5 });

            // Assert
            Assert.False(written);
            Assert.Equal("""{ "A": 40 }""", _fileSystem.Contents(TargetPath));
        }

        [Fact]
        public void TryWrite_WhenTheReplaceFails_ReturnsFalseAndLeavesTheTargetAlone()
        {
            // Arrange
            _fileSystem.AddFile(TargetPath, """{ "A": 40 }""");
            _fileSystem.ReplaceFails = true;

            // Act
            var written = _store.TryWrite(FileName, new Dictionary<string, uint> { ["A"] = 5 });

            // Assert
            Assert.False(written);
            Assert.Equal("""{ "A": 40 }""", _fileSystem.Contents(TargetPath));
        }

        [Fact]
        public void TryWrite_WhenReadBack_ReturnsTheSameValue()
        {
            // Arrange
            _store.TryWrite(FileName, new Dictionary<string, uint> { ["A"] = 40 });

            // Act
            var state = _store.Read<Dictionary<string, uint>>(FileName);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(40u, state["A"]);
        }
    }
}
