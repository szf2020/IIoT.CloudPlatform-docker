using FluentAssertions;
using IIoT.HttpApi;
using IIoT.MigrationWorkApp.SeedData;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace IIoT.EndToEndTests;

public sealed class ConfigurationGuardTests
{
    [Fact]
    public void DesignTimeConnectionStringResolver_MissingConnectionString_ShouldThrowClearError()
    {
        var act = () => DesignTimeConnectionStringResolver.Resolve(_ => null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DesignTimeConnectionStringResolver.ConnectionStringEnvironmentVariable}*");
    }

    [Fact]
    public void SeedAdminOptions_Load_ShouldDefaultRealName_AndAllowMissingPassword()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [SeedAdminOptions.EmployeeNoKey] = IIoTAppFixture.SeedAdminEmployeeNo
            })
            .Build();

        var options = SeedAdminOptions.Load(configuration);

        options.EmployeeNo.Should().Be(IIoTAppFixture.SeedAdminEmployeeNo);
        options.Password.Should().BeNull();
        options.RealName.Should().Be(IIoTAppFixture.SeedAdminRealName);
    }

    [Fact]
    public void SeedAdminOptions_Load_ShouldRequireEmployeeNumber()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var act = () => SeedAdminOptions.Load(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{SeedAdminOptions.EmployeeNoKey}*");
    }

    [Fact]
    public void SeedAdminOptions_RequirePassword_ShouldThrowWhenMissing()
    {
        var options = new SeedAdminOptions(
            IIoTAppFixture.SeedAdminEmployeeNo,
            null,
            IIoTAppFixture.SeedAdminRealName);

        var act = () => options.RequirePassword();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{SeedAdminOptions.PasswordKey}*");
    }

    [Fact]
    public void AppHost_ShouldWireSeedAdminParametersIntoMigrationProject()
    {
        var appHostSource = File.ReadAllText(FindRepoFile("src", "hosts", "IIoT.AppHost", "AppHost.cs"));

        appHostSource.Should().Contain("AddParameter(\"seed-admin-no\"");
        appHostSource.Should().Contain("AddParameter(\"seed-admin-password\", secret: true)");
        appHostSource.Should().Contain("WithEnvironment(\"SEED_ADMIN_NO\", seedAdminNo)");
        appHostSource.Should().Contain("WithEnvironment(\"SEED_ADMIN_PASSWORD\", seedAdminPassword)");
    }

    private static string FindRepoFile(params string[] relativeSegments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativeSegments[0]);
            if (Directory.Exists(candidate))
            {
                return Path.Combine(current.FullName, Path.Combine(relativeSegments));
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for AppHost source inspection.");
    }
}
