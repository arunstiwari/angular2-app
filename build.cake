#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools"
#tool nuget:?package=MSBuild.SonarQube.Runner.Tool
#addin nuget:?package=Cake.Sonar
#addin "nuget:https://www.nuget.org/api/v2?package=Cake.VsMetrics"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./src/Example/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/Example.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./src/Example.sln", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild("./src/Example.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
});

Task("Run-Static-Analysis")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    Information("Generate VsMetrics XML report:");
	var projects = GetFiles("bin/Debug/*.dll");
	var settings = new VsMetricsSettings()
	{
		SuccessFile = true,
		IgnoreGeneratedCode = true
	};

	VsMetrics(projects, "metrics_result.xml", settings);
});


Task("Run-Code-Coverage")
    .IsDependentOn("Run-Static-Analysis")
    .Does(() =>
{
	DotCoverCover(tool => {
	  tool.NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
        });
	  },
	  new FilePath("./result.dcvr"),
	  new DotCoverCoverSettings()
		.WithFilter("+:*")
		.WithFilter("-:*.Tests"));
		
	DotCoverReport(new FilePath("./result.dcvr"),
		new FilePath("./result.html"),
		new DotCoverReportSettings {
		ReportType = DotCoverReportType.HTML
	});
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Code-Coverage");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
