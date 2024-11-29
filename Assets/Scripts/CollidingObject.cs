using UnityEngine;

public class CollidingObject : MonoBehaviour {

    [SerializeField]
    public int price;

    private GameManager gameManager;

    void Start()
    {
        GameObject gameManagerObject = this.gameObject;
        do
        {
            gameManagerObject = gameManagerObject.transform.parent.gameObject;
        }
        while (!gameManagerObject.name.Contains("Env"));
        gameManagerObject = gameManagerObject.transform.Find("GameManager").gameObject;
        gameManager = gameManagerObject.GetComponent<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if(collision.gameObject.CompareTag("Vehicle")) {
            //연료 획득 시
            if(gameObject.name.Contains("Fuel")) {  
                gameManager.FuelCharge();
                Destroy(gameObject);
            }
            
            //목표 도착지에 도달하여 게임 성공
            else if(gameObject.name.Contains("Goal")) {  
                gameManager.ReachGoal = true;
                gameManager.StartGameOver();
            }

            //코인 획득
            else if(gameObject.name.Contains("Coin")) {  
                gameManager.GetCoin(price);
                Destroy(gameObject);
            }
        }
    }
}