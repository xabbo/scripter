$Version=$(dotnet-gitversion /showvariable SemVer)
dotnet publish src/Xabbo.Scripter -c "Release" -f "net6.0-windows" -o "bin\v$Version"