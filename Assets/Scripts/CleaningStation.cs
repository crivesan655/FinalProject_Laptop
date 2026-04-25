using UnityEngine;

public class CleaningStation : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        PartInfo part = other.GetComponent<PartInfo>();

        if (part != null && part.requiresCleaning)
        {
            part.currentState = PartState.Clean;
            part.requiresCleaning = false;

            Debug.Log(part.partName + " cleaned!");
        }
    }
}