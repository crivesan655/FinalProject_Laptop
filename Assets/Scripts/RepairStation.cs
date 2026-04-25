using UnityEngine;

public class RepairStation : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        PartInfo part = other.GetComponent<PartInfo>();

        if (part != null && part.requiresRepair)
        {
            part.currentState = PartState.Repaired;
            part.requiresRepair = false;

            Debug.Log(part.partName + " repaired!");
        }
    }
}