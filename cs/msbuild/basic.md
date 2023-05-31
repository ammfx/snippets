## Basic properties
https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-properties
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Define 'MyProperty'="MyValue" -->
    <MyProperty>MyValue</MyProperty>

    <!-- Use property value -->
    <MyProperty2>$(MyProperty)</MyProperty2>

    <!-- Set some other prop depending on `MyProperty` value -->
    <DefineConstants Condition="'$(MyProperty)'=='MyValue'">$(DefineConstants);MY_CONSTANT</DefineConstants>
  </PropertyGroup>

  <!-- Same as above, use `Condition=` on any tag -->
  <PropertyGroup Condition="'$(MyProperty)'=='MyValue'">
    <!-- Define `MY_CONSTANT` in project -->
    <DefineConstants>$(DefineConstants);MY_CONSTANT</DefineConstants>
  </PropertyGroup>
</Project>
```

## Basic collections
https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-items
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- Create collection named `MyCollection` and add to it object { Name="MyTextValue" } -->
    <MyCollection Include="MyTextValue" />

    <!-- Add to `MyCollection` object { Name="Value2", MyProp="MyPropValue" } -->
    <MyCollection Include="Value2" MyProp="MyPropValue" />

    <!-- Same as above -->
    <MyCollection Include="Value2">
      <MyProp>MyPropValue</MyProp>
    </MyCollection>
  </ItemGroup>
</Project>
```

## Targets
- https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets#sdk-and-default-build-targets
- Targets contains ordered list of tasks https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-tasks
- https://learn.microsoft.com/en-us/visualstudio/msbuild/exec-task
```xml
<Target Name="MessageBeforePublish" BeforeTargets="BeforePublish">
  <Message Text="BeforePublish" Importance="high" />
</Target>
<Target Name="MessageAfterPublish" AfterTargets="AfterPublish">
  <Message Text="AfterPublish" Importance="high" />
</Target>

<!-- Runned only explicitly or if other runned target depend on it:
     `msbuild MyProject.sln -target:MyExplicitTarget;SomeOtherTarget`
     Runs Clean,Publish targets before itself -->
<Target Name="MyExplicitTarget" DependsOnTargets="Clean;Publish">
  <Message Text="Hello" Importance="high" />

  <!-- Call `Copy` task -->
  <Copy SourceFiles="@(MySourceFiles)" DestinationFolder="@(MyDestFolder)">
    <Output TaskParameter="CopiedFiles" ItemName="SuccessfullyCopiedFiles" />
  </Copy>
</Target>
```

## log view
Analyze msbuild log using https://github.com/KirillOsenkov/MSBuildStructuredLog
```cmd
:: generate `msbuild.binlog` file, a)
msbuild MyProject.sln /bl
:: b)
dotnet build -bl
:: c) set environment variable MSBUILDDEBUGENGINE=1 to get `msbuild.binlog` when building from Visual Studio
```

## Directory.Build.props
- https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties
- https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props
```xml
<Project>
  <PropertyGroup>
    <!-- Move all projects bin+obj folders to single root `Output/` folder -->
    <MyOutput>$(MSBuildThisFileDirectory)..\Output\</MyOutput>
    <OutputPath>$(MyOutput)Dev\$(MSBuildProjectName)\</OutputPath>
    <BaseIntermediateOutputPath>$(MyOutput)obj\$(MSBuildProjectName)\</BaseIntermediateOutputPath>

    <!-- Do not add 'net6.0' to output path -->
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <!-- Do not add 'win10-x64' to output path -->
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
    <!-- Do not create other languages folders (like en-GB) in output -->
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>

    <!-- Use same settings (overridable) for all projects -->
    <TargetFramework>net6.0</TargetFramework>

    <!-- Set dll properties for all projects -->
    <Company>ACompany</Company>
    <Copyright>Copyright © ACompany 2020</Copyright>
    <Product>Examples</Product>
    <Version>1.0.0</Version>
    <FileVersion>1.0.0</FileVersion>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <InformationalVersion>1.0.1-some-string</InformationalVersion>
  </PropertyGroup>
</Project>
```

## Directory.Packages.props
https://devblogs.microsoft.com/nuget/introducing-central-package-management/

Directory.Packages.props:
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="System.Memory" Version="4.5.5" />
    <PackageVersion Include="OtherPackage" Version="1.0.0" />
  </ItemGroup>
</Project>
```
MyProject.csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="System.Memory" /> <!-- without version -->
    <PackageReference Include="OtherPackage" VersionOverride="2.0.0" />
  </ItemGroup>
</Project>
```

## nuget.config
https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageRestore>
    <!--Allow NuGet to download missing packages -->
    <add key="enabled" value="True" />
    <!-- Automatically check for missing packages during build in Visual Studio -->
    <add key="automatic" value="True" />
  </packageRestore>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="MyPrivateNuget" value="https://pkgs.dev.azure.com/MyOrg/_packaging/MyProject/nuget/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="MyPrivateNuget">
      <package pattern="MyPrivatePackageName" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## .editorconfig
```ini
root = true

[*]
charset = utf-8
indent_style = tab
end_of_line = crlf
trim_trailing_whitespace = true
# insert_final_newline = true

#[*.{xml,xaml,csproj,props}]
# indent_size = 2  # for indent_style=space

[*.cs]
# indent_size = 4  # for indent_style=space
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
dotnet_style_namespace_match_folder = true

dotnet_naming_rule.private_members_with_underscore.symbols = private_fields
dotnet_naming_rule.private_members_with_underscore.style = prefix_underscore
dotnet_naming_rule.private_members_with_underscore.severity = suggestion
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.prefix_underscore.capitalization = camel_case
dotnet_naming_style.prefix_underscore.required_prefix = _

dotnet_diagnostic.CS4014.severity = error       # missing await

# Microsoft.CodeQuality.Analyzers
dotnet_diagnostic.CA2000.severity = warning     # Dispose objects before losing scope
dotnet_diagnostic.CA2002.severity = error       # Do not lock on objects with weak identity
dotnet_diagnostic.CA2007.severity = error       # Consider calling ConfigureAwait on the awaited task
dotnet_diagnostic.CA2016.severity = error       # Forward the CancellationToken parameter to methods that take one

# add PackageReference `Microsoft.VisualStudio.Threading.Analyzers` to check errors:
dotnet_diagnostic.VSTHRD001.severity = warning  # Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread instead of APIs that can deadlock or require specifying a priority.
dotnet_diagnostic.VSTHRD002.severity = error    # Synchronously waiting on tasks or awaiters may cause deadlocks. Use JoinableTaskFactory.Run instead.
dotnet_diagnostic.VSTHRD003.severity = warning  # Avoid awaiting foreign Tasks, can result in deadlocks
dotnet_diagnostic.VSTHRD105.severity = error    # Avoid method overloads that assume TaskScheduler.Current
dotnet_diagnostic.VSTHRD103.severity = warning  # Synchronously blocks. Await ThrowsAsync instead.
dotnet_diagnostic.VSTHRD110.severity = error    # Observe result of async calls
dotnet_diagnostic.VSTHRD200.severity = none     # Naming stylesNaming styles:  Before: Task Open()  After: Task OpenAsync()

```
