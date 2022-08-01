using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Async.Tasks;
using Core.Async;

public class TestScript : MonoBehaviour
{
    private void Start()
    {
        var task = UnityTaskDispatcher.Async<string>(() => "task completed");
        AsyncCalculationThread.Self.Execute(async () => 
        {
            Debug.Log(await task);
        });
    }
}
