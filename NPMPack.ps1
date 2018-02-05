Switch ("$env:Build_SourceBranchName")
{
    "master" { $env:tag = "master"  }
    "develop" { $env:tag = "develop" }
    default { exit }
}

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



Switch ("$env:Build_SourceBranchName") 
{
    "develop"{		
		npm --% publish --registry=%NPMFeed% --tag %tag%	
	}

	"master" {		
		$env:version = $json.version
		$env:name = $json.name
		npm --% dist-tag add %name%@%version% %tag% --registry=%NPMFeed%
	}
}

$LASTEXITCODE = 0
