namespace AgentWindowsControl.Core;

public sealed record ThirdPartyLicense(
    string Name,
    string PackageId,
    string Version,
    string License,
    string ProjectUrl,
    string NuGetUrl,
    string Compatibility,
    string[] Requirements);

public static class Licensing
{
    public static readonly ThirdPartyLicense LibreAutomate = new(
        Name: "LibreAutomate",
        PackageId: "LibreAutomate",
        Version: "1.15.0",
        License: "MIT",
        ProjectUrl: "https://www.libreautomate.com/",
        NuGetUrl: "https://www.nuget.org/packages/LibreAutomate",
        Compatibility: "MIT is compatible with this project's MIT license.",
        Requirements:
        [
            "Keep the LibreAutomate copyright notice and MIT license text when distributing LibreAutomate binaries, source, or substantial portions.",
            "Document the LibreAutomate dependency in third-party notices.",
            "If LibreAutomate source is vendored or modified, include the upstream MIT license notice with the vendored copy."
        ]);
}
