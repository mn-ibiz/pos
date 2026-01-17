using System.Data;
using System.Text;
using FluentAssertions;
using Moq;
using Serilog;
using HospitalityPOS.Infrastructure.Services;
using Xunit;

namespace HospitalityPOS.Business.Tests.Services;

/// <summary>
/// Unit tests for the ExportService class.
/// </summary>
public class ExportServiceTests : IDisposable
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly ExportService _exportService;
    private readonly string _testDirectory;

    public ExportServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _exportService = new ExportService(_loggerMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), "ExportServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    #region ExportToCsvAsync<T> Tests

    [Fact]
    public async Task ExportToCsvAsync_ShouldExportDataToFile()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Id = 1, Name = "Item 1", Value = 100.50m },
            new() { Id = 2, Name = "Item 2", Value = 200.75m }
        };
        var filePath = Path.Combine(_testDirectory, "test_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Id,Name,Value");
        content.Should().Contain("1,Item 1,100.50");
        content.Should().Contain("2,Item 2,200.75");
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldHandleEmptyCollection()
    {
        // Arrange
        var data = new List<TestData>();
        var filePath = Path.Combine(_testDirectory, "empty_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Id,Name,Value");
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1); // Only header
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldEscapeSpecialCharacters()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Id = 1, Name = "Item, with comma", Value = 100m },
            new() { Id = 2, Name = "Item \"with quotes\"", Value = 200m },
            new() { Id = 3, Name = "Item\nwith newline", Value = 300m }
        };
        var filePath = Path.Combine(_testDirectory, "special_chars_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("\"Item, with comma\"");
        content.Should().Contain("\"Item \"\"with quotes\"\"\"");
        content.Should().Contain("\"Item\nwith newline\"");
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldFormatDatesCorrectly()
    {
        // Arrange
        var testDate = new DateTime(2025, 12, 31, 14, 30, 45);
        var data = new List<TestDataWithDate>
        {
            new() { Id = 1, CreatedAt = testDate }
        };
        var filePath = Path.Combine(_testDirectory, "date_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("2025-12-31 14:30:45");
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldFormatBooleansCorrectly()
    {
        // Arrange
        var data = new List<TestDataWithBool>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false }
        };
        var filePath = Path.Combine(_testDirectory, "bool_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("1,Yes");
        content.Should().Contain("2,No");
    }

    [Fact]
    public async Task ExportToCsvAsync_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var data = new List<TestData> { new() { Id = 1, Name = "Test", Value = 100m } };
        var newDirectory = Path.Combine(_testDirectory, "NewDir", "SubDir");
        var filePath = Path.Combine(newDirectory, "test.csv");

        // Act
        await _exportService.ExportToCsvAsync(data, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
    }

    #endregion

    #region ExportToCsvAsync(DataTable) Tests

    [Fact]
    public async Task ExportToCsvAsync_DataTable_ShouldExportDataToFile()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Value", typeof(decimal));
        dataTable.Rows.Add(1, "Item 1", 100.50m);
        dataTable.Rows.Add(2, "Item 2", 200.75m);

        var filePath = Path.Combine(_testDirectory, "datatable_export.csv");

        // Act
        await _exportService.ExportToCsvAsync(dataTable, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("Id,Name,Value");
        content.Should().Contain("1,Item 1,100.50");
        content.Should().Contain("2,Item 2,200.75");
    }

    #endregion

    #region GenerateFilename Tests

    [Fact]
    public void GenerateFilename_ShouldGenerateCorrectFilename()
    {
        // Arrange
        var reportType = "Daily Sales";
        var startDate = new DateTime(2025, 12, 31);

        // Act
        var result = _exportService.GenerateFilename(reportType, startDate);

        // Assert
        result.Should().Be("Daily_Sales_20251231.csv");
    }

    [Fact]
    public void GenerateFilename_ShouldIncludeDateRangeWhenEndDateDiffers()
    {
        // Arrange
        var reportType = "Sales By Product";
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        // Act
        var result = _exportService.GenerateFilename(reportType, startDate, endDate);

        // Assert
        result.Should().Be("Sales_By_Product_20251201-20251231.csv");
    }

    [Fact]
    public void GenerateFilename_ShouldNotIncludeDateRangeWhenSameDay()
    {
        // Arrange
        var reportType = "Daily Summary";
        var startDate = new DateTime(2025, 12, 31);
        var endDate = new DateTime(2025, 12, 31);

        // Act
        var result = _exportService.GenerateFilename(reportType, startDate, endDate);

        // Assert
        result.Should().Be("Daily_Summary_20251231.csv");
    }

    [Fact]
    public void GenerateFilename_ShouldUsePdfExtension()
    {
        // Arrange
        var reportType = "Sales Report";
        var startDate = new DateTime(2025, 12, 31);

        // Act
        var result = _exportService.GenerateFilename(reportType, startDate, extension: "pdf");

        // Assert
        result.Should().Be("Sales_Report_20251231.pdf");
    }

    [Fact]
    public void GenerateFilename_ShouldSanitizeInvalidCharacters()
    {
        // Arrange
        var reportType = "Sales/By/Product";
        var startDate = new DateTime(2025, 12, 31);

        // Act
        var result = _exportService.GenerateFilename(reportType, startDate);

        // Assert
        result.Should().NotContain("/");
        result.Should().EndWith(".csv");
    }

    #endregion

    #region GetDefaultExportDirectory Tests

    [Fact]
    public void GetDefaultExportDirectory_ShouldReturnValidPath()
    {
        // Act
        var result = _exportService.GetDefaultExportDirectory();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("POS Reports");
    }

    #endregion

    #region ExportToPdfAsync Tests

    [Fact]
    public async Task ExportToPdfAsync_ShouldReturnFalse_WhenNotImplemented()
    {
        // Arrange
        var data = new List<TestData> { new() { Id = 1, Name = "Test", Value = 100m } };
        var filePath = Path.Combine(_testDirectory, "test.pdf");

        // Act
        var result = await _exportService.ExportToPdfAsync(data, filePath, "Test Report");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Test Data Classes

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    private class TestDataWithDate
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class TestDataWithBool
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
