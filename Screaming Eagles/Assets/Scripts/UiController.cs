using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UiController : MonoBehaviour
{
    public PlayerController playerController;

    private Label lblWeaponEquiped;
    private Label lblClipSize;

    private void Awake()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        lblWeaponEquiped = root.Q<Label>("lblWeaponEquiped");
        lblClipSize = root.Q<Label>("lblClipSize");
    }

    private void Update()
    {
        switch (playerController.CurrentSelectedWeapon) //Uses Hardcoded weapon names. Should update if add more weapons
        {
            case SelectedWeapon.Primary:
                lblWeaponEquiped.text = "Rocket Jumper";
                lblClipSize.text = $"{playerController.currentPrimaryClipContent}/4";
                break;
            case SelectedWeapon.Melee:
                lblWeaponEquiped.text = "Market Gardener";
                lblClipSize.text = "Melee";
                break;
            default:
                lblWeaponEquiped.text = "Invalid (report bug)";
                break;
        }

        
    }
}
