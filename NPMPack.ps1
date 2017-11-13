$tag = "master" 
Switch ("$(Build.SourceBranchName)")
{
    "master" { $tag = "master"  }
    "develop" { $tag = "develop" }
    "TypescriptClientGeneration" { $tag = "test"}
    default { exit }
}

cd *.Typescript/package
Set-Content -Value "//www.myget.org/F/hipapp/npm/:_authToken=$MyGetKey" -Path ./.npmrc
npm install

Switch ("$(Build.SourceBranchName)") {
    "develop"{	
		npm publish  --registry=https://www.myget.org/F/hipapp/npm/ --tag $tag	
	}
	"master" {
		$json = Get-Content -Path package.json | ConvertFrom-Json
		$env:version = $json.version
		$env:name = $json.name
        $env:tag = $tag
		npm --% dist-tag add %name%@%version% %tag%
	}
}

$LASTEXITCODE = 0
