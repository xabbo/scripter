# xabbo scripter
A C# scripting interface for [G-Earth](https://github.com/sirjonasxx/G-Earth).\
Powered by the
[Xabbo.Common](https://www.github.com/b7c/Xabbo.Common),
[Xabbo.GEarth](https://www.github.com/b7c/Xabbo.GEarth) and
[Xabbo.Core](https://www.github.com/b7c/Xabbo.Core) libraries.

See [this repository](https://www.github.com/b7c/xabbo-scripts) for a collection of useful scripts and examples of what is possible with the scripter.

![image](https://user-images.githubusercontent.com/58299468/181401866-6950ee4b-6bcc-49bb-a35b-24f2798b33d0.png)

## Usage

Check out the [wiki](https://github.com/b7c/Xabbo.Scripter/wiki) for information on how to get started writing your own scripts.

## Building from source

Requires the .NET 6 SDK.

```sh
# Clone the repo
git clone https://github.com/b7c/Xabbo.Scripter -b develop
cd Xabbo.Scripter
# Initialize the submodules
git submodule update --init
# Checkout each submodule into a branch,
# required for Xabbo libraries using GitVersion
git submodule foreach 'git checkout -b Xabbo.Scripter'
# Build & run the application
dotnet build src/Xabbo.Scripter -c Release -o bin\Release
cd bin\Release
.\Xabbo.Scripter
```
