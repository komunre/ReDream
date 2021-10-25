﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Jint;
using Jint.Native;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.CodeDom;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;

namespace ReDream.Shared
{
    public class CompileGameException : Exception
    {
        public CompileGameException() : base("Unable to compile game")
        {

        }
    }
    public class ScriptWorker
    {
        string[] files;
        public static string workingPath;
        public static string ClientPath;
        public Engine eng;
        string[] blacklist;
        public ReGame game;
        private List<Assembly> _assemblies = new();
        private Dictionary<GameObject, Type> _gameObjects = new();
        public List<string> AdditionalTypes = new();
        private List<string> CompiledTypes = new();
        public bool Client = false;
        private AssemblyLoadContext _context = new AssemblyLoadContext("game", true);
        public bool Compiled = false;
        public bool CompilationStarted = false;
        private int _filesLoaded = 0;
        private int _filesToLoad = 0;
        public ScriptWorker()
        {
            eng = new Engine(options =>
            {
                options.LimitRecursion(100);
            });
            eng.SetValue("network", new Networking());
            eng.SetValue("remath", new ReMath());
            eng.SetValue("tools", typeof(ReTools));
            game = new ReGame();
            eng.SetValue("game", game);
            eng.SetValue("gameObj", typeof(GameObject));
            eng.SetValue("getFile", new Func<string, JsValue>(GetFile));
        }
        public void AddType(string type)
        {
            AdditionalTypes.Add(type);
        }
        public void Initialize(string path, string clientPath, bool client)
        {
            workingPath = path;
            ClientPath = clientPath;
            Client = client;
            //blacklist = File.ReadAllText(workingPath + "/blacklist.txt").Split("\n");
            //ProcessDirectory(workingPath, true);
            ReloadCode();
        }

        public void ReloadCode()
        {
            lock (_gameObjects)
            {
                if (CompilationStarted && !Compiled) return;
                CompilationStarted = true;
                _gameObjects.Clear();
                _context.Unload();
                _context = new AssemblyLoadContext("game", true);
                LoadDirectory(workingPath, true);
                ProcessCSharp(true);
            }
        }
        public void Update()
        {
            if (workingPath == null) return;
            //ProcessFile(workingPath + "/index.js");
            //ProcessDirectory(workingPath, false);
            if (_filesLoaded >= _filesToLoad)
            {
                Compiled = true;
                CompilationStarted = false;
                ProcessCSharp(false);
            }
        }

        protected JsValue GetFile(string path)
        {
            try
            {
                var what = eng.Execute(File.ReadAllText(workingPath + "/" + path));
                return what.GetValue("module.exports");
            }
            catch (Exception e)
            {
                ReTools.Log("ERROR: " + e.Message);
            }
            return null;
        }

        protected void LoadDirectory(string targetDirectory, bool start)
        {
            if (Path.GetFullPath(targetDirectory).Equals(Path.GetFullPath(ClientPath)) && !Client) return;
            var progressing = false;
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            _filesToLoad += fileEntries.Length;
            foreach (string fileName in fileEntries)
                CompileFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                progressing = true;
                LoadDirectory(subdirectory, start);
            }
        }

        protected void CompileFile(string fileName)
        {
            var dotSplit = fileName.Split(".");
            var format = dotSplit[dotSplit.Length - 1];
            if (format != "cs")
            {
                _filesToLoad--;
                return;
            }

            var rel = Path.GetFullPath(fileName);
            var splitted = rel.Split("/");
            if (splitted.Length < 2)
            {
                splitted = rel.Split("\\");
            }
            var outName = splitted[splitted.Length - 1] + ".dll";
            var refList = new List<string>();
            refList.AddRange( new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(ReTools).GetTypeInfo().Assembly.Location,
                typeof(GameObject).GetTypeInfo().Assembly.Location,
                typeof(ReGame).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll")
                }
            );
            refList.AddRange(AdditionalTypes);
            refList.AddRange(CompiledTypes);
            var refPaths = refList.ToArray();
            var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(fileName));
            var compilation = CSharpCompilation.Create(outName, new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ms = new FileStream(outName, FileMode.Create);
            var result = compilation.Emit(ms);

            ms.Close();

            if (!result.Success)
            {
                ReTools.Log("COMPILATION ERROR");
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
                _filesToLoad--;
            }
            else
            {
                ReTools.Log("Compiled!");

                Assembly assembly = _context.LoadFromAssemblyPath(Path.GetFullPath(outName));
                var objects = assembly.ExportedTypes.ToList().Where(t => t.BaseType.IsAssignableFrom(typeof(GameObject)));
                foreach (var obj in objects)
                {
                    if (!obj.IsAbstract)
                    {
                        var target = Activator.CreateInstance(obj);
                        _gameObjects.Add(target as GameObject, obj);
                    }
                    CompiledTypes.Add(assembly.Location);
                }

                _filesLoaded++;
            }

            /*var codeProvider = new CSharpCodeProvider();
            var parameters = new CompilerParameters(null, outName, true);
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = false;
            var netstandard = Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
            parameters.ReferencedAssemblies.Add(netstandard.Location);
            var results = codeProvider.CompileAssemblyFromFile(parameters, fileName);
            results.Errors.Cast<CompilerError>().ToList().ForEach(error => Console.WriteLine(error.ErrorText));
            LoadFile(outName);*/
        }

        protected void LoadFile(string fileName)
        {
            
        }


        protected void ProcessCSharp(bool start)
        {
            foreach (var obj in _gameObjects)
            {
                if (!start)
                {
                    var meth = obj.Value.GetMethod("Update");
                    meth.Invoke(obj.Key, new[] { game });
                }
                else
                {
                    var meth = obj.Value.GetMethod("Start");
                    meth.Invoke(obj.Key, new[] { game });
                }
            }
        }
    }
}
