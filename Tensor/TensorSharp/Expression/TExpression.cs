﻿// ***********************************************************************
// Assembly         : TensorSharp
// Author           : Community
// Created          : 12-09-2018
//
// Last Modified By : Deepak Battini
// Last Modified On : 11-25-2018
// ***********************************************************************
// <copyright file="TExpression.cs" company="TensorSharp">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TensorSharp.Expression
{
    /// <summary>
    /// Class TExpression.
    /// </summary>
    public abstract class TExpression
    {
        /// <summary>
        /// Gets a value indicating whether this instance is valid lvalue.
        /// </summary>
        /// <value><c>true</c> if this instance is valid lvalue; otherwise, <c>false</c>.</value>
        public bool IsValidLvalue { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TExpression"/> class.
        /// </summary>
        /// <param name="isValidLvalue">if set to <c>true</c> [is valid lvalue].</param>
        public TExpression(bool isValidLvalue = false)
        {
            this.IsValidLvalue = isValidLvalue;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public abstract Tensor Evaluate(Tensor writeTarget);
    }

    /// <summary>
    /// Class ViewExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class ViewExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly TExpression src;
        /// <summary>
        /// The evaluate
        /// </summary>
        private readonly Func<Tensor, Tensor> evaluate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewExpression"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="evaluate">The evaluate.</param>
        public ViewExpression(TExpression src, Func<Tensor, Tensor> evaluate)
            : base(src.IsValidLvalue)
        {
            this.src = src;
            this.evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        /// <exception cref="InvalidOperationException">Cannot Select directly into another tensor</exception>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            if (writeTarget != null) throw new InvalidOperationException("Cannot Select directly into another tensor");

            using (var s = src.Evaluate(null))
            {
                return evaluate(s);
            }
        }
    }

    /// <summary>
    /// Class FromArrayExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class FromArrayExpression : TExpression
    {
        /// <summary>
        /// The allocator
        /// </summary>
        private readonly IAllocator allocator;
        /// <summary>
        /// The array
        /// </summary>
        private readonly Array array;

        /// <summary>
        /// Initializes a new instance of the <see cref="FromArrayExpression"/> class.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        /// <param name="array">The array.</param>
        public FromArrayExpression(IAllocator allocator, Array array)
            : base(false)
        {
            this.allocator = allocator;
            this.array = array;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            if (writeTarget != null)
            {
                writeTarget.CopyFrom(array);
                return writeTarget;
            }
            else
            {
                return Tensor.FromArray(allocator, array);
            }
        }
    }

    /// <summary>
    /// Class AsTypeExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class AsTypeExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly TExpression src;
        /// <summary>
        /// The type
        /// </summary>
        private readonly DType type;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsTypeExpression"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="type">The type.</param>
        public AsTypeExpression(TExpression src, DType type)
        {
            this.src = src;
            this.type = type;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var srcVal = src.Evaluate(null))
            {
                if (writeTarget == null)
                {
                    writeTarget = new Tensor(srcVal.Allocator, type, srcVal.Sizes);
                }

                Ops.Copy(writeTarget, srcVal);
                return writeTarget;
            }
        }
    }

    /// <summary>
    /// Class ToDeviceExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class ToDeviceExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly TExpression src;
        /// <summary>
        /// The allocator
        /// </summary>
        private readonly IAllocator allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDeviceExpression"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="allocator">The allocator.</param>
        public ToDeviceExpression(TExpression src, IAllocator allocator)
        {
            this.src = src;
            this.allocator = allocator;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var srcVal = src.Evaluate(null))
            {
                if (writeTarget == null)
                {
                    writeTarget = new Tensor(allocator, srcVal.ElementType, srcVal.Sizes);
                }

                Ops.Copy(writeTarget, srcVal);
                return writeTarget;
            }
        }
    }

    /// <summary>
    /// Class ScatterFillExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class ScatterFillExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly TExpression src;
        /// <summary>
        /// The indices
        /// </summary>
        private readonly TExpression indices;
        /// <summary>
        /// The value
        /// </summary>
        private readonly SVar value;
        /// <summary>
        /// The dimension
        /// </summary>
        private readonly int dimension;


        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterFillExpression"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="value">The value.</param>
        /// <param name="dimension">The dimension.</param>
        /// <param name="indices">The indices.</param>
        public ScatterFillExpression(TExpression src, SVar value, int dimension, TExpression indices)
        {
            this.src = src;
            this.value = value;
            this.dimension = dimension;
            this.indices = indices;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var s = src.Evaluate(null))
            using (var i = indices.Evaluate(null))
            {
                if (!writeTarget.Equals(s))
                {
                    Ops.Copy(writeTarget, s);
                }
                Ops.ScatterFill(writeTarget, value.Evaluate(), dimension, i);
            }

            return writeTarget;
        }
    }

    /// <summary>
    /// Class FillExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class FillExpression : TExpression
    {
        /// <summary>
        /// The allocator
        /// </summary>
        private readonly IAllocator allocator;
        /// <summary>
        /// The element type
        /// </summary>
        private readonly DType elementType;
        /// <summary>
        /// The sizes
        /// </summary>
        private readonly long[] sizes;
        /// <summary>
        /// The fill action
        /// </summary>
        private readonly Action<Tensor> fillAction;


        /// <summary>
        /// Initializes a new instance of the <see cref="FillExpression"/> class.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="sizes">The sizes.</param>
        /// <param name="fillAction">The fill action.</param>
        public FillExpression(IAllocator allocator, DType elementType, long[] sizes, Action<Tensor> fillAction)
        {
            this.allocator = allocator;
            this.elementType = elementType;
            this.sizes = sizes;
            this.fillAction = fillAction;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            if (writeTarget == null)
                writeTarget = new Tensor(allocator, elementType, sizes);

            fillAction(writeTarget);

            return writeTarget;
        }
    }

    /// <summary>
    /// Class AddmmExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class AddmmExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        /// <summary>
        /// The m1
        /// </summary>
        /// <summary>
        /// The m2
        /// </summary>
        private readonly TExpression src, m1, m2;
        /// <summary>
        /// The alpha
        /// </summary>
        /// <summary>
        /// The beta
        /// </summary>
        private readonly float alpha, beta;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddmmExpression"/> class.
        /// </summary>
        /// <param name="beta">The beta.</param>
        /// <param name="src">The source.</param>
        /// <param name="alpha">The alpha.</param>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        public AddmmExpression(float beta, TExpression src, float alpha, TExpression m1, TExpression m2)
        {
            this.beta = beta;
            this.src = src;
            this.alpha = alpha;
            this.m1 = m1;
            this.m2 = m2;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var s = src.Evaluate(null))
            using (var m1Val = m1.Evaluate(null))
            using (var m2Val = m2.Evaluate(null))
            {
                return Ops.Addmm(writeTarget, beta, s, alpha, m1Val, m2Val);
            }
        }
    }


    /// <summary>
    /// Class TensorValueExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class TensorValueExpression : TExpression
    {
        /// <summary>
        /// The value
        /// </summary>
        private readonly Tensor value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TensorValueExpression"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public TensorValueExpression(Tensor value)
            : base(true)
        {
            this.value = value;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            if (writeTarget == null)
                return value.CopyRef();
            else
            {
                Ops.Copy(writeTarget, value);
                return writeTarget;
            }
        }
    }

    /// <summary>
    /// Class BinaryTensorTensorExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class BinaryTensorTensorExpression : TExpression
    {
        /// <summary>
        /// The left
        /// </summary>
        /// <summary>
        /// The right
        /// </summary>
        private readonly TExpression left, right;
        /// <summary>
        /// The evaluate
        /// </summary>
        private readonly Func<Tensor, Tensor, Tensor, Tensor> evaluate;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryTensorTensorExpression"/> class.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="evaluate">The evaluate.</param>
        public BinaryTensorTensorExpression(TExpression left, TExpression right, Func<Tensor, Tensor, Tensor, Tensor> evaluate)
        {
            this.left = left;
            this.right = right;
            this.evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var lhs = left.Evaluate(null))
            using (var rhs = right.Evaluate(null))
            {
                return evaluate(writeTarget, lhs, rhs);
            }
        }
    }

    /// <summary>
    /// Class UnaryTensorExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class UnaryTensorExpression : TExpression
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly TExpression src;
        /// <summary>
        /// The evaluate
        /// </summary>
        private readonly Func<Tensor, Tensor, Tensor> evaluate;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryTensorExpression"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="evaluate">The evaluate.</param>
        public UnaryTensorExpression(TExpression src, Func<Tensor, Tensor, Tensor> evaluate)
        {
            this.src = src;
            this.evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var s = src.Evaluate(null))
            {
                return evaluate(writeTarget, s);
            }
        }
    }

    /// <summary>
    /// Class BinaryScalarTensorExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class BinaryScalarTensorExpression : TExpression
    {
        /// <summary>
        /// The left
        /// </summary>
        public readonly SExpression left;
        /// <summary>
        /// The right
        /// </summary>
        public readonly TExpression right;
        /// <summary>
        /// The evaluate
        /// </summary>
        public readonly Func<Tensor, float, Tensor, Tensor> evaluate;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryScalarTensorExpression"/> class.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="evaluate">The evaluate.</param>
        public BinaryScalarTensorExpression(SExpression left, TExpression right, Func<Tensor, float, Tensor, Tensor> evaluate)
        {
            this.left = left;
            this.right = right;
            this.evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var rhs = right.Evaluate(null))
            {
                return evaluate(writeTarget, left.Evaluate(), rhs);
            }
        }
    }

    /// <summary>
    /// Class BinaryTensorScalarExpression.
    /// Implements the <see cref="TensorSharp.Expression.TExpression" />
    /// </summary>
    /// <seealso cref="TensorSharp.Expression.TExpression" />
    public class BinaryTensorScalarExpression : TExpression
    {
        /// <summary>
        /// The left
        /// </summary>
        public readonly TExpression left;
        /// <summary>
        /// The right
        /// </summary>
        public readonly SExpression right;
        /// <summary>
        /// The evaluate
        /// </summary>
        public readonly Func<Tensor, Tensor, float, Tensor> evaluate;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryTensorScalarExpression"/> class.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="evaluate">The evaluate.</param>
        public BinaryTensorScalarExpression(TExpression left, SExpression right, Func<Tensor, Tensor, float, Tensor> evaluate)
        {
            this.left = left;
            this.right = right;
            this.evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates the specified write target.
        /// </summary>
        /// <param name="writeTarget">The write target.</param>
        /// <returns>Tensor.</returns>
        public override Tensor Evaluate(Tensor writeTarget)
        {
            using (var lhs = left.Evaluate(null))
            {
                return evaluate(writeTarget, lhs, right.Evaluate());
            }
        }
    }
}
