$csproj = (ls *.Sdk\*.csproj).FullName
Switch ("$env:Build_SourceBranchName")
{
    "develop" { dotnet pack "$csproj" -o . }
    default { exit }
}
$nupkg = (ls *.Sdk\*.nupkg).FullName
dotnet nuget push "$nupkg" -k "$env:MyGetKey" -s "$env:NuGetFeed"
$LASTEXITCODE = 0