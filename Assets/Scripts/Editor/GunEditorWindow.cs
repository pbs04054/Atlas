using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class GunEditorWindow : EditorWindow
{

    GunList gunList;
    GunType gunType;

    int id;
    string gunName;
    int price;
    int atk;
    float gsi;
    int mb;
    float mnbsa;
    float mxbsa;
    float rrps;
    float rc;
    float ri;
    float bs;
    int bps;
    Shot gf;
    ShotMode gfm;
    ReloadMode gr;

    int tabIndex;
    bool isEditMode;
    GunInfo selectedGun;

    GameObject[] bullets;
    string[] bulletNames;
    int selectBullet;

    [MenuItem("Atlas/Make Gun")]
    public static void ShowWindow()
    {        
        GetWindow(typeof(GunEditorWindow));
    }

    void OnEnable()
    {
        Refresh();
        isEditMode = false;
        selectedGun = null;
    }

    void OnGUI()
    {
        GUILayout.Label("아마 완성? 일탠데 문제가 있으면 바로 카톡날려주세요", EditorStyles.boldLabel);

        tabIndex = GUILayout.Toolbar(tabIndex, new string[] {"제작", "목록", "편집" });

        switch (tabIndex)
        {
            case 0:
                {
                    isEditMode = false;
                    selectedGun = null;
                    CreateTab();
                }
                break;
            case 1:
                {
                    isEditMode = false;
                    selectedGun = null;
                    ListTab();
                }
                break;
            case 2:
                {
                    EditTab();
                }
                break;
        }
    }

    void CreateTab()
    {
        gunType = (GunType)EditorGUILayout.EnumPopup("타입", gunType);
        EditorGUI.BeginDisabledGroup(true);
        id = EditorGUILayout.IntField("ID", gunList.GetList(gunType).Count + (int)gunType * 100);
        EditorGUI.EndDisabledGroup();
        gunName = EditorGUILayout.TextField("이름", gunName);
        price = EditorGUILayout.IntField("가격", price);
        atk = EditorGUILayout.IntField("공격력", atk);
        gsi = EditorGUILayout.FloatField("공격속도 (총알 발사 간격)", gsi);
        mb = EditorGUILayout.IntField("최대 총알 수", mb);
        mnbsa = EditorGUILayout.FloatField("최소 반동 각도", mnbsa);
        mxbsa = EditorGUILayout.FloatField("최대 반동 각도", mxbsa);
        rrps = EditorGUILayout.FloatField("초당 반동 회복", rrps);
        rc = EditorGUILayout.FloatField("반동", rc);
        ri = EditorGUILayout.FloatField("재장전 시간", ri);
        bs = EditorGUILayout.FloatField("총알 속도", bs);
        bps = EditorGUILayout.IntField("발사 당 총알 수 (샷건)", bps);
        gf = (Shot)EditorGUILayout.EnumPopup("gunFire", gf);
        gfm = (ShotMode)EditorGUILayout.EnumPopup("gunFireMode", gfm);
        gr = (ReloadMode)EditorGUILayout.EnumPopup("gunReload", gr);
        selectBullet = EditorGUILayout.Popup("총알(Resources/Prefabs/Bullet)", selectBullet, bulletNames);

        if (GUILayout.Button("만들기"))
        {
            GunInfo gunInfo = new GunInfo(id, gunName, gunType, price, atk, gsi, mb, mnbsa, mnbsa, mxbsa, rrps, rc, ri, bs, bps, gf, gfm, gr, bulletNames[selectBullet]);
            gunList.GetList(gunInfo.type).Add(gunInfo);
            File.WriteAllText(Application.dataPath + "/Resources/GunData.json", JsonUtility.ToJson(gunList, true));
            AssetDatabase.Refresh();
        }

    }

    void ListTab()
    {
        foreach(GunType type in System.Enum.GetValues(typeof(GunType)))
        {
            EditorGUILayout.LabelField(type.ToString(), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach(GunInfo gunInfo in gunList.GetList(type))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(gunInfo.name);
                if (GUILayout.Button("편집"))
                {
                    tabIndex = 2;
                    selectedGun = gunInfo;
                    selectBullet = ArrayUtility.FindIndex(bulletNames, (bulletName) => bulletName == selectedGun.bullet);
                    isEditMode = true;
                    GUIUtility.keyboardControl = 0;
                    return;
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    public void EditTab()
    {
        if (isEditMode == false)
        {
            GUILayout.Label("편집 모드가 아닙니다.", EditorStyles.boldLabel);
            return;
        }
        if(selectedGun == null)
        {
            GUILayout.Label("총이 선택되지 않았습니다.", EditorStyles.boldLabel);
            return;
        }
        selectedGun.type = (GunType)EditorGUILayout.EnumPopup("타입", selectedGun.type);
        EditorGUI.BeginDisabledGroup(true);
        selectedGun.id = EditorGUILayout.IntField("ID", selectedGun.id);
        EditorGUI.EndDisabledGroup();
        selectedGun.name = EditorGUILayout.TextField("이름", selectedGun.name);
        selectedGun.price = EditorGUILayout.IntField("가격", selectedGun.price);
        selectedGun.attack = EditorGUILayout.IntField("공격력", selectedGun.attack);
        selectedGun.gunShotInterval = EditorGUILayout.FloatField("공격속도 (총알 발사 간격)", selectedGun.gunShotInterval);
        selectedGun.maxBullets = EditorGUILayout.IntField("최대 총알 수", selectedGun.maxBullets);
        selectedGun.minBulletSpreadAngle = EditorGUILayout.FloatField("최소 반동 각도", selectedGun.minBulletSpreadAngle);
        selectedGun.maxBulletSpreadAngle = EditorGUILayout.FloatField("최대 반동 각도", selectedGun.maxBulletSpreadAngle);
        selectedGun.recoverRecoilPerSec = EditorGUILayout.FloatField("초당 반동 회복", selectedGun.recoverRecoilPerSec);
        selectedGun.recoil = EditorGUILayout.FloatField("반동", selectedGun.recoil);
        selectedGun.reloadInterval = EditorGUILayout.FloatField("재장전 시간", selectedGun.reloadInterval);
        selectedGun.bulletSpeed = EditorGUILayout.FloatField("총알 속도", selectedGun.bulletSpeed);
        selectedGun.bulletsPerShot = EditorGUILayout.IntField("발사 당 총알 수 (샷건)", selectedGun.bulletsPerShot);
        selectedGun.gunFire = (Shot)EditorGUILayout.EnumPopup("gunFire", selectedGun.gunFire);
        selectedGun.gunFireMode = (ShotMode)EditorGUILayout.EnumPopup("gunFireMode", selectedGun.gunFireMode);
        selectedGun.gunReload = (ReloadMode)EditorGUILayout.EnumPopup("gunReload", selectedGun.gunReload);
        selectBullet = EditorGUILayout.Popup("총알(Resources/Prefabs/Bullet)", selectBullet, bulletNames);
        selectedGun.bullet = bulletNames[selectBullet];
        
        if (GUILayout.Button("편집"))
        {
            File.WriteAllText(Application.dataPath + "/Resources/GunData.json", JsonUtility.ToJson(gunList, true));
            AssetDatabase.Refresh();
            isEditMode = false;
            selectedGun = null;
            Refresh();
        }

    }

    void Refresh()
    {
        string jsonData = Resources.Load<TextAsset>("GunData").text;
        if (System.String.IsNullOrEmpty(jsonData))
            gunList = new GunList();
        else
            gunList = JsonUtility.FromJson<GunList>(jsonData);
        
        bullets = Resources.LoadAll<GameObject>("Prefabs/Bullets/");
        bulletNames = new string[bullets.Length];
        for (var i = 0; i < bullets.Length; i++)
        {
            bulletNames[i] = bullets[i].name;
        }
    }

}