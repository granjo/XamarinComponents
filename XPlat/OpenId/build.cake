
#load "../../common.cake"

var TARGET = Argument ("t", Argument ("target", "Default"));

var ANDROID_VERSION = "0.7.0";
var ANDROID_NUGET_VERSION = "0.7.0";
var IOS_VERSION = "0.92.0";
var IOS_NUGET_VERSION = "0.92.0";

var AAR_URL = string.Format ("https://bintray.com/openid/net.openid/download_file?file_path=net%2Fopenid%2Fappauth%2F{0}%2Fappauth-{0}.aar", ANDROID_VERSION);

var PODFILE = new List<string> {
	"platform :ios, '8.0'",
	"install! 'cocoapods', :integrate_targets => false",
	"target 'Xamarin' do",
	string.Format ("  pod 'AppAuth', '{0}'", IOS_VERSION),
	"end",
};

var buildSpec = new BuildSpec {
	Libs = new [] {
		new DefaultSolutionBuilder {
			SolutionPath = "./iOS/source/OpenId.AppAuth.iOS.sln",
			Configuration="Release",
			OutputFiles = new [] { 
				new OutputFileCopy {
					FromFile = "./iOS/source/OpenId.AppAuth.iOS/bin/Release/OpenId.AppAuth.iOS.dll",
				}
			}
		},
		new DefaultSolutionBuilder {
			SolutionPath = "./Android/source/OpenId.AppAuth.Android.sln",
			OutputFiles = new [] { 
				new OutputFileCopy {
					FromFile = "./Android/source/OpenId.AppAuth.Android/bin/Release/OpenId.AppAuth.Android.dll",
				}
			}
		}
	},

	NuGets = new [] {
		new NuGetInfo { NuSpec = "./nuget/OpenId.AppAuth.Android.nuspec", Version = ANDROID_NUGET_VERSION },
		new NuGetInfo { NuSpec = "./nuget/OpenId.AppAuth.iOS.nuspec", Version = IOS_NUGET_VERSION },
	},

	Samples = new [] {
		new IOSSolutionBuilder { SolutionPath = "./iOS/samples/OpenIdAuthSampleiOS.sln",  Configuration = "Release", Platform="iPhone" },
		new DefaultSolutionBuilder { SolutionPath = "./Android/samples/OpenIdAuthSampleAndroid.sln" }
	},

	// Components = new [] {
	// 	new Component { ManifestDirectory = "./component" }
	// }
};

Task ("externals-android")
	.WithCriteria (!FileExists ("./externals/android/appauth.aar"))
	.Does (() => 
{
	EnsureDirectoryExists ("./externals/android");

	DownloadFile (AAR_URL, "./externals/android/appauth.aar");
});
Task ("externals-ios")
	.WithCriteria (!FileExists ("./externals/ios/libAppAuth.a"))
	.Does (() => 
{
	if (CocoaPodVersion (new CocoaPodSettings ()) < new System.Version (1, 0))
		PODFILE.RemoveAt (1);

	EnsureDirectoryExists ("./externals/ios");

	FileWriteLines ("./externals/ios/Podfile", PODFILE.ToArray ());

	CocoaPodRepoUpdate ();
	
	CocoaPodInstall ("./externals/ios", new CocoaPodInstallSettings { NoIntegrate = true });

	XCodeBuild (new XCodeBuildSettings {
		Project = "./externals/ios/Pods/Pods.xcodeproj",
		Target = "AppAuth",
		Sdk = "iphoneos",
		Configuration = "Release",
	});

	XCodeBuild (new XCodeBuildSettings {
		Project = "./externals/ios/Pods/Pods.xcodeproj",
		Target = "AppAuth",
		Sdk = "iphonesimulator",
		Configuration = "Release",
	});

	RunLipoCreate ("./", 
		"./externals/ios/libAppAuth.a",
		"./externals/ios/build/Release-iphoneos/AppAuth/libAppAuth.a",
		"./externals/ios/build/Release-iphonesimulator/AppAuth/libAppAuth.a");
});
Task ("externals")
	.IsDependentOn ("externals-android")
	.IsDependentOn ("externals-ios");

Task ("clean").IsDependentOn ("clean-base").Does (() => 
{
	if (DirectoryExists ("./externals"))
		DeleteDirectory ("./externals", true);
});

SetupXamarinBuildTasks (buildSpec, Tasks, Task);

RunTarget (TARGET);