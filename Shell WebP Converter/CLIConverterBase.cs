using System;
using System.IO;

namespace Shell_WebP_Converter.CLI
{
    internal abstract class CLIConverterBase
    {
        protected string Input { get; set; }
        protected string Output { get; set; }
        protected bool DeleteOriginal { get; set; }
        protected bool OverwriteFiles { get; set; }

        protected CLIConverterBase(string input, string output, bool deleteOriginal, bool overwriteFiles)
        {
            Input = input;
            Output = output;
            DeleteOriginal = deleteOriginal;
            OverwriteFiles = overwriteFiles;
        }

        public virtual void Run()
        {
            if (File.Exists(Input))
            {
                if (Output.Length == 0)
                {
                    Output = GenerateDefaultOutputPath(Input);
                }

                if (!OverwriteFiles)
                {
                    Output = ConverterCommon.GetUniqueFilePath(Output);
                }

                try
                {
                    using (MemoryStream ms = ConvertFile(Input))
                    using (FileStream fs = File.Create(Output))
                    {
                        ms.CopyTo(fs);
                    }

                    if (DeleteOriginal && Input != Output)
                    {
                        File.Delete(Input);
                    }
                }
                catch (Exception ex)
                {
                    App.Log(Input + " | " + ex.Message);
                    throw;
                }
            }
            else
            {
                throw new Exception("Input does not exist");
            }
        }

        protected abstract string GenerateDefaultOutputPath(string inputFile);

        protected abstract MemoryStream ConvertFile(string inputFile);

        protected struct ProgressCounter
        {
            public int tasksToBeDone;
            public int tasksCompleted;
            public float progress
            {
                get
                {
                    return MathF.Round((float)tasksCompleted / (float)tasksToBeDone * 100, 1);
                }
            }
        }
    }
}
