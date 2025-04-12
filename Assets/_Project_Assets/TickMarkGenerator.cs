using System.Collections.Generic;
using UnityEngine;

public class TickMarkGenerator : MonoBehaviour
{
    public GameObject tickPrefab;
    public float spacing = 0.05f;
    public float maxLength = 1.5f;

    private List<GameObject> ticks = new List<GameObject>();
    private Vector3 origin;
    private Vector3 direction;

    public void Initialize(Vector3 start, Vector3 dragDirection)
    {
        origin = start;
        direction = dragDirection.normalized;
    }

    public void UpdateTicks(float distance)
    {
        Debug.Log("Tick");
        int tickCount = Mathf.FloorToInt(distance / spacing);
        ClearExtraTicks(tickCount);

        for (int i = 0; i < tickCount; i++)
        {
            if (i < ticks.Count) continue;

            Vector3 tickPos = origin + direction * spacing * (i + 1);
            GameObject tick = Instantiate(tickPrefab, tickPos, Quaternion.LookRotation(direction));
            tick.transform.SetParent(transform, true);
            ticks.Add(tick);
        }
    }

    private void ClearExtraTicks(int keepCount)
    {
        while (ticks.Count > keepCount)
        {
            Destroy(ticks[ticks.Count - 1]);
            ticks.RemoveAt(ticks.Count - 1);
        }
    }

    public void ClearAll()
    {
        foreach (var tick in ticks)
            Destroy(tick);
        ticks.Clear();
    }
}