svn checkout https://wpftoolkit.svn.codeplex.com/svn/Main src
sed -i 's/.*AssemblyDelaySign.*//' src/Source/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/Properties/AssemblyInfo.cs
sed -i 's/.*AssemblyKeyFile.*//' src/Source/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/Properties/AssemblyInfo.cs
sed -i 's/.*AssemblyKeyName.*//' src/Source/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/Properties/AssemblyInfo.cs
msbuild.exe src/Source/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/Xceed.Wpf.Toolkit.csproj /p:Configuration=release
cp src/Source/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/bin/release/* ./
