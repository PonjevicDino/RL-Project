﻿using UnityEngine;

public class CameraController : MonoBehaviour {
    public Transform vehiclePos;
    private Vector3 offset;

    //게임 시작 시에 차량 위치와 카메라 세팅
    public void SetUp() {
        vehiclePos.position = new Vector3(Camera.main.ViewportToWorldPoint(new Vector3(0, 0)).x + 3f, vehiclePos.position.y, 0f);
        CarController carController = vehiclePos.gameObject.GetComponent<CarController>();
        carController.StartPos = vehiclePos.position;
        offset = transform.position + Vector3.left * 3.0f;
    }

    private void Update() {
        //카메라가 차량을 따라다님
        transform.position = vehiclePos.position + offset;
    }
}