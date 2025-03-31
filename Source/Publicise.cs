using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;
using Microsoft.Build.Framework;

namespace Publicise.MSBuild.Task
{
	public class Publicise : Microsoft.Build.Utilities.Task
	{
        const string OutputSuffix = "_public";

		public virtual ITaskItem[] InputAssemblies { get; set; }
		public virtual string OutputPath { get; set; }
		public virtual bool PubliciseCompilerGenerated { get; set; }

		public override bool Execute()
		{
			if (InputAssemblies == null)
			{
				Log.LogError($"No input assemblies decalred.");
				return false;
			}

			foreach (var assembly in InputAssemblies)
			{
				var path = assembly.ItemSpec;
				MakePublic(path, OutputPath);
			}

			return true;
		}

        bool MakePublic(string assemblyPath, string outputPath)
        {
            if (!File.Exists(assemblyPath))
            {
                Log.LogError($"Invalid path {assemblyPath}");
                return false;
            }

			string filename = Path.GetFileNameWithoutExtension(assemblyPath);
			string lastHash = null;
			string curHash = ComputeHash(assemblyPath);
			string hashPath = Path.Combine(outputPath, $"{filename}{OutputSuffix}.hash");

            if (File.Exists(hashPath))
                lastHash = File.ReadAllText(hashPath);

			if (curHash == lastHash)
			{
				Log.LogMessage("Public assembly is up to date.");
				return true;
			}

            Log.LogMessage($"Making a public assembly from {assemblyPath}");
			RewriteAssembly(assemblyPath).Write($"{Path.Combine(outputPath, filename)}{OutputSuffix}.dll");
            File.WriteAllText(hashPath, curHash);
			return true;
		}

		string ComputeHash(string assemblyPath)
        {
            StringBuilder res = new StringBuilder();

			using (var hash = SHA1.Create())
			{
				using (FileStream file = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    hash.ComputeHash(file);
                    file.Close();
                }

                foreach (byte b in hash.Hash)
                    res.Append(b.ToString("X2"));
            }

            return res.ToString();
        }

        // Based on https://gist.github.com/Zetrith/d86b1d84e993c8117983c09f1a5dcdcd
		ModuleDef RewriteAssembly(string assemblyPath)
        {
            ModuleDef assembly = ModuleDefMD.Load(assemblyPath);

            foreach (TypeDef type in assembly.GetTypes())
            {
				PubliciseType(type);

				foreach (MethodDef method in type.Methods)
					PubliciseMethod(method);

				foreach (FieldDef field in type.Fields)
					PubliciseField(field);
			}

			return assembly;

			void PubliciseType(TypeDef type)
			{
				if (ShouldPublicise(type.CustomAttributes))
				{
					type.Attributes &= ~TypeAttributes.VisibilityMask;
					if (type.IsNested)
						type.Attributes |= TypeAttributes.NestedPublic;
					else
						type.Attributes |= TypeAttributes.Public;
				}
			}

			void PubliciseMethod(MethodDef method)
			{
				if (ShouldPublicise(method.CustomAttributes) || (IsAutoProperty() && !IsPublicOverride()))
                {
                    method.Attributes &= ~MethodAttributes.MemberAccessMask;
                    method.Attributes |= MethodAttributes.Public;
                }

				// Auto property methods are compiler generated, but they are user facing and should be always be publicised.
				bool IsAutoProperty() => method.IsGetter || method.IsSetter;

				// Don't make methods which explicitly implement interfaces public 
				bool IsPublicOverride() => method.Overrides.Any();
			}

			void PubliciseField(FieldDef field)
			{
				if (ShouldPublicise(field.CustomAttributes))
                {
                    field.Attributes &= ~FieldAttributes.FieldAccessMask;
                    field.Attributes |= FieldAttributes.Public;
                }
            }

			bool ShouldPublicise(CustomAttributeCollection attributes) =>
				PubliciseCompilerGenerated || !attributes.Any(a => a.AttributeType.Name == nameof(CompilerGeneratedAttribute));
        }
	}
}