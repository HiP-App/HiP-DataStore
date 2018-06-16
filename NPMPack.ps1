if(Test-Path *.Sdk/*.Sdk.csproj)
{
	$file = [xml](gc *.Sdk/*.Sdk.csproj)
	$sdkVersion = $file.Project.PropertyGroup.Version
}

cd *.Typescript/package
$regUrl = $env:NPMFeed.Replace("http:","").Replace("https:","")
Set-Content -Value "$($regUrl):_authToken=$env:MyGetKey" -Path ./.npmrc

$json = Get-Content -Path package.json | ConvertFrom-Json

if($sdkVersion){
    $json.version = "$sdkVersion".Replace(" Version","")
    $json | ConvertTo-Json -depth 100 | Set-Content "package.json"
}

npm install
	
npm --% publish --registry=%NPMFeed%

$LASTEXITCODE = 0
