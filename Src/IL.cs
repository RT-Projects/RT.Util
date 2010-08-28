using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using RT.Util;

namespace RT.KitchenSink
{
    /// <summary>Provides a collection on unconnected pieces of functionality.</summary>
    public static partial class Ks
    {
        private static AssemblyBuilder _assemblyBuilder = null;
        private static ModuleBuilder _moduleBuilder = null;
        private static Dictionary<Action<IIL>, IRunner> _runners = null;

        /// <summary>Executes a sequence of IL instructions specified in a lambda block. See remarks for how to use.</summary>
        /// <param name="code">Provides a delegate that specifies the IL instructions to execute.</param>
        /// <remarks>Any code in the provided delegate that is not a call to the <see cref="IIL"/> interface instance provided,
        /// will be executed before all the IL, not interleaved with it as specified. Think of the delegate method as one that constructs
        /// the IL code before it is executed.</remarks>
        public static void RunIL(Action<IIL> code)
        {
            if (_assemblyBuilder == null)
            {
                _assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName("il_runner_assembly"), AssemblyBuilderAccess.Run);
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("il_runner_module");
                _runners = new Dictionary<Action<IIL>, IRunner>();
            }

            if (!_runners.ContainsKey(code))
            {
                var typeBuilder = _moduleBuilder.DefineType("il_runner_class_" + _runners.Count, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(object), new Type[] { typeof(IRunner) });
                var methodBuilder = typeBuilder.DefineMethod("Run", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, typeof(void), Type.EmptyTypes);
                var il = methodBuilder.GetILGenerator();
                var iil = new iller(il);
                code(iil);
                var type = typeBuilder.CreateType();
                _runners[code] = (IRunner) Activator.CreateInstance(type);
            }

            _runners[code].Run();
        }

        private class iller : IIL
        {
            private ILGenerator _il;
            public iller(ILGenerator il) { _il = il; }

            public IILLocal DeclareLocal(Type type) { return new ilLocal(_il.DeclareLocal(type)); }
            public IILLabel DefineLabel() { return new ilLabel(_il.DefineLabel()); }

            public void MarkLabel(IILLabel label) { _il.MarkLabel(((ilLabel) label).Label); }
            public void Add() { _il.Emit(OpCodes.Add); }
            public void Bge(IILLabel label) { _il.Emit(OpCodes.Bge, ((ilLabel) label).Label); }
            public void Blt(IILLabel label) { _il.Emit(OpCodes.Blt, ((ilLabel) label).Label); }
            public void Br(IILLabel label) { _il.Emit(OpCodes.Br, ((ilLabel) label).Label); }
            private void emitCall(OpCode opcode, LambdaExpression expr)
            {
                if (!(expr.Body is MethodCallExpression))
                    throw new InvalidOperationException("Method call expression expected.");
                _il.Emit(opcode, ((MethodCallExpression) expr.Body).Method);
            }
            public void Call(Expression<Action> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1>(Expression<Action<T1>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, T2>(Expression<Action<T1, T2>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, T2, T3>(Expression<Action<T1, T2, T3>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> method) { emitCall(OpCodes.Call, method); }
            public void Call<TResult>(Expression<Func<TResult>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, TResult>(Expression<Func<T1, TResult>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> method) { emitCall(OpCodes.Call, method); }
            public void Call<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> method) { emitCall(OpCodes.Call, method); }
            public void Dup() { _il.Emit(OpCodes.Dup); }
            public void Ldc_i4(int constant) { _il.Emit(OpCodes.Ldc_I4, constant); }
            public void Ldloc(IILLocal local) { _il.Emit(OpCodes.Ldloc, ((ilLocal) local).Local); }
            public void Pop() { _il.Emit(OpCodes.Pop); }
            public void Ret() { _il.Emit(OpCodes.Ret); }
            public void Stloc(IILLocal local) { _il.Emit(OpCodes.Stloc, ((ilLocal) local).Local); }
        }

        private class ilLabel : IILLabel { private Label _label; public ilLabel(Label label) { _label = label; } public Label Label { get { return _label; } } }
        private class ilLocal : IILLocal { private LocalBuilder _local; public ilLocal(LocalBuilder local) { _local = local; } public LocalBuilder Local { get { return _local; } } }
    }

    /// <summary>Provides an interface for running a method. This is used internally by <see cref="Ks.RunIL"/>,
    /// but needs to be public in order for the dynamic assembly to implement it.</summary>
    public interface IRunner
    {
        /// <summary>Runs something.</summary>
        void Run();
    }

    /// <summary>Provides methods to construct a sequence of IL instructions.</summary>
    public interface IIL
    {
        /// <summary>Declares a local variable.</summary>
        /// <param name="type">Type of the local variable to declare.</param>
        /// <returns>An object that can be used in methods such as <see cref="Ldloc"/>.</returns>
        IILLocal DeclareLocal(Type type);
        /// <summary>Defines a goto label.</summary>
        /// <returns>An object that can be used in methods such as <see cref="Br"/>.</returns>
        IILLabel DefineLabel();
        /// <summary>Defines the position of a goto label within the sequence of instructions.</summary>
        /// <param name="label">The goto label defined by <see cref="DefineLabel"/>.</param>
        void MarkLabel(IILLabel label);

        /// <summary>Add instruction.</summary>
        void Add();
        /// <summary>Bge instruction.</summary>
        void Bge(IILLabel label);
        /// <summary>Blt instruction.</summary>
        void Blt(IILLabel label);
        /// <summary>Br instruction.</summary>
        void Br(IILLabel label);
        /// <summary>Call instruction.</summary>
        void Call(Expression<Action> method);
        /// <summary>Call instruction.</summary>
        void Call<T1>(Expression<Action<T1>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, T2>(Expression<Action<T1, T2>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, T2, T3>(Expression<Action<T1, T2, T3>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> method);
        /// <summary>Call instruction.</summary>
        void Call<TResult>(Expression<Func<TResult>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, TResult>(Expression<Func<T1, TResult>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> method);
        /// <summary>Call instruction.</summary>
        void Call<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> method);
        /// <summary>Dup instruction.</summary>
        void Dup();
        /// <summary>Ldc_i4 instruction.</summary>
        void Ldc_i4(int constant);
        /// <summary>Ldloc instruction.</summary>
        void Ldloc(IILLocal local);
        /// <summary>Pop instruction.</summary>
        void Pop();
        /// <summary>Ret instruction.</summary>
        void Ret();
        /// <summary>Stloc instruction.</summary>
        void Stloc(IILLocal local);
    }

    /// <summary>Provides an object that represents a local variable.</summary>
    /// <seealso cref="IIL"/>
    public interface IILLocal { }
    /// <summary>Provides an object that represents a goto label.</summary>
    /// <seealso cref="IIL"/>
    public interface IILLabel { }
}
