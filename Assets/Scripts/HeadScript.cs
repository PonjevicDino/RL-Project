using UnityEngine;

public class HeadScript : MonoBehaviour {

    [HideInInspector] public bool headHit = false;

    private GameManager gameManager;

    void Start()
    {
        gameManager = this.transform.parent.parent.parent.Find("GameManager").GetComponent<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        //머리가 땅에 닿아서 게임오버 
        if(collision.gameObject.CompareTag("Platform") && !gameManager.isDie) {
            headHit = true;
            gameManager.PlaySound("crack");
            gameManager.StartGameOver();
        }
    }
}