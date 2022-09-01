using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Async.Tasks;
using Core.Async;
using Core.Async.Advanced;
using System.Runtime.InteropServices;

public class TestScript : MonoBehaviour
{
    private async void Start()
    {
        int x = 10, y = 10;
        BurstSumData burstSumData = new BurstSumData() { x = x, y = y };
        burstSumData = await burstSumData.Compile();
        System.Threading.Tasks.Task.Delay(1000).Wait();
        Debug.Log(burstSumData.result);
    }

    [Unity.Burst.BurstCompile]
    private struct BurstSumData
    {
        public int x,y,result;
        [Unity.Burst.BurstCompile]
        public static unsafe void Sum(void* ptr)
        {
            BurstSumData* data = (BurstSumData*)ptr;
            data->result = data->x + data->y;
        }

        public unsafe BurstTask<BurstSumData> Compile() => BurstTask<BurstSumData>.Run(BurstSumData.Sum, ref this);
    }
}
