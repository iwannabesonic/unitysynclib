using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using System.Runtime.CompilerServices;
using System;
using Unity.Collections;

namespace Core.Async.Advanced
{
    public unsafe delegate void BurstAction(void* job);

    public readonly partial struct BurstTask<TData> where TData : unmanaged
    {
        private readonly BurstTaskJob job;
        private readonly JobHandle handle;

        public BurstTask(FunctionPointer<BurstAction> ptr_action, ref TData container)
        {
            job = new BurstTaskJob(ptr_action, ref container);
            handle = job.Schedule();
        }

        [BurstCompile(DisableSafetyChecks = true)]
        private struct BurstTaskJob : IJob
        {
            public TData result;
            public readonly FunctionPointer<BurstAction> ptr_action;

            public unsafe void Execute()
            {
                fixed(TData* ptr_data = &result)
                    ptr_action.Invoke(ptr_data);
            }
            
            public BurstTaskJob(FunctionPointer<BurstAction> action, ref TData container)
            {
                result = container;
                ptr_action = action;
            }
        }

        public Awaiter GetAwaiter() => new Awaiter(this);

        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly BurstTask<TData> task;

            void INotifyCompletion.OnCompleted(Action continuation)
            {
                continuation();
            }

            void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            {
                continuation();
            }

            public Awaiter(BurstTask<TData> task)
            {
                this.task = task;
            }

            public bool IsCompleted => task.handle.IsCompleted;
            public TData GetResult()
            {
                while (!IsCompleted)
                    continue;
                return task.job.result;
            }
        }


        [BurstCompile(CompileSynchronously = true)]
        public static unsafe BurstTask<TData> Run(in BurstAction action, ref TData container)
            => new BurstTask<TData>(BurstCompiler.CompileFunctionPointer(action), ref container);
    }
}
