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
    public static partial class Ks
    {
        private static AssemblyBuilder _assemblyBuilder = null;
        private static ModuleBuilder _moduleBuilder = null;
        private static Dictionary<Action<IIL>, IRunner> _runners = null;

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

    public interface IRunner
    {
        void Run();
    }

    public interface IIL
    {
        IILLocal DeclareLocal(Type type);
        IILLabel DefineLabel();
        void MarkLabel(IILLabel label);

        void Add();
        void Bge(IILLabel label);
        void Blt(IILLabel label);
        void Br(IILLabel label);
        void Call(Expression<Action> method);
        void Call<T1>(Expression<Action<T1>> method);
        void Call<T1, T2>(Expression<Action<T1, T2>> method);
        void Call<T1, T2, T3>(Expression<Action<T1, T2, T3>> method);
        void Call<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> method);
        void Dup();
        void Ldc_i4(int constant);
        void Ldloc(IILLocal local);
        void Pop();
        void Ret();
        void Stloc(IILLocal local);
    }

    public interface IILLocal { }
    public interface IILLabel { }
}
