using UnityEngine;

public enum PartState
{
    Assembled,
    Disassembled,
    Dirty,
    Clean,
    Broken,
    Repaired
}

public class PartInfo : MonoBehaviour
{
    public string partName;
    [TextArea] public string description;

    public PartType partType;
    public PartState currentState = PartState.Disassembled;

    public bool requiresCleaning;
    public bool requiresRepair;

    public bool isInstalled;

    public string requiredTool;

    public PartInfo[] requiredRemovedParts;

    public bool enforceDependencies = true;

    // How close a dependency must be to block removal (world units)
    public float dependencyProximity = 0.5f;

    public bool CanBeRemoved()
    {
        if (!enforceDependencies)
            return true;

        if (requiredRemovedParts == null || requiredRemovedParts.Length == 0)
            return true;

        foreach (var part in requiredRemovedParts)
        {
            if (part != null && part.currentState != PartState.Disassembled)
                return false;
        }

        return true;
    }

    // Returns true if any dependency that must be removed is still present and within 'radius' world units
    public bool HasNearbyBlockingParts(float radius)
    {
        if (requiredRemovedParts == null || requiredRemovedParts.Length == 0)
            return false;

        foreach (var part in requiredRemovedParts)
        {
            if (part == null) continue;

            // Only consider dependencies that are not yet disassembled
            if (part.currentState != PartState.Disassembled)
            {
                if (part.transform != null)
                {
                    float d = Vector3.Distance(transform.position, part.transform.position);
                    if (d <= radius)
                        return true;
                }
                else
                {
                    // If no transform, treat as blocking (defensive)
                    return true;
                }
            }
        }

        return false;
    }
}