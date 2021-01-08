﻿using System.IO;
using System.Security.Cryptography;
using System.Text;
using dnlib.DotNet;
using Microsoft.Build.Framework;

namespace Publicise.MSBuild.Task
{
    public class Publicise : Microsoft.Build.Utilities.Task
    {
        const string outputSuffix = "_public";

		public virtual ITaskItem[] InputAssemblies { get; set; }
        public virtual string OutputPath { get; set; }

        public override bool Execute()
        {
			if(InputAssemblies == null)
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
			string hashPath = Path.Combine(outputPath, $"{filename}{outputSuffix}.hash");

			if (File.Exists(hashPath))
				lastHash = File.ReadAllText(hashPath);

            if (curHash == lastHash)
			{
				Log.LogMessage("Public assembly is up to date.");
                return true;
			}

			Log.LogMessage($"Making a public assembly from {assemblyPath}");
			RewriteAssembly(assemblyPath).Write($"{Path.Combine(outputPath, filename)}{outputSuffix}.dll");
			File.WriteAllText(hashPath, curHash);
            return true;
		}

		static string ComputeHash(string assemblyPath)
		{
			StringBuilder res = new StringBuilder();

			using(var hash = SHA1.Create())
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
		static ModuleDef RewriteAssembly(string assemblyPath)
		{
			ModuleDef assembly = ModuleDefMD.Load(assemblyPath);

			foreach (TypeDef type in assembly.GetTypes())
			{
				type.Attributes &= ~TypeAttributes.VisibilityMask;

				if (type.IsNested)
				{
					type.Attributes |= TypeAttributes.NestedPublic;
				}
				else
				{
					type.Attributes |= TypeAttributes.Public;
				}

				foreach (MethodDef method in type.Methods)
				{
					method.Attributes &= ~MethodAttributes.MemberAccessMask;
					method.Attributes |= MethodAttributes.Public;
				}

				foreach (FieldDef field in type.Fields)
				{
					field.Attributes &= ~FieldAttributes.FieldAccessMask;
					field.Attributes |= FieldAttributes.Public;
				}
			}

			return assembly;
		}
	}
}
