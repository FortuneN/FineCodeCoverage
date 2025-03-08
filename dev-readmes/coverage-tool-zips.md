# Updating coverage tool zips procedure

FCC uses 3 coverage providers, these are zip files included in the VSIX.
These get extracted to AppData\Local\FineCodeCoverage

You need to debug FCC to ensure updates to these zips work. This will override the existing.

## All updated zips will need to follow this procedure.

1. Add to \FineCodeCoverage\Shared Files\ZippedTools directory
2. Add as a linked file to the FineCodeCoverage and FineCodeCoverage2022 projects

Expand the project in solution explorer  
Right click on the ZippedTools directory  
Add -> Existing Item  
Navigate to \FineCodeCoverage\Shared Files\ZippedTools  
In the browser dialog select All files to see the zips  
Select the zip you want to add  
Click the drop down arrow on the Add button and select Add as Link

3. Set the Build properties in the properties window

Select the file  
Set Build Action to Content  
Set Include in VSIX to True

4. Remove the previous zip in solution explorer

5. For all updated zips debug an appropriate project ( details follow ) and if coverage is provided the old zip can be deleted from \FineCodeCoverage\Shared Files\ZippedTools

## How to update the zips

### msCodeCoverage

1. [Download the nuget package](https://www.nuget.org/packages/Microsoft.CodeCoverage/).
2. Change the file extension to zip
3. Check to see if the zip is compatible

The name should be of the form microsoft.codecoverage.VERSION.zip
It needs to be compatible with MsCodeCoverageRunSettingsService

```csharp
public void Initialize(string appDataFolder, IFCCEngine fccEngine, CancellationToken cancellationToken)
{
    this.fccEngine = fccEngine;
    var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolder, zipDirectoryName,zipPrefix, cancellationToken);
    fccMsTestAdapterPath = Path.Combine(zipDestination, "build", "netstandard2.0");
    shimPath = Path.Combine(zipDestination, "build", "netstandard2.0", "CodeCoverage", "coreclr", "Microsoft.VisualStudio.CodeCoverage.Shim.dll");
}
```

The fccMsTestAdapterPath needs to be a directory that contains a ...collector.dll which is currently called Microsoft.VisualStudio.TraceDataCollector.dll.  
The shimPath needs to exist.

4. Add the zip to the solution - see instruction at the top of this page.
5. Debug

Debug a project with the option MsCodeCoverage Yes

### coverletCollector

1. [Download the nuget package](https://www.nuget.org/packages/coverlet.collector/).
2. Change the file extension to zip
3. Check to see if the zip is compatible
   The name should be of the form coverlet.collector.VERSION.zip
   It needs to be compatible with CoverletDataCollectorUtil

```csharp
public void Initialize(string appDataFolder,CancellationToken cancellationToken)
{
    var zipDestination = toolUnzipper.EnsureUnzipped(appDataFolder, zipDirectoryName, zipPrefix, cancellationToken);
    var testAdapterPath = Path.Combine(zipDestination, "build", "netstandard2.0");
    TestAdapterPathArg = $@"""{testAdapterPath}""";
}
```

Given that it is a data collector like msCodeCoverage the testAdapterPath needs to be a directory that contains a ...collector.dll which is currently called coverlet.collector.dll. 4. Add the zip to the solution - see instruction at the top of this page. 5. Debug

Debug an SDK style project with RunMsCodeCoverage No and with the test project having an MSBuild property `<UseDataCollector/>`

### coverlet

1. dotnet tool install --global coverlet.console

This will create or update installation in username/.dotnet/tools directory

2. Create a directory - coverlet.console.VERSION
3. Create a sub directory - .store

Copy the following from within username/.dotnet/tools
coverlet.exe into coverlet.console.VERSION
coverlet.console dirctory into .store.

4. Add the zip to the solution - see instruction at the top of this page.
5. Debug

Debug an SDK style project with RunMsCodeCoverage No and with the test project **not** having an MSBuild property `<UseDataCollector/>`
