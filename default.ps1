Properties {
	$configuration = "Release"
	$solutionDir = Split-Path $psake.build_script_file
	$buildDir = "$solutionDir\_build"
	$logsDir = "$buildDir\logs"
	$packageDir = "$buildDir\packages"
	$packageVersion = "0.0.1"
}

Task default -Depends Clean, Build, Package

Task Clean {
	Exec { msbuild StatsDPerfMon.sln -t:clean -p:"Configuration=$configuration" }
	if (Test-Path $buildDir) {
		Remove-Item $buildDir -recurse -force
	}
	if (Test-Path $packageDir) {
		Remove-Item $packageDir -recurse -force
	}
	mkdir $buildDir
	mkdir $packageDir
}

Task Build {
	Exec { msbuild StatsDPerfMon.sln -p:"Configuration=$configuration" }
}

Task Package {
	Exec { .nuget\nuget.exe pack .\StatsDPerfMon\StatsDPerfMon.csproj -Prop Configuration=$configuration -out "$packageDir" -version $packageVersion }
}
