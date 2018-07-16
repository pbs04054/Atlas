using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Enemy), true)]
public class EnemyEditor : Editor
{

    Enemy enemy;
    SerializedProperty baseHealth;
    SerializedProperty baseDefense;
    SerializedProperty baseSpeed;
    SerializedProperty baseAvoidRate;
    SerializedProperty baseDamage;
    SerializedProperty baseAttackSpeed;
    SerializedObject obj;

    bool toggle;

    void OnEnable()
    {
        enemy = (Enemy)target;
        obj = new SerializedObject(target);
        baseHealth = obj.FindProperty("baseHealth");
        baseDamage = obj.FindProperty("baseDamage");
        baseDefense = obj.FindProperty("baseDefense");
        baseSpeed = obj.FindProperty("baseSpeed");
        baseAvoidRate = obj.FindProperty("baseAvoidRate");
        baseAttackSpeed = obj.FindProperty("baseAttackSpeed");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("=================================");
        Rect r1 = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r1, enemy.CurHealth / enemy.MaxHealth, "HP : " + enemy.CurHealth + " / " + enemy.MaxHealth);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();
        EditorGUILayout.LabelField("Damage : " + enemy.Damage);
        EditorGUILayout.LabelField("Attack Speed : " + enemy.AttackSpeed);
        EditorGUILayout.LabelField("Defense : " + enemy.Defense);
        EditorGUILayout.LabelField("Speed : " + enemy.Speed);
        EditorGUILayout.LabelField("AvoidRate : " + enemy.AvoidRate);
        EditorGUILayout.LabelField("=================================");

        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        EditorGUILayout.PropertyField(baseHealth, false);
        EditorGUILayout.PropertyField(baseDamage);
        EditorGUILayout.PropertyField(baseAttackSpeed);
        EditorGUILayout.PropertyField(baseDefense);
        EditorGUILayout.PropertyField(baseSpeed);
        EditorGUILayout.PropertyField(baseAvoidRate);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField("=================================");

        toggle = EditorGUILayout.Toggle("Base Inspector",toggle);
        if (toggle)
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("=================================");
        }
        obj.ApplyModifiedProperties();
        obj.Update();
        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

}
