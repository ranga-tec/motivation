using FluentAssertions;
using Poms.Web.Services;

namespace Poms.Tests;

public sealed class StoragePathValidatorTests
{
    [Fact]
    public void Validate_CreatesWritableStorageDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var files = Path.Combine(root, "files");
        var keys = Path.Combine(root, "keys");

        try
        {
            StoragePathValidator.Validate(files, keys, isProduction: false, allowEphemeralStorage: false);

            Directory.Exists(files).Should().BeTrue();
            Directory.Exists(keys).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Validate_RejectsTemporaryStorageInProduction()
    {
        var files = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "files");
        var keys = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "keys");

        var action = () => StoragePathValidator.Validate(
            files,
            keys,
            isProduction: true,
            allowEphemeralStorage: false);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*temporary storage*");
    }
}
