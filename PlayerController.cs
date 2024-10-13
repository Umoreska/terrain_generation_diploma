using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float ground_moving_speed = 5f;
    [SerializeField] private float air_moving_speed_mod = .8f;
    [SerializeField] private float jump_strength = 10f;
    private CharacterController controller;
    void Start() {
        controller = GetComponent<CharacterController>();
    }


    void Update() {

        float vertical = Input.GetAxisRaw("Vertical") * ground_moving_speed;
        float horizontal = Input.GetAxisRaw("Horizontal") * ground_moving_speed;

        float y;

        if(controller.isGrounded) {
            y = 0;
            if(Input.GetKeyDown(KeyCode.Space)) {
                y = jump_strength;
            }
        }else {
            vertical *= air_moving_speed_mod;
            horizontal *= air_moving_speed_mod;
            y =+ Physics.gravity.y*Time.deltaTime;
        }

        Vector3 move_direction = new Vector3(Time.deltaTime*horizontal, y, Time.deltaTime*vertical);        
        controller.Move(move_direction);
    }
}
