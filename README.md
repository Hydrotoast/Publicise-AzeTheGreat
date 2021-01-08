# Publicise
An MSBuild Task that creates a copies of an assemblies in which all members are public.

This is useful for easily referencing non-public members without the hassles or associated performance hits of reflection.

## Usage

1. Install the [NuGet Package](https://www.nuget.org/packages/Aze.Publicise.MSBuild.Task/1.0.0).

2. Create the MSBuild Target.  Properties are:  
`InputAssemblies`: Assemblies to be publicized.  
`OutputPath`: The folder to which publicized assemblies will be saved.  
`PubliciseCompilerGenerated`: If true, compiler generated members will be publicized.  Defaults to false.

   Recommended Targets:  
`AfterTargets="Clean"`: Will only execute on a clean or rebuild.  
`BeforeTargets="BeforeBuild"`: Will execute before every build.  

   Complete example of usage:
   ```
   <Target Name="Publicise" AfterTargets="Clean">
      <ItemGroup>
         <PubliciseInputAssemblies
            Include="
   	       PathToAssembly1;
	       PathToAssembly2"/>
      </ItemGroup>
   
      <Publicise
         InputAssemblies="@(PubliciseInputAssemblies)"
         OutputPath="../lib/"
         PubliciseCompilerGenerated="true"/>
   </Target>
   ```

3. Ensure Unsafe Code is enabled.  If it is not enabled, runtime access violations will be encountered in certain situations.

4. Change your project references to `AssemblyName_public.dll`, which will be located in the `OutputPath` folder (after the task is executed at least once).
