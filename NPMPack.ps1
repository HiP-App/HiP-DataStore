Switch ("$env:Build_SourceBranchName")
{
    "master" { $env:tag = "master"  }
    "develop" { $env:tag = "develop" }
    default { exit }
}

cd *.Typescript/package
Set-Content -Value "//www.myget.org/F/hipapp/npm/:_authToken=$env:MyGetKey" -Path ./.npmrc
npm install

Switch ("$env:Build_SourceBranchName") 
{
    "develop"{		
		npm --% publish  --registry=%MyGetFeed% --tag %tag%	
	}

	"master" {
		$json = Get-Content -Path package.json | ConvertFrom-Json
		$env:version = $json.version
		$env:name = $json.name
		npm --% dist-tag add %name%@%version% %tag%
	}
}

$LASTEXITCODE = 0
