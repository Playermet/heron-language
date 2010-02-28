﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Parallel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace HeronEngine
{
    /// <summary>
    /// Represents ta reduce expression.
    /// This transforms ta list of N items into ta list of 1 items (unless N == 0) by 
    /// applying an associative binary function to items in the list. 
    /// </summary>
    public class ReduceExpr : Expression
    {
        [HeronVisible] public string a;
        [HeronVisible] public string b;
        [HeronVisible] public Expression list;
        [HeronVisible] public Expression yield;

        public ReduceExpr(string a, string b, Expression list, Expression expr)
        {
            this.a = a;
            this.b = b;
            this.list = list;
            this.yield = expr;
        }

        public override HeronValue Eval(VM vm)
        {
            IInternalIndexable ii = vm.EvalInternalList(list);
            HeronValue[] output = new HeronValue[ii.InternalCount()];
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Config.maxThreads;
            var p = Partitioner.Create(1, ii.InternalCount());

            if (ii.InternalCount() == 0)
                return new ListValue();

            HeronValue result = ii.InternalAt(0);
            object resultLock = new Object();

            Parallel.ForEach(
                p,
                po,
                () =>
                {
                    LoopParams lp = new LoopParams();
                    lp.op = new OptimizationParams();
                    lp.acc = lp.op.AddNewAccessor(a);
                    lp.acc2 = lp.op.AddNewAccessor(b);
                    lp.vm = vm.Fork();
                    lp.expr = yield.Optimize(lp.op);
                    return lp;
                },
                (Tuple<int, int> range, ParallelLoopState state, LoopParams lp) =>
                {
                    if (range.Item1 == range.Item2)
                        return lp;

                    lp.acc.Set(ii.InternalAt(range.Item1));
                    
                    for (int i = range.Item1 + 1; i < range.Item2; ++i)
                    {
                        lp.acc2.Set(ii.InternalAt(i));
                        lp.acc.Set(lp.vm.Eval(lp.expr));
                    }

                    // Update the result 
                    lock (resultLock)
                    {
                        lp.acc2.Set(result);
                        result = lp.vm.Eval(lp.expr);
                    }

                    return lp;
                },
                (LoopParams lp) => { }
                );

            return new ArrayValue(new HeronValue[] { result });
        }

        public override Expression Optimize(OptimizationParams op)
        {
            return this;
        }

        public override string ToString()
        {
            return "reduce (" + a + ", " + b + " in " + list.ToString() + ") " + yield.ToString();
        }

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.ReduceExpr;
        }
    }
}