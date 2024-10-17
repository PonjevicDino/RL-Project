using UnityEngine;

public class HeadScript : MonoBehaviour {

    [HideInInspector] public bool headHit = false;

    private void OnTriggerEnter2D(Collider2D collision) {
        //머리가 땅에 닿아서 게임오버 
        if(collision.gameObject.CompareTag("Platform") && !GameManager.Instance.isDie) {
            headHit = true;
            GameManager.Instance.PlaySound("crack");
            GameManager.Instance.StartGameOver();
        }
    }
}