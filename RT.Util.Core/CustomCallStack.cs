using System;
using System.Collections.Generic;

namespace RT.Util
{
    /// <summary>
    ///     Provides static functionality to execute work on an unlimited call stack, which is not limited to 1 MB as the
    ///     standard call stack is.</summary>
#if EXPORT_UTIL
    public
#endif
    static class CustomCallStack
    {
        /// <summary>
        ///     Runs the specified work on a custom call stack, which is not limited to 1 MB as the standard call stack is.
        ///     See remarks for details.</summary>
        /// <typeparam name="T">
        ///     Type of result to compute.</typeparam>
        /// <param name="node">
        ///     Work to be executed.</param>
        /// <returns>
        ///     The result of the computation.</returns>
        /// <remarks>
        ///     <para>
        ///         <see cref="CustomCallStack.Run{T}"/> expects a delegate that is structured in such a way that it:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             always knows at what state in the computation it is each time it is called;</description></item>
        ///         <item><description>
        ///             returns a <see cref="WorkStep{T}.Call"/> each time it requires some other value to be computed, and
        ///             then expects that resulting value to come in through the parameter next time it is called;</description></item>
        ///         <item><description>
        ///             returns a <see cref="WorkStep{T}.Return"/> when it is done. At this point the delegate is not called
        ///             again.</description></item>
        ///         <item><description>
        ///             The delegate may return <c>null</c> to indicate the same as a <see cref="WorkStep{T}.Return"/>
        ///             containing a <c>default(T)</c> value.</description></item></list>
        ///     <para>
        ///         CustomCallStack works as follows:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             The first time the specified delegate is called, its parameter receives <c>default(T)</c>.</description></item>
        ///         <item><description>
        ///             If the value returned by the delegate is a <see cref="WorkStep{T}.Return"/>, the work is done and the
        ///             result is returned.</description></item>
        ///         <item><description>
        ///             If the value returned is a <see cref="WorkStep{T}.Call"/>, this is treated similarly to a method call.
        ///             The old delegate is pushed on a stack and the new delegate is executed according to the same rules
        ///             until it returns a <see cref="WorkStep{T}.Return"/>. Once it does so, the original delegate is popped
        ///             from the stack and then called with the result passed into its parameter.</description></item></list>
        ///     <para>
        ///         There are deliberately no safeguards in this algorithm; it will allow you to grow the stack indefinitely
        ///         and not generate an equivalent to the <see cref="StackOverflowException"/>. This means that if your
        ///         delegates always return a <see cref="WorkStep{T}.Call"/>, they will consume memory rampantly and never
        ///         finish.</para></remarks>
        public static T Run<T>(WorkNode<T> node)
        {
            var evaluationStack = new Stack<WorkNode<T>>();
            var prevResult = default(T);

            while (true)
            {
                var nextStep = node(prevResult);
                if (nextStep is WorkStep<T>.Call call)
                {
                    evaluationStack.Push(node);
                    node = call.Callee;
                    prevResult = default;
                }
                else
                {
                    prevResult = nextStep == null ? default : ((WorkStep<T>.Return) nextStep).Result;
                    if (evaluationStack.Count == 0)
                        return prevResult;
                    node = evaluationStack.Pop();
                }
            }
        }
    }

    /// <summary>
    ///     Describes the result of a call to a <see cref="WorkNode{T}"/>.</summary>
    /// <typeparam name="T">
    ///     Type of result to compute.</typeparam>
    /// <remarks>
    ///     This type is closed: Every value is either <c>null</c> or an instance of <see cref="WorkStep{T}.Return"/> or <see
    ///     cref="WorkStep{T}.Call"/>. The value <c>null</c> is treated as being equivalent to a <see
    ///     cref="WorkStep{T}.Return"/> containing a <c>default(T)</c>.</remarks>
#if EXPORT_UTIL
    public
#endif
    abstract class WorkStep<T>
    {
        /// <summary>Implicitly converts from a result value to a <see cref="Return"/>.</summary>
        public static implicit operator WorkStep<T>(T result)
        {
            // Note that ‘result’ could be null and that would be fine, as null is certainly a valid return value.
            // In such a case, we save an allocation by returning null instead of a Return instance that wraps a null value.
            return result == null ? null : new Return(result);
        }

        /// <summary>Implicitly converts from another work delegate to a <see cref="Call"/>.</summary>
        public static implicit operator WorkStep<T>(WorkNode<T> work)
        {
            // Implicit operator that throws: normally not considered to be a good idea, but in this case, we
            // cannot really convert a null to a null here, as the result of this implicit conversion is expected
            // to be a Call, but we allow WorkNode<T> delegates to return null to indicate a Return.
            if (work == null)
                throw new ArgumentNullException(nameof(work), "The implicit conversion from WorkNode<T> to WorkStep<T> cannot accept null.");
            return new Call(work);
        }

        private WorkStep() { }

        /// <summary>Indicates that the current delegate has finished its work and returned a result.</summary>
        public sealed class Return : WorkStep<T>
        {
            /// <summary>The result returned by the computation.</summary>
            public T Result { get; private set; }
            /// <summary>
            ///     Constructs a <see cref="Return"/> value.</summary>
            /// <param name="result">
            ///     Specifies the result returned by the computation.</param>
            /// <remarks>
            ///     For convenience, the type <see cref="WorkStep{T}"/> declares an implicit operator equivalent to this
            ///     constructor.</remarks>
            public Return(T result) { Result = result; }
        }

        /// <summary>Indicates that the current delegate requires another value to be computed before it can continue.</summary>
        public sealed class Call : WorkStep<T>
        {
            /// <summary>Specifies the additional work required.</summary>
            public WorkNode<T> Callee { get; private set; }
            /// <summary>
            ///     Constructs a <see cref="Call"/> value.</summary>
            /// <param name="work">
            ///     Specifies the additional work required.</param>
            /// <remarks>
            ///     For convenience, the type <see cref="WorkStep{T}"/> declares an implicit operator equivalent to this
            ///     constructor.</remarks>
            public Call(WorkNode<T> work) { Callee = work; }
        }
    }

    /// <summary>
    ///     Provides a delegate to specify work to be executed on an unlimited call stack. See <see
    ///     cref="CustomCallStack.Run{T}"/> for details.</summary>
    /// <typeparam name="T">
    ///     Type of result to compute.</typeparam>
    /// <param name="previousSubresult">
    ///     The result of the work step last returned by this same delegate.</param>
    /// <returns>
    ///     The next step of computation (which is either a <see cref="WorkStep{T}.Return"/> or a <see
    ///     cref="WorkStep{T}.Call"/>).</returns>
#if EXPORT_UTIL
    public
#endif
    delegate WorkStep<T> WorkNode<T>(T previousSubresult);
}
