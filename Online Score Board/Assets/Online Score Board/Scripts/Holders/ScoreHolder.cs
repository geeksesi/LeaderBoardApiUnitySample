using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreHolder : MonoBehaviour {

    public Text numText;
    public Text nameText;
    public Text scoreText;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetScoreData(int num, string name, int score)
    {
        numText.text = num.ToString();
        nameText.text = name;
        scoreText.text = score.ToString();
    }
}
