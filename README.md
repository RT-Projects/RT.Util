[![Build status](https://ci.appveyor.com/api/projects/status/vcq0cfok21wlmdvm?svg=true)](https://ci.appveyor.com/project/rstarkov/rt-util)

## Upgrading from version 1.*

In version 2.* this library has been broken up into a number of smaller components. RT.Util itself only has components which directly depend on some Windows / Framework functionality. The majority of the code is now in RT.Util.Core, targeting netstandard2.0. If you can't find a type you used in RT.Util v1, it's almost certainly in one of the packages below.

## RT.Util.Core

[![NuGet](https://img.shields.io/nuget/v/RT.Util.Core.svg)](https://www.nuget.org/packages/RT.Util.Core/)

Wad of stuff.

## RT.Util

[![NuGet](https://img.shields.io/nuget/v/RT.Util.svg)](https://www.nuget.org/packages/RT.Util/)

Wad of stuff that isn't compatible with .NET Core, or is dependent on something that isn't compatible.

## RT.Serialization

[![NuGet](https://img.shields.io/nuget/v/RT.Serialization.svg)](https://www.nuget.org/packages/RT.Serialization/)

Serialize classes to/from JSON/XML/binary/your own format. The feature set is optimized for maintaining backwards compatibility of serialized files as the classes evolve, in particular when used for application settings / configuration.

RT.Serialization implements core logic and is format-agnostic. Specific formats are implemented by the following libraries:

- RT.Serialization.Xml [![NuGet](https://img.shields.io/nuget/v/RT.Serialization.Xml.svg)](https://www.nuget.org/packages/RT.Serialization.Xml/)
- RT.Serialization.Json [![NuGet](https://img.shields.io/nuget/v/RT.Serialization.Json.svg)](https://www.nuget.org/packages/RT.Serialization.Json/)
- RT.Serialization.Binary [![NuGet](https://img.shields.io/nuget/v/RT.Serialization.Binary.svg)](https://www.nuget.org/packages/RT.Serialization.Binary/)

## RT.PostBuild

[![NuGet](https://img.shields.io/nuget/v/RT.PostBuild.svg)](https://www.nuget.org/packages/RT.PostBuild/)

Execute tasks after project build to validate invariants and fail the build if violated.

## RT.Json

[![NuGet](https://img.shields.io/nuget/v/RT.Json.svg)](https://www.nuget.org/packages/RT.Json/)

A JSON parser written before Json.NET became good. Slower than Json.NET. API aimed at stringent access.

## RT.Util.Legacy

[![NuGet](https://img.shields.io/nuget/v/RT.Util.Legacy.svg)](https://www.nuget.org/packages/RT.Util.Legacy/)

Legacy code from RT.Util. Preserved only to make it possible to compile ancient unmaintained private projects.
