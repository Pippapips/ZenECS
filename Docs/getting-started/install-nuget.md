# Install via NuGet

> Docs / Getting started / Install via NuGet

Install ZenECS Core via NuGet for .NET projects (non-Unity).

## Overview

ZenECS Core is available as a NuGet package for .NET Standard 2.1+ projects. This is ideal for:

- **Server Applications**: Game servers, simulation servers
- **Standalone Tools**: Command-line tools, utilities
- **Other Game Engines**: Godot, MonoGame, etc.
- **Testing**: Unit tests and integration tests

## Installation

### Via .NET CLI

```bash
dotnet add package ZenECS.Core --version 1.0.0
```

### Via Package Manager

In Visual Studio:
1. Right-click project → **Manage NuGet Packages**
2. Search for `ZenECS.Core`
3. Click **Install**

### Via PackageReference

Add to `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="ZenECS.Core" Version="1.0.0" />
</ItemGroup>
```

### Via packages.config

Add to `packages.config`:

```xml
<packages>
  <package id="ZenECS.Core" version="1.0.0" targetFramework="netstandard2.1" />
</packages>
```

## Requirements

- **.NET Standard 2.1** or higher
- **.NET Core 3.1+**, **.NET 5+**, or **.NET Framework 4.8+**

## Verify Installation

### Check Package

```bash
dotnet list package
```

Should show:
```
ZenECS.Core    1.0.0
```

### Test in Code

```csharp
using ZenECS.Core;

class Program
{
    static void Main()
    {
        var kernel = new Kernel();
        var world = kernel.CreateWorld(null, "TestWorld");
        Console.WriteLine("ZenECS Core: OK");
    }
}
```

## Project Types

### Console Application

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ZenECS.Core" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Class Library

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ZenECS.Core" Version="1.0.0" />
  </ItemGroup>
</Project>
```

## Next Steps

After installation:

1. **[Quick Start Guide](quickstart-basic.md)** - Build your first system
2. **[Core Concepts](../core/world.md)** - Learn ECS concepts
3. **[Samples Overview](samples-overview.md)** - Explore examples

## See Also

- [Install via UPM](install-upm.md) - Unity installation
- [Quick Start](quickstart-basic.md) - Get started quickly
- [Core README](../../Packages/com.zenecs.core/README.md) - Detailed documentation
