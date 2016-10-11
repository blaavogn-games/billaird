using UnityEngine;
using System.Collections;

public class ScoreBoard : MonoBehaviour {
    private int _reds = 0, _blues = 0;
    
    public void AddBall(CueBall b)
    {
        b.transform.localScale = new Vector2(3,3);
        b.transform.position = new Vector2(-12 + _reds++, -8.3f);
    }
}
