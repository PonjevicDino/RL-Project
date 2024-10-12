using UnityEngine;

public class CollidingObject : MonoBehaviour {

    [SerializeField]
    public int price;

    private void OnTriggerEnter2D(Collider2D collision) {
        if(collision.gameObject.CompareTag("Vehicle")) {
            //연료 획득 시
            if(gameObject.name.Contains("Fuel")) {  
                GameManager.Instance.FuelCharge();
                Destroy(gameObject);
            }
            
            //목표 도착지에 도달하여 게임 성공
            else if(gameObject.name.Contains("Goal")) {  
                GameManager.Instance.ReachGoal = true;
                GameManager.Instance.StartGameOver();
            }

            //코인 획득
            else if(gameObject.name.Contains("Coin")) {  
                GameManager.Instance.GetCoin(price);
                Destroy(gameObject);
            }
        }
    }
}