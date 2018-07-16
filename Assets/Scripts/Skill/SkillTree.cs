using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree : MonoBehaviour {

    //If game starts, Skill Manager will check player's class and instantiate a skilltree.
    //When skill has instantiated, start will called and instantiate all class' skill lists.
    //Skill needs to have curLevel(maybe starts as 0) maxLevel, preSkills(skills that needs to learn this skill).

    public List<Skill> skillList = new List<Skill>();

	// Use this for initialization
	public virtual void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SkillTreeUpdate()
    {
        // TODO : Update all skills state which are in skillList
    }
}
