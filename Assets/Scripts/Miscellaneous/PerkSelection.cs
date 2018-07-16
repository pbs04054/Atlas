using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerkSelection : MonoBehaviour {

    SkillManager sm;
    private Perk perk;
    [SerializeField]
    Text nameText, descriptionText;

    public void Initialize(Perk perk)
    {
        sm = GameManager.inst.playerController.sm;
        this.perk = perk;
        PerkInfo info = GameManager.inst.playerController.sm.perkInfoList[(int)perk];
        nameText.text = info.name;
        descriptionText.text = info.description;
    }

    public void OnClicked()
    {
        sm.perks.Remove(perk);
        sm.perks.Add(perk, true);
        if (GameManager.inst.playerController.PerkAvailable.Count != 0)
        {
            GameManager.inst.inGameUIManager.OpenPerkSelection(GameManager.inst.playerController.PerkAvailable.Dequeue());
        }
        else
        {
            GameManager.inst.inGameUIManager.ClosePerkSelection();
        }
        GameManager.inst.inGameUIManager.UpdatePerk();
    }
}
