using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartTooltipManager : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject tooltipPrefab;

    private GameObject currentTooltip;
    private PartInfo hoveredPart;
    private Text tooltipText;

    void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // This checks if we are pointing at a part
        if (Physics.Raycast(ray, out hit, 5f))
        {
            PartInfo part = hit.collider.GetComponent<PartInfo>();
            if (part != null)
            {
                if (hoveredPart != part)
                {
                    ShowTooltip(part);
                }
                UpdateTooltipPosition(part);
            }
            else
            {
                HideTooltip();
            }

        }
        else
        {
            HideTooltip();
        }
    }

    void ShowTooltip(PartInfo part)
    {
        HideTooltip();

        hoveredPart = part;
        currentTooltip = Instantiate(tooltipPrefab);

        tooltipText = currentTooltip.GetComponent<Text>();
        if (tooltipText != null)
        {
            tooltipText.text = $"{part.partName}\n{part.description}";
        }
        
    }

    void UpdateTooltipPosition(PartInfo part)
    {
        if (currentTooltip != null)
        {
            Vector3 offset = part.transform.right* 0.5f + Vector3.up * 0.5f;
            currentTooltip.transform.position = part.transform.position + offset;
            currentTooltip.transform.LookAt(playerCamera.transform);
            currentTooltip.transform.Rotate(0, 180f, 0);
        }
    }
    void HideTooltip()
    {
        if (currentTooltip != null)
        {
            Destroy(currentTooltip);
            currentTooltip = null;
            hoveredPart = null;
        }
    }
}