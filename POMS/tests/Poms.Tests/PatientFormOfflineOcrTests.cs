using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class PatientFormOfflineOcrTests
{
    [Fact]
    public void Parser_MapsTemplateRegions_AndKeepsUnreadableRequiredFieldsBlank()
    {
        var tsv = string.Join('\n',
        [
            "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext",
            Row(1, 0, 0, 0, 0, 0, 0, 1000, 1000, -1, ""),
            Row(5, 1, 1, 1, 1, 70, 230, 90, 30, 92, "Mohamed"),
            Row(5, 1, 1, 1, 2, 170, 230, 80, 30, 90, "Lukman"),
            Row(5, 1, 2, 1, 1, 560, 230, 110, 30, 96, "26.10.2020"),
            Row(5, 1, 3, 1, 1, 570, 350, 90, 30, 94, "Female"),
            Row(5, 1, 4, 1, 1, 600, 490, 100, 30, 88, "Gampaha"),
            Row(5, 1, 5, 1, 1, 70, 770, 100, 30, 86, "Lukman"),
            Row(5, 1, 5, 1, 2, 220, 770, 90, 30, 91, "Father"),
            Row(5, 1, 6, 1, 1, 620, 770, 140, 30, 89, "0778548589")
        ]);

        var result = TesseractPatientFormParser.Parse(tsv);

        result.FullName.Should().Be("Mohamed Lukman");
        result.DateOfBirth.Should().Be("2020-10-26");
        result.Gender.Should().Be("Female");
        result.District.Should().Be("Gampaha");
        result.GuardianName.Should().Be("Lukman");
        result.GuardianRelationship.Should().Be("Father");
        result.GuardianPhone.Should().Be("0778548589");
        result.IdentificationNumber.Should().BeNull();
        result.Warnings.Should().Contain(warning => warning.Contains("Identification number"));
    }

    [Fact]
    public async Task Coordinator_RoutesToSelectedConfiguredEngine()
    {
        var openAi = new StubEngine(PatientFormOcrProvider.OpenAi, configured: true);
        var offline = new StubEngine(PatientFormOcrProvider.Offline, configured: false);
        var service = new PatientFormOcrService([openAi, offline]);

        var result = await service.ExtractAsync(
            PatientFormOcrProvider.OpenAi,
            new MemoryStream([1]),
            "image/png");

        result.FullName.Should().Be("Selected OpenAi");
        openAi.CallCount.Should().Be(1);
        offline.CallCount.Should().Be(0);
        service.IsConfigured(PatientFormOcrProvider.Offline).Should().BeFalse();
    }

    [Fact]
    public async Task FileStorage_StagesAndMaterializesOriginalScan_WithOpaqueNames()
    {
        var root = Path.Combine(Path.GetTempPath(), $"poms-file-tests-{Guid.NewGuid():N}");
        try
        {
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };
            var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "FormImage", "legacy-form.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var storage = new FileStorageService(root);

            var staged = await storage.StageOcrImportAsync(formFile, "image/png");
            var stored = await storage.MaterializeOcrImportAsync(staged.Token, "2026/0001");
            var restored = await storage.GetFileAsync(stored.StoragePath);
            await storage.DeleteStagedOcrImportAsync(staged.Token);

            stored.FileName.Should().Be("legacy-form.png");
            stored.ContentType.Should().Be("image/png");
            Path.GetFileName(stored.StoragePath).Should().NotBe("legacy-form.png");
            restored.Should().Equal(bytes);
            Directory.EnumerateFiles(root, $"{staged.Token}*", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static string Row(
        int level,
        int block,
        int paragraph,
        int line,
        int word,
        int left,
        int top,
        int width,
        int height,
        double confidence,
        string text) =>
        $"{level}\t1\t{block}\t{paragraph}\t{line}\t{word}\t{left}\t{top}\t{width}\t{height}\t{confidence}\t{text}";

    private sealed class StubEngine(PatientFormOcrProvider provider, bool configured) : IPatientFormOcrEngine
    {
        public PatientFormOcrProvider Provider { get; } = provider;
        public string DisplayName => Provider.ToString();
        public string Description => "Test engine";
        public bool IsConfigured { get; } = configured;
        public int CallCount { get; private set; }

        public Task<PatientFormOcrResult> ExtractAsync(
            Stream image,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new PatientFormOcrResult { FullName = $"Selected {Provider}" });
        }
    }
}
