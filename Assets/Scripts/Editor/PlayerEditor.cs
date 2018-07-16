using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{

    Player player;
    SerializedProperty baseHealth;
    SerializedProperty baseStamina;
    SerializedProperty baseDefense;
    SerializedProperty baseSpeed;
    SerializedProperty baseAvoidRate;
    SerializedProperty baseStaminaPerSecond;
    SerializedProperty playerClass;
    SerializedObject obj;

    void OnEnable()
    {
        player = (Player)target;
        obj = new SerializedObject(target);
        baseHealth = obj.FindProperty("baseHealth");
        baseStamina = obj.FindProperty("baseStamina");
        baseDefense = obj.FindProperty("baseDefense");
        baseSpeed = obj.FindProperty("baseSpeed");
        baseAvoidRate = obj.FindProperty("baseAvoidRate");
        baseStaminaPerSecond = obj.FindProperty("baseStaminaPerSecond");
        playerClass = obj.FindProperty("playerClass");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("=================================");
        Rect r1 = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r1, player.CurHealth / player.MaxHealth, "HP : " + player.CurHealth + " / " + player.MaxHealth);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        Rect r2 = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r2, player.CurStamina / player.MaxStamina, "Stamina : " + player.CurStamina + " / " + player.MaxStamina);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("Defense : " + player.Defense);
        EditorGUILayout.LabelField("Speed : " + player.Speed);
        EditorGUILayout.LabelField("AvoidRate : " + player.AvoidRate);
        EditorGUILayout.LabelField("StaminaPerSecond : " + player.StaminaPerSecond);
        EditorGUILayout.LabelField("=================================");

        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        EditorGUILayout.PropertyField(playerClass);
        EditorGUILayout.PropertyField(baseHealth, false);
        EditorGUILayout.PropertyField(baseStamina);
        EditorGUILayout.PropertyField(baseDefense);
        EditorGUILayout.PropertyField(baseSpeed);
        EditorGUILayout.PropertyField(baseAvoidRate);
        EditorGUILayout.PropertyField(baseStaminaPerSecond);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField("=================================");

        obj.ApplyModifiedProperties();
        obj.Update();
        if(GUI.changed)
            EditorUtility.SetDirty(target);
    }

}