using Brinell.Core.Settings;

namespace Brinell.Core.Tests;

public sealed class TestSettingsProviderTests
{
    [Fact]
    public void Resolve_MergesDefaultIncludesLocalAndScenarioFiles()
    {
        var directory = CreateTempDirectory();
        try
        {
            var settingsDirectory = Path.Combine(directory, "TestSettings");
            Directory.CreateDirectory(Path.Combine(settingsDirectory, "profiles"));
            Directory.CreateDirectory(Path.Combine(settingsDirectory, "scenarios"));
            File.WriteAllText(Path.Combine(settingsDirectory, "testsettings.json"), """
                {
                  "include": [ "profiles/deterministic.json" ],
                  "settings": {
                    "capabilities": {
                      "hardware": false,
                      "liveApi": false
                    },
                    "hardware": {
                      "a9Camera": {
                        "host": "default-host",
                        "username": "default-user"
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(Path.Combine(settingsDirectory, "profiles", "deterministic.json"), """
                {
                  "settings": {
                    "hardware": {
                      "a9Camera": {
                        "host": "profile-host"
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(Path.Combine(settingsDirectory, "testsettings.local.json"), """
                {
                  "settings": {
                    "hardware": {
                      "a9Camera": {
                        "password": "local-password"
                      }
                    }
                  }
                }
                """);
            File.WriteAllText(Path.Combine(settingsDirectory, "scenarios", "uat-006-2.json"), """
                {
                  "settings": {
                    "capabilities": {
                      "hardware": true
                    },
                    "hardware": {
                      "a9Camera": {
                        "host": "scenario-host"
                      }
                    }
                  }
                }
                """);

            var settings = new JsonTestSettingsProvider().Resolve(new TestSettingsRequest(
                directory,
                ScenarioId: "uat-006-2"));

            Assert.True(settings.GetRequired<bool>("capabilities.hardware"));
            Assert.False(settings.GetRequired<bool>("capabilities.liveApi"));
            Assert.Equal("scenario-host", settings.GetRequired<string>("hardware.a9Camera.host"));
            Assert.Equal("default-user", settings.GetRequired<string>("hardware.a9Camera.username"));
            Assert.Equal("local-password", settings.GetRequired<string>("hardware.a9Camera.password"));
            Assert.Equal(4, settings.Sources.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Bind_TypedRootAndSection_UseProperties()
    {
        var directory = CreateTempDirectory();
        try
        {
            var settingsDirectory = Path.Combine(directory, "TestSettings");
            Directory.CreateDirectory(settingsDirectory);
            File.WriteAllText(Path.Combine(settingsDirectory, "testsettings.json"), """
                {
                  "settings": {
                    "hardware": {
                      "a9Camera": {
                        "host": "192.168.168.1",
                        "username": "admin",
                        "password": "secret"
                      }
                    }
                  }
                }
                """);

            var settings = new JsonTestSettingsProvider().Resolve(new TestSettingsRequest(directory));
            var root = settings.Bind<ExampleRootSettings>();
            var camera = settings.Bind<ExampleA9CameraSettings>();

            Assert.Equal("192.168.168.1", root.Hardware.A9Camera.Host);
            Assert.Equal("admin", camera.Username);
            Assert.Equal("secret", camera.Password);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"brinell-testsettings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    [TestSettingsRoot]
    private sealed class ExampleRootSettings
    {
        public ExampleHardwareSettings Hardware { get; init; } = new();
    }

    private sealed class ExampleHardwareSettings
    {
        public ExampleA9CameraSettings A9Camera { get; init; } = new();
    }

    [TestSettingsSection("hardware.a9Camera")]
    private sealed class ExampleA9CameraSettings
    {
        public string Host { get; init; } = string.Empty;

        public string Username { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
