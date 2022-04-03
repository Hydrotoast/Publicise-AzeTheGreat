# Publicise
An MSBuild Task that creates copies of assemblies in which all members are public.

This is useful for easily referencing non-public members without the hassles or associated performance hits of reflection.

## Usage

1. Install the [NuGet Package](https://www.nuget.org/packages/Aze.Publicise.MSBuild.Task).

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
   	       PathTo/Assembly1.dll;
	              PathTo/Assembly2.dll"/>
      </ItemGroup>
   
      <Publicise
         InputAssemblies="@(PubliciseInputAssemblies)"
         OutputPath="../lib/"
         PubliciseCompilerGenerated="true"/>
   </Target>
   ```

3. Ensure Unsafe Code is enabled.  If it is not enabled, runtime access violations will be encountered in certain situations.

4. Change your project references to `AssemblyName_public.dll`, which will be located in the `OutputPath` folder (after the task is executed at least once).

5. The task stores a hash of the assemblies to reduce build time when there is nothing new to compute.  If you need to re-publicise the assembly when it has not changed, simply delete the corresponding `x.hash` file located next to the `x_public.dll` assembly.

## Credits

Code based on [this task](https://github.com/rwmt/Publicise), with original idea (to my knowledge), from [this repository](https://github.com/CabbageCrow/AssemblyPublicizer).
