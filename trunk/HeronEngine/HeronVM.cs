﻿/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace HeronEngine
{
    /// <summary>
    /// This represents the current state of the Heron virtual machine. 
    /// </summary>
    public class VM
    {
        #region helper classes
        /// <summary>
        /// Used for creation and deletion of scopes.
        /// Do not instantiate directly, only VM creates this.
        /// <seealso cref="HeronVm.CreateFrame"/>
        /// </summary>
        public class DisposableScope : IDisposable
        {
            VM vm;
            public DisposableScope(VM vm)
            {
                this.vm = vm;
                vm.PushScope();
            }
            public DisposableScope(VM vm, Scope scope)
            {
                this.vm = vm;
                vm.PushScope(scope);
            }
            public void Dispose()
            {
                vm.PopScope();
            }
        }

        /// <summary>
        /// Helper class for the creation and deletion of frames.
        /// Do not instantiate directly, only VM creates this.
        /// <seealso cref="HeronVm.CreateFrame"/>
        /// </summary>
        public class DisposableFrame : IDisposable
        {
            VM vm;
            public DisposableFrame(VM vm, FunctionDefn def, ClassInstance ci)
            {
                this.vm = vm;
                vm.PushNewFrame(def, ci);
            }
            public void Dispose()
            {
                vm.PopFrame();
            }
        }
        #endregion helper classes

        #region fields
        /// <summary>
        /// The current result value
        /// </summary>
        private HeronValue result;

        /// <summary>
        /// A list of call stack frames (also called activation records)
        /// </summary>
        private Stack<Frame> frames = new Stack<Frame>();

        /// <summary>
        /// Currently executing program. It contains global names.
        /// </summary>
        private ProgramDefn program;

        /// <summary>
        /// A flag that is set to true when a return statement occurs. 
        /// </summary>
        bool bReturning = false;
        #endregion

        #region properties
        /// <summary>
        /// Returns the moduleDef definition associated with the current frame.
        /// </summary>
        /// <returns></returns>
        public ModuleDefn CurrentModuleDef
        {
            get
            {
                Frame f = CurrentFrame;
                ModuleDefn m = f.moduleDef;
                return m;
            }
        }

        /// <summary>
        /// Returns the global moduleDef definition
        /// </summary>
        public ModuleDefn GlobalModuleDef
        {
            get
            {
                if (program == null)
                    return null;
                return program.GetGlobal();
            }
        }

        /// <summary>
        /// Returns the currently executing program.
        /// </summary>
        /// <returns></returns>
        public ProgramDefn Program
        {
            get
            {
                return program;
            }
        }
        #endregion

        #region construction, initialization, and finalization
        /// <summary>
        /// Constructor
        /// </summary>
        public VM()
        {
            InitializeVM();
        }

        public void InitializeVM()
        {
            program = new ProgramDefn("_program_");

            // Clear all the frames
            frames.Clear();
            result = null;

            // Push an empty first frame and scope
            PushNewFrame(null, null);
            PushScope();

            // Load the global types
            foreach (HeronType t in program.GetGlobal().GetTypes())
                AddVar(t.name, t);
        }
        #endregion

        #region evaluation functions
        public HeronValue EvalString(string s)
        {
            Expression x = HeronCodeModelBuilder.ParseExpr(program, s);
            x.ResolveAllTypes(program.GetGlobal());
            return Eval(x); ;
        }

        public ModuleDefn LoadModule(string sFileName)
        {
            ModuleDefn m = HeronCodeModelBuilder.ParseFile(program, sFileName);
            program.AddModule(m);
            string sFileNameAsModuleName = sFileName.Replace('/', '.').Replace('\\', '.');
            sFileNameAsModuleName = Path.GetFileNameWithoutExtension(sFileNameAsModuleName);
            int n = sFileNameAsModuleName.IndexOf(m.name);
            if (n + m.name.Length != sFileNameAsModuleName.Length)
            {
                throw new Exception("The module name '" + m.name + "' does not correspond to the file name '" + sFileName + "'");
            }
            return m;
        }

        public bool FindModulePathInDir(string sModule, string sDir, out string sResult)
        {
            foreach (string sExt in Config.extensions)
            {
                sResult = sDir + "\\" + sModule + sExt;
                if (File.Exists(sResult)) return true;
            }
            sResult = "";
            return false;
        }

        public string FindModulePath(string sModule, string sCurrentPath)
        {
            string sResult;
            if (FindModulePathInDir(sModule, sCurrentPath, out sResult))
                return sResult;

            foreach (String sPath in Config.inputPath)
            {
                if (FindModulePathInDir(sModule, sPath, out sResult))
                    return sResult;
            }

            throw new Exception("Could not find module : " + sModule);
        }

        private void LoadDependentModules(string sFile)
        {
            // Load any dependent modules 
            List<string> modules = new List<string>(program.GetUnloadedDependentModules());
            while (modules.Count > 0)
            {
                foreach (string s in modules)
                {
                    string sPath = FindModulePath(s, Path.GetDirectoryName(sFile));
                    LoadModule(sPath);
                }

                modules = new List<string>(program.GetUnloadedDependentModules());
            }
        }

        public void ResolveModules()
        {
            foreach (ModuleDefn md in program.GetModules())
            {
                md.ResolveTypes();
                foreach (ClassDefn c in md.GetClasses())
                    c.VerifyInterfaces();
            }
        }

        public void EvalFile(string sFile)
        {
            InitializeVM();
            ModuleDefn m = LoadModule(sFile);
            LoadDependentModules(sFile);
            ResolveModules();
            RunModule(m);
        }

        public void RunMeta(ModuleInstance m)
        {
            HeronValue f = m.GetFieldOrMethod("Meta");
            if (f == null)
                return;
            f.Apply(this, new HeronValue[] { });
        }

        public void RunMain(ModuleInstance m)
        {
            HeronValue f = m.GetFieldOrMethod("Main");
            if (f == null)
                throw new Exception("Could not find a 'Main' method to run");
            f.Apply(this, new HeronValue[] { });
        }

        public void RunModule(ModuleDefn m)
        {
            ModuleInstance mi = m.Instantiate(this, new HeronValue[] { }, null) as ModuleInstance;
            RunMeta(mi);
            RunMain(mi);            
        }

        /// <summary>
        /// Evaluates a list expression as an IHeronEnumerator
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public SeqValue EvalList(Expression list)
        {
            HeronValue tmp = Eval(list);
            if (!(tmp is SeqValue))
                throw new Exception("Expected an enumerable value");
            return (tmp as SeqValue);
        }

        /// <summary>
        /// Evaluates a list expression, converting it into an IEnumerable&lt;HeronValue&gt;
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public IEnumerable<HeronValue> EvalListAsDotNet(Expression list)
        {
            return new HeronToEnumeratorAdapter(this, EvalList(list));
        }

        /// <summary>
        /// Call this instead of "Expression.Eval()", this way you can set
        /// breakpoints etc.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public HeronValue Eval(Expression value)
        {
            return value.Eval(this);
        }

        /// <summary>
        /// Call this instead of "Stsatement.Eval()", this way you can set
        /// breakpoints etc.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Eval(Statement statement)
        {
            statement.Eval(this);
        }
        #endregion
        
        #region scope and frame management
        /// <summary>
        /// Gets the current activation record.
        /// </summary>
        /// <returns></returns>
        public Frame CurrentFrame
        {
            get
            {               
                return frames.Peek();
            }
        }

        /// <summary>
        /// Creates a new lexical scope. Roughly corresponds to an open brace ('{') in many languages.
        /// </summary>
        public void PushScope()
        {
            PushScope(new Scope());
        }

        /// <summary>
        /// Creates a new scope, with a predefined set of variable names. Useful for function arguments
        /// or class fields.
        /// </summary>
        /// <param name="scope"></param>
        public void PushScope(Scope scope)
        {
            frames.Peek().AddScope(scope);
        }

        /// <summary>
        /// Removes the current scope. Correspond roughly to a closing brace ('}').
        /// </summary>
        public void PopScope()
        {
            frames.Peek().PopScope();
        }

        /// <summary>
        /// Creates a scope, and when DisposableScope.Dispose is called removes it
        /// Normally you would use this as follows:
        /// <code>
        ///     using (vm.CreateScope())
        ///     {
        ///       ...
        ///     }
        /// </code>
        /// </summary>
        /// <returns></returns>
        public DisposableScope CreateScope()
        {
            return new DisposableScope(this);
        }

        /// <summary>
        /// Creates a scope, and when DisposableScope.Dispose is called removes it
        /// Normally you would use this as follows:
        /// <code>
        ///     using (vm.CreateScope(scope))
        ///     {
        ///       ...
        ///     }
        /// </code>
        /// </summary>
        /// <returns></returns>
        public DisposableScope CreateScope(Scope scope)
        {
            return new DisposableScope(this, scope);
        }

        /// <summary>
        /// Called when a new function execution starts.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="self"></param>
        public void PushNewFrame(FunctionDefn f, ClassInstance self)
        {
            frames.Push(new Frame(f, self));
        }

        /// <summary>
        /// Inidcates the current activation record is finished.
        /// </summary>
        public void PopFrame()
        {
            // Reset the returning flag, to indicate that the returning operation is completed. 
            frames.Pop();
        }

        /// <summary>
        /// Creates a new frame, and returns a frame manager, which will release the frame
        /// on Dispose.
        /// </summary>
        /// <param name="fun"></param>
        /// <param name="classInstance"></param>
        /// <returns></returns>
        public DisposableFrame CreateFrame(FunctionDefn fun, ClassInstance classInstance)
        {
            return new DisposableFrame(this, fun, classInstance);
        }
        #endregion

        #region control flow
        /// <summary>
        /// This is used by loops over statements to check whether a return statement, a break 
        /// statement, or a throw statement was called. Currently only return statements are supported.
        /// </summary>
        /// <returns></returns>
        public bool ShouldExitScope()
        {
            return bReturning;
        }

        /// <summary>
        /// Called by a return statement. Sets the function result, and sets a flag to indicate 
        /// to executing statement groups that execution should terminate.
        /// </summary>
        /// <param name="ret"></param>
        public void Return(HeronValue ret)
        {
            Trace.Assert(!bReturning, "internal error, returning flag was not reset");
            bReturning = true;
            result = ret;
        }

        /// <summary>
        /// Returns the return result, and sets it to null.
        /// </summary>
        /// <returns></returns>
        public HeronValue GetAndResetResult()
        {
            HeronValue r = result;
            result = null;
            bReturning = false;
            return r;
        }
        #endregion

        #region variables, fields, and name management
        /// <summary>
        /// Assigns a value a variable name in the current environment.
        /// The name must already exist
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void SetVar(string s, HeronValue o)
        {
            Trace.Assert(o != null);
            frames.Peek().SetVar(s, o);
        }

        /// <summary>
        /// Returns true if the name is that of a variable in the local scope
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasVar(string name)
        {
            return frames.Peek().HasVar(name);
        }

        /// <summary>
        /// Returns true if the name is a field in the current object scope.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            return frames.Peek().HasField(name);
        }

        /// <summary>
        /// Looks up a name as a field in the current object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HeronValue GetField(string name)
        {
            return frames.Peek().GetField(name);
        }

        /// <summary>
        /// Assigns a value to a field.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void SetField(string s, HeronValue o)
        {
            Trace.Assert(o != null);
            frames.Peek().SetField(s, o);
        }

        /// <summary>
        /// Looks up the value or type associated with the name.
        /// Looks in each scope in the currenst stack frame until a match is found.
        /// If no match is found then the various moduleDef scopes are searched.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public HeronValue LookupName(string s)
        {
            HeronValue r = frames.Peek().LookupName(s);
            if (r != null)
                return r;

            if (CurrentModuleDef != null)
                foreach (HeronType t in CurrentModuleDef.GetTypes())
                    if (t.name == s)
                        return t;

            if (GlobalModuleDef != null)
                foreach (HeronType t in GlobalModuleDef.GetTypes())
                    if (t.name == s)
                        return t;

            throw new Exception("Could not find '" + s + "' in the environment");
        }

        /// <summary>
        /// Creates a new variable name in the current scope.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="o"></param>
        public void AddVar(string s, HeronValue o)
        {
            Assure(o != null, "Null is not an acceptable value");
            frames.Peek().AddVar(s, o);
        }
        
        /// <summary>
        /// Add a group of variables at once
        /// </summary>
        /// <param name="vars"></param>
        public void AddVars(Scope vars)
        {
            foreach (string name in vars.Keys)
                AddVar(name, vars[name]);
        }        
        #endregion 

        #region utility functions
        /// <summary>
        /// Throw an exception if condition is not true. However, not an assertion. 
        /// This is used to check for exceptional run-time condition.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="s"></param>
        private void Assure(bool b, string s)
        {
            if (!b)
                throw new Exception("error occured: " + s);
        }

        /// <summary>
        /// Returns a textual representation of the environment. 
        /// Used primarily for debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Frame f in frames)
                sb.Append(f.ToString());
            return sb.ToString();
        }
        #endregion 

        /// <summary>
        /// Returns all frames, useful for creating a call stack 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Frame> GetFrames()
        {
            return frames;
        }

        /// <summary>
        /// Convenience function for invoking a method on an object
        /// </summary>
        /// <param name="self"></param>
        /// <param name="s"></param>
        /// <param name="funcs"></param>
        /// <returns></returns>
        public HeronValue Invoke(HeronValue self, string s, HeronValue[] args)
        {
            HeronValue f = self.GetFieldOrMethod(s);
            HeronValue r = f.Apply(this, new HeronValue[] { });
            return r;
        }

        /// <summary>
        /// Convenience function for invoking methods without arguments
        /// </summary>
        /// <param name="self"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public HeronValue Invoke(HeronValue self, string s)
        {
            return Invoke(self, s, new HeronValue[] { });
        }
    }
}