using System;
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
    [SerializeField] private float ray_check_height = 50f;
    private LayerMask ground_layer;
    Transform cam;
    float smooth_time = 0.1f;
    float turn_smooth_velocity;
    void Start() {
        cam = Camera.main.transform;
        ground_layer = LayerMask.NameToLayer("Ground") ;
        controller = GetComponent<CharacterController>();
        StartCoroutine(Delay(1f, PlacePlayerOnTerrainSurface));
        //PlacePlayerOnTerrainSurface();
    }

    private IEnumerator Delay(float time, Action action) {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }

    private void PlacePlayerOnTerrainSurface() {
        Vector3 characterPosition = transform.position;

        // Set the starting point of the raycast (above the current character's position)
        Vector3 rayStartPos = new Vector3(characterPosition.x, ray_check_height, characterPosition.z);

        // Create a ray pointing downward
        Ray ray = new Ray(rayStartPos, Vector3.down);
        RaycastHit hit;

        Debug.DrawLine(rayStartPos, rayStartPos+Vector3.down*ray_check_height, Color.red, 10f);


        Debug.Log($"ray start pos:{rayStartPos}; end pos:{rayStartPos+Vector3.down*ray_check_height};");
        // Cast the ray downward to check for a collision with the terrain or other colliders
        if (Physics.Raycast(ray, out hit, ray_check_height*2)) {
            // Move the character to the hit point
            transform.position = new Vector3(characterPosition.x, hit.point.y, characterPosition.z);
            Debug.Log("Character moved to: " + transform.position);
        } else {
            Debug.LogWarning("No collision detected. The ray missed any colliders.");
        }
    }


    void Update() {


        Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cam.eulerAngles.y, ref turn_smooth_velocity, smooth_time);
        transform.rotation = Quaternion.Euler(0, angle, 0);
        
        if(direction.magnitude > 0.1f) {
            float target_angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y; 
            Vector3 move_direction = (Quaternion.Euler(0f, target_angle, 0f) * Vector3.forward).normalized;
            _ = controller.Move(ground_moving_speed * Time.deltaTime * move_direction);
        }else {
            
        }

        /*float vertical = Input.GetAxisRaw("Vertical") * ground_moving_speed;
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
        controller.Move(move_direction);*/
    }
}
