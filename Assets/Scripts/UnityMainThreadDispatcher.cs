using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Func<Task>> actionQueue = new();
    private static bool isProcessing = false;

    public static void Enqueue(Func<Task> action)
    {
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
        }
    }

    private async void Update()
    {
        if (isProcessing || actionQueue.Count == 0)
            return;

        Func<Task> nextAction;

        lock (actionQueue)
        {
            if (actionQueue.Count == 0)
                return;

            nextAction = actionQueue.Dequeue();
            isProcessing = true;
        }

        try
        {
            await nextAction(); // Wait for the action (like ExecuteRedeemAsync) to finish
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Dispatcher] Error: {ex}");
        }

        isProcessing = false;
    }
}
