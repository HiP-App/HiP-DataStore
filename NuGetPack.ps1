$csproj = (ls *.Sdk\*.csproj).FullName
Switch ("$(Build.SourceBranchName)")
{
    "master" { dotnet pack "$csproj" -o . }
    "develop" { dotnet pack "$csproj" -o . --version-suffix "develop" }
    default { exit }
}
$nupkg = (ls *.Sdk\*.nupkg).FullName
dotnet --% nuget push "$nupkg" -k %MyGetKey% -s %NuGetFeed%
$LASTEXITCODE = 0