# Fine Code Coverage

[![Build status](https://ci.appveyor.com/api/projects/status/yq8s0ridnphpx4ig?svg=true)](https://ci.appveyor.com/project/FortuneN/finecodecoverage)

Download this extension from the [Visual Studio Market Place](https://marketplace.visualstudio.com/items?itemName=FortuneNgwenya.FineCodeCoverage)
or get the [CI build](https://www.vsixgallery.com/extension/fcc-f339fe606-9d51-4fca-895c-d50375137b62).  Previous versions can be obtained from [here](https://ci.appveyor.com/project/FortuneN/finecodecoverage).

---------------------------------------

### <a href="https://www.youtube.com/watch?v=Rae5bTE2D3o" target="_blank">Watch Introduction Video</a>

### Highlights unit test code coverage
Run a(some) unit test(s) and ...

#### Get highlights on the code being tested
![Code Being Tested](Art/preview-subject.png)

#### Get highlights on the code doing the testing
![Code Doing The Testing](Art/preview-test.png)

#### See Coverage View
![Coverage View](Art/Output-Coverage.png)

#### See Summary View
![Summary View](Art/Output-Summary.png)

#### See Risk Hotspots View
![Risk Hotspots View](Art/Output-RiskHotspots.png)

#### Global (Shared) options
![Global Options](Art/Options-Global.png)

#### Local (Test Project) options (override globals in your csproj/vbproj : OPTIONAL)
```
<PropertyGroup Label="FineCodeCoverage">
  <Enabled>
	True
  </Enabled>
  <Exclude>
	[ThirdParty.*]*
	[FourthParty]*
  </Exclude>
  <Include>
	[*]*
  </Include>
  <ExcludeByFile>
	**/Migrations/*
	**/Hacks/*.cs
  </ExcludeByFile>
  <ExcludeByAttribute>
	MyCustomExcludeFromCodeCoverage
  </ExcludeByAttribute>
  <IncludeTestAssembly>
	True
  </IncludeTestAssembly>
</PropertyGroup>
```

#### Exclude Referenced Project in referenced project ( csproj/vbproj : OPTIONAL )
```
<PropertyGroup>
	<FCCExcludeFromCodeCoverage/>
</PropertyGroup>			
```

#### Coverlet specific
<PropertyGroup>
	<UseDataCollector/>
</PropertyGroup>

Coverlet has different "drivers".  Fine Code Coverage has in the past only used the coverlet console driver.  This has some issues associated with it.
It is now possible to switch to the Data Collector driver.  This is the better driver but cannot be used for all projects.
For now this is opt in.  In the future Fine Code Coverage will determine the appropriate driver.
Please consult [coverlet docs](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md) for version support.
** Note that it is unnecessary to add the nuget coverlet.collector package as FCC internally supplies coverlet.collector 3.0.3**
Fine Code Coverage will use the Data Collector driver under two circumstances :
1) You are testing with runsettings that contains the coverlet collector ( and not disabled)
2) You set the UseDataCollector project property

The Coverlet Data Collector settings can be found [here](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md#advanced-options-supported-via-runsettings).
If you are using option 2) above then Common settings ( Exclusions and inclusions ) will be generated from project propertes ( above ) and global visual studio options (see below ) with project properties taking precedence.
If you are using option 1) then project and global options will only be used where a Common settings Configuration element is absent and the RunSettingsOnly option ( see below) has been changed to false.


#### Options
```
Enabled                 Specifies whether or not coverage output is enabled
RunInParallel           By default tests run and then coverage is performed.  Set to true to run coverage immediately
RunWhenTestsFail        By default coverage runs when tests fail.  Set to false to prevent this.  **Cannot be used in conjunction with RunInParallel**
RunWhenTestsExceed      Specify a value to only run coverage based upon the number of executing tests. **Cannot be used in conjunction with RunInParallel**
Exclude                 Filter expressions to exclude specific modules and types (multiple values)
Include                 Filter expressions to include specific modules and types (multiple values)
ExcludeByFile           Glob patterns specifying source files to exclude e.g. **/Migrations/* (multiple values)
ExcludeByAttribute      Attributes to exclude from code coverage (multiple values)
IncludeTestAssembly     Specifies whether to report code coverage of the test assembly

RunSettingsOnly			Specify false for global and project options to be used for coverlet data collector configuration elements when not specified in runsettings

Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.

You can ignore a method or an entire class from code coverage by creating and applying the [ExcludeFromCodeCoverage] attribute present in the System.Diagnostics.CodeAnalysis namespace.
You can also ignore additional attributes by adding to the 'ExcludeByAttributes' list (short name or full name supported) e.g. :
[GeneratedCode] => Present in System.CodeDom.Compiler namespace
[MyCustomExcludeFromCodeCoverage] => Any custom attribute that you may define
```

#### Filter Expressions
```
Wildcards
* => matches zero or more characters
		
Examples
[*]* => All types in all assemblies (nothing is instrumented)
[coverlet.*]Coverlet.Core.Coverage => The Coverage class in the Coverlet.Core namespace belonging to any assembly that matches coverlet.* (e.g coverlet.core)
[*]Coverlet.Core.Instrumentation.* => All types belonging to Coverlet.Core.Instrumentation namespace in any assembly
[coverlet.*.tests]* => All types in any assembly starting with coverlet. and ending with .tests

Both 'Exclude' and 'Include' options can be used together but 'Exclude' takes precedence.
```
 
## Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the [Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)

## Credits
[Coverlet](https://github.com/coverlet-coverage/coverlet)

[OpenCover](https://github.com/OpenCover/opencover)

[ReportGenerator](https://github.com/danielpalme/ReportGenerator)

## Please support the project
| Provider | Type      | Link                                                                                                                              |
|:---------|:---------:|:---------------------------------------------------------------------------------------------------------------------------------:|
| Paypal   | Once      | [<img src="https://www.paypalobjects.com/webstatic/mktg/Logo/pp-logo-100px.png">](https://paypal.me/FortuneNgwenya)               |
| Librepay | Recurring | [<img alt="Donate using Liberapay" src="Art/librepay.png">](https://liberapay.com/FortuneN/donate)                                |
