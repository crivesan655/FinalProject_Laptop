using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public PartInfo[] allParts;

    public bool AllDisassembled()
    {
        foreach (var p in allParts)
            if (p.currentState == PartState.Assembled)
                return false;

        return true;
    }

    public bool AllClean()
    {
        foreach (var p in allParts)
            if (p.requiresCleaning)
                return false;

        return true;
    }

    public bool AllRepaired()
    {
        foreach (var p in allParts)
            if (p.requiresRepair)
                return false;

        return true;
    }

    public bool AllAssembled()
    {
        foreach (var p in allParts)
            if (!p.isInstalled)
                return false;

        return true;
    }
}