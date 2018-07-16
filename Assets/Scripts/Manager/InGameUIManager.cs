using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InGameUIManager : NetworkBehaviour {

    [SerializeField]
    Text moneyText, healthText, staminaText, shopMoneyText, waveTimerText, enemyRemainText, bulletText, levelText, expText;

    [SerializeField]
    Slider healthSlider, staminaSlider, expSlider, rescueSlider;

    [SerializeField]
    GameObject perkUI, abilitySelectionUI, activeSkills, perkSelectionUI;

    [SerializeField]
    GameObject[] skillUI = new GameObject[4], skillLockUI = new GameObject[4];

    [SerializeField]
    RectTransform crossHair;

    [SerializeField]
    Image[] skillIconUI = new Image[4];
    public enum Vignette { Hit, Danger };

    Transform hitVignettes;
    Image fatalVignetteImage;
    Coroutine fatalVignetteUpdator;

    GameObject[] debuffIcons = new GameObject[7];
    
    void Awake()
    {
        hitVignettes = transform.Find("Vignettes").Find("HitVignette");
        fatalVignetteImage = transform.Find("Vignettes").Find("FatalVignette").GetComponent<Image>();
        for (int i = 0; i < transform.Find("DebuffGrid").childCount; i++)
        {
            debuffIcons[i] = transform.Find("DebuffGrid").GetChild(i).gameObject;
        }
        Cursor.visible = false;
    }

    [SerializeField]
    Text[] skillStockTexts = new Text[4];

    void Update()
    {
        if (GameManager.inst.playerController != null && GameManager.inst.playerController.sm.classSkill != null)
        {
            UpdateSkillUI();
        }
        if (GameManager.inst.playerController == null) return;

        UpdateCrossHair();
    }

    private void UpdateCrossHair()
    {
        Plane plane = new Plane(Vector3.up, GameManager.inst.playerController.transform.position);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float distance;
        plane.Raycast(ray, out distance);
        crossHair.position = Camera.main.WorldToScreenPoint(ray.GetPoint(distance) + new Vector3(0, GameManager.inst.playerController.gunFirePoint.position.y));
    }

    public void EnableCrossHair()
    {
        crossHair.gameObject.SetActive(true);
        Cursor.visible = false;
    }

    public void DisableCrossHair()
    {
        crossHair.gameObject.SetActive(false);
        Cursor.visible = true;
    }

    public void InitializeSkillIcons(PlayerClass playerClass)
    {
        switch (playerClass)
        {
            case PlayerClass.ASSAULT:
                skillIconUI[0].sprite = Resources.Load<Sprite>("Textures/AssaultSkill/0");
                skillIconUI[1].sprite = Resources.Load<Sprite>("Textures/AssaultSkill/1");
                skillIconUI[2].sprite = Resources.Load<Sprite>("Textures/AssaultSkill/2");
                skillIconUI[3].sprite = Resources.Load<Sprite>("Textures/AssaultSkill/3");
                break;
            case PlayerClass.SNIPER:
                skillIconUI[0].sprite = Resources.Load<Sprite>("Textures/SniperSkill/0");
                skillIconUI[1].sprite = Resources.Load<Sprite>("Textures/SniperSkill/1");
                skillIconUI[2].sprite = Resources.Load<Sprite>("Textures/SniperSkill/2");
                skillIconUI[3].sprite = Resources.Load<Sprite>("Textures/SniperSkill/3");
                break;
            case PlayerClass.GUARD:
                skillIconUI[0].sprite = Resources.Load<Sprite>("Textures/GuardSkill/0");
                skillIconUI[1].sprite = Resources.Load<Sprite>("Textures/GuardSkill/1");
                skillIconUI[2].sprite = Resources.Load<Sprite>("Textures/GuardSkill/2");
                skillIconUI[3].sprite = Resources.Load<Sprite>("Textures/GuardSkill/3");
                break;
            case PlayerClass.DOCTOR:
                skillIconUI[0].sprite = Resources.Load<Sprite>("Textures/DoctorSkill/0");
                skillIconUI[1].sprite = Resources.Load<Sprite>("Textures/DoctorSkill/1");
                skillIconUI[2].sprite = Resources.Load<Sprite>("Textures/DoctorSkill/2");
                skillIconUI[3].sprite = Resources.Load<Sprite>("Textures/DoctorSkill/3");
                break;
        }
    }

    public void UpdateHealth()
    {
        healthText.text = (int)GameManager.inst.playerController.player.CurHealth + " / " + (int)GameManager.inst.playerController.player.MaxHealth;
        healthSlider.value = GameManager.inst.playerController.player.CurHealth / GameManager.inst.playerController.player.MaxHealth;
    }

    public void UpdateStamina()
    {
        staminaText.text = (int)GameManager.inst.playerController.player.CurStamina + " / " + (int)GameManager.inst.playerController.player.MaxStamina;
        staminaSlider.value = GameManager.inst.playerController.player.CurStamina / GameManager.inst.playerController.player.MaxStamina;
    }


    public void UpdateExp()
    {
        expText.text = GameManager.inst.playerController.player.CurExp + " / " + GameManager.inst.playerController.player.MaxExp;
        expSlider.value = GameManager.inst.playerController.player.CurExp / (float)GameManager.inst.playerController.player.MaxExp;
    }

    public void UpdateMoney()
    {
        moneyText.text = shopMoneyText.text = GameManager.inst.playerController.player.Money.ToString();
    }

    public void UpdateWaveTime()
    {
        waveTimerText.text = GameManager.inst.enemyManager.WaveTimer.ToString("f1");
    }

    public void UpdateEnemyRemain()
    {
        enemyRemainText.text = GameManager.inst.enemyManager.EnemyCounter.ToString();
    }

    public void UpdateBullet()
    {
        if (GameManager.inst.playerController.gun == null)
        {
            bulletText.text = "No Weapon";
            return;
        }
        bulletText.text = GameManager.inst.playerController.gun.RemainedBullets.ToString() + " / " + GameManager.inst.playerController.gun.MaxBullets.ToString();
    }

    public void UpdateLevel()
    {
        levelText.text = GameManager.inst.playerController.player.Level.ToString();
        CheckSkillLock();
    }

    public void UpdatePerk()
    {
        perkUI.SetActive(GameManager.inst.playerController.PerkAvailable.Count != 0);
    }

    public void UpdateRescueTime()
    {
        rescueSlider.value = GameManager.inst.playerController.player.RescueTimer / GameManager.inst.playerController.player.RescueTime;
    }

    public void ToggleRescueBar(bool toggle)
    {
        rescueSlider.gameObject.SetActive(toggle);
    }

    public void OpenPerkSelection(Perk perk)
    {
        perkSelectionUI.SetActive(true);
        PerkSelection[] perkSelections = GetComponentsInChildren<PerkSelection>();
        perkSelections[0].Initialize(perk);
        perkSelections[1].Initialize(perk + 1);
        PlayerState.Transition(PlayerState.wait);
    }

    public void ClosePerkSelection()
    {
        perkSelectionUI.SetActive(false);
        PlayerState.Transition(PlayerState.idle);
    }

    public void UpdateSkillUI()
    {
        Skill skill;
        for (int i = 0; i < 4; i++)
        {
            skill = GameManager.inst.playerController.sm.classSkill.skills[i];
            if (skill == null)
                continue;
            if (GameManager.inst.inGameUIManager.gameObject.activeSelf)
            {
                if (skill.Timer > 0)
                {
                    skillUI[i].GetComponentInChildren<Text>().text = skill.Timer.ToString("f1");
                }
                else
                {
                    skillUI[i].GetComponentInChildren<Text>().text = "";
                }
                if (!skill.useable)
                {
                    foreach(var image in skillUI[i].GetComponentsInChildren<Image>())
                    {
                        image.color = new Color(0.25f, 0.25f, 0.25f, 1);
                    }
                }
                else
                {
                    foreach (var image in skillUI[i].GetComponentsInChildren<Image>())
                    {
                        image.color = new Color(1, 1, 1, 1);
                    }
                }
            }
        }        
    }

    public void CheckSkillLock()
    {
        try
        {
            for (int i = 0; i < 4; ++i)
            {
                skillLockUI[i].SetActive(GameManager.inst.playerController.sm.classSkill.skills[i].isLocked);
            }
        }
        catch { }
    }

    public void HitVignette()
    {
        Image[] hitvignettes = hitVignettes.GetComponentsInChildren<Image>();
        foreach (Image image in hitvignettes)
        {
            image.enabled = false;
        }
        StopCoroutine("HitVignetteUpdator");
        StartCoroutine("HitVignetteUpdator");
    }

    IEnumerator HitVignetteUpdator()
    {
        float timer = 0;
        Image hitVignetteImage = hitVignettes.GetChild(Random.Range(0, hitVignettes.childCount)).GetComponent<Image>();
        hitVignetteImage.enabled = true;
        hitVignetteImage.color = new Color(1, 66 / 255f, 66 / 255f, 0.5f);
        while (true)
        {
            if (timer >= 1)
                break;
            hitVignetteImage.color = new Color(1, 66 / 255f, 66 / 255f, Mathf.Clamp01(0.5f - timer / 1));
            timer += Time.deltaTime;
            yield return null;
        }
        hitVignetteImage.enabled = false;
    }

    public void FatalVignette(bool enable)
    {
        if (enable && fatalVignetteUpdator == null)
        {
            fatalVignetteImage.enabled = true;
            fatalVignetteUpdator = StartCoroutine("FatalVignetteUpdator");
        }
        else
        {
            fatalVignetteImage.enabled = false;
            StopCoroutine("FatalVignetteUpdator");
            fatalVignetteUpdator = null;
        }
    }

    IEnumerator FatalVignetteUpdator()
    {
        while (true)
        {
            fatalVignetteImage.CrossFadeAlpha(0.3f, 1f, false);
            yield return new WaitForSeconds(1f);
            fatalVignetteImage.CrossFadeAlpha(0.7f, 1f, false);
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateSkillStockText(int index, int stock)
    {
        skillStockTexts[index].text = stock.ToString();
    }

    public void UpdateDebuffGrid(int[] debuffs)
    {
        for (int i = 0; i < debuffs.Length; i++)
        {
            int stack = debuffs[i];
            debuffIcons[i].gameObject.SetActive(stack > 0);
        }
    }
    
}
