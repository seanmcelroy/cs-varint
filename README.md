# cs-varint

[![license](https://img.shields.io/github/license/seanmcelroy/cs-varint.svg)](LICENSE)
[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

This is a C# .NET Core port of the repository https://github.com/multiformats/go-varint

Varint helpers that enforce minimal encoding.

The purpose of this port is to support future efforts to port go-libp2p to the C# language.

## Install

This is a C# class library.  You may include it in any project by referencing using the dotnet CLI too,
such as:

```
dotnet add reference ./VarInt/VarInt.csproj
```

Or simply by using the NuGet package manager, like:

```
dotnet add package cs-varint
```

## Usage

To convert to or from variable binary encoded UInt64 values, use the VarInt static
class methods.

```
var into = VarInt.ToUVarInt(i);
var back = VarInt.FromUVarInt(into);
```

This is a library that is to be used by a project which includes it.

Method signatures are generally preserved as-is from the source library go-varint,
with these notable deviations:

1. Uvarint is renamed UVarInt to match .NET standard naming conventions, such as seeen in BigInt
2. Errors are not returned as objects in a tuple; they are thrown as C# exceptions, so callers will need to use try/catch (common in .NET) instead of return value checking (common in Go)

## Contributing

PRs accepted.

Small note: If editing the Readme, please conform to the [standard-readme](https://github.com/RichardLitt/standard-readme) specification.

## License

[MIT © Sean McElroy](LICENSE)
