using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    public Transform trfm;
    int trackerID;
    private void Start()
    {
        trfm = transform;
        cachedPositions = new Vector3[20];
    }

    private void FixedUpdate()
    {
        HandlePositionPredicting();
    }

    public Vector3 PredictedPosition(float time)
    {
        int cacheIndices = Mathf.RoundToInt(time / 0.1f);

        if (cacheIndices < 1) { return trfm.position; }
        else if (cacheIndices < 21)
        {
            cacheIndices = newCacheIndex - cacheIndices;
            if (cacheIndices < 0) { cacheIndices += cachedPositions.Length; }
            return trfm.position + trfm.position - cachedPositions[cacheIndices];
        }
        else
        {
            return trfm.position + trfm.position - (cachedPositions[newCacheIndex] * time / 2);
        }
    }

    int newCacheIndex;
    int positionCacheTimer;
    [SerializeField] Vector3[] cachedPositions;
    public void HandlePositionPredicting()
    {
        if (positionCacheTimer > 0) { positionCacheTimer--; return; }
        positionCacheTimer = 5;

        cachedPositions[newCacheIndex] = trfm.position;
        newCacheIndex++;
        newCacheIndex = newCacheIndex % cachedPositions.Length;
    }
}