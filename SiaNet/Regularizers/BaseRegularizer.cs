﻿using System;
using System.Collections.Generic;
using System.Text;
using TensorSharp;
using TensorSharp.Expression;

namespace SiaNet.Regularizers
{
    public abstract class BaseRegularizer
    {
        internal float L1 { get; set; }

        internal float L2 { get; set; }

        public BaseRegularizer(float l1 = 0.01f, float l2 = 0.01f)
        {
            L1 = l1;
            L2 = l2;
        }

        public abstract Tensor Call(TVar x);

        public abstract Tensor CalcGrad(TVar x, TVar grad);
    }
}
