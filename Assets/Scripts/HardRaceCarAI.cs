using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

public class HardRaceCarAI : Agent
{
    enum MovementAction { Forward = 0, Backward = 1, None = 2 };
    enum TurnAction { Right = 0, Left = 1, None = 2 };

    

    private const float acceleration = 5f;
    private const float deceleration = -3f;
    private const float rotation_unit = 50f;
    private const float max_velocity = 50f;
    private const float min_velocity = -20f;
    private const float distance_behind = 5f;
    
    private float velocity = 0f;
    private float reward = 0.0f;
    private int number_collisions = 0;
    private Texture2D screenshot_texture_format;

    public Camera camera;

    public Rigidbody car;

    Queue<GameObject> walls = new Queue<GameObject>();

    private void Coverer()
    {
        if (!IsPositionCovered(car.transform.position))
        {
            CoverArea();
        }
    }

    private bool IsPositionCovered(Vector3 position)
    {
        foreach (GameObject wall in walls)
        {
            BoxCollider boxCollider = wall.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                if (boxCollider.bounds.Contains(position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void CoverArea()
    {
        GameObject verticalPlane = new GameObject("CoveredArea");

        BoxCollider boxCollider = verticalPlane.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        boxCollider.size = new Vector3(40f, 40f, 5f);

        Vector3 carPosition = car.transform.position;
        Vector3 behindCarPosition = carPosition - car.transform.forward * distance_behind;
        verticalPlane.transform.position = behindCarPosition;

        verticalPlane.transform.rotation = Quaternion.LookRotation(car.transform.forward);

        verticalPlane.transform.Rotate(90, 0, 0);

        if (walls.Count == 15)
        {
            DeleteCoveredArea();
        }
        walls.Enqueue(verticalPlane);
    }

    private void DeleteCoveredArea()
    {
        Destroy(walls.First());
        walls.Dequeue();
    }

    public void Start()
    {
        CoverArea();
        car = GetComponent<Rigidbody>();
        screenshot_texture_format = new Texture2D(84, 84, TextureFormat.RGB24, false);

        InvokeRepeating("Coverer", 0f, 0.85f);
    }

    public override void OnEpisodeBegin()
    {
        for (int i = 0; i < walls.Count; i++)
        {
            DeleteCoveredArea();
        }
        car.position = new Vector3(-295.4f, 1.05f, 3557.9f);
        car.transform.rotation = Quaternion.Euler(0, 51.967f, 0);
        velocity = 0;
        reward = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        RenderTexture screen = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = screen;
        camera.Render();

        RenderTexture.active = screen;
        screenshot_texture_format.ReadPixels(new Rect(0, 0, 84, 84), 0, 0);
        screenshot_texture_format.Apply();

        Color[] pixels = screenshot_texture_format.GetPixels();
        foreach (Color pixel in pixels)
        {
            sensor.AddObservation(pixel.r);
            sensor.AddObservation(pixel.g);
            sensor.AddObservation(pixel.b);
        }
        sensor.AddObservation(velocity);

        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(screen);
    }

    private void Update()
    {
        if (car.position.y < -10) { Debug.Log("Fell of the Map"); AddReward(-100f); EndEpisode(); }

        Vector3 carRotation = transform.rotation.eulerAngles;

        float rotationX = carRotation.x > 180 ? carRotation.x - 360 : carRotation.x;
        float rotationZ = carRotation.z > 180 ? carRotation.z - 360 : carRotation.z;

        if (rotationX > 30 || rotationX < -30 || rotationZ > 30 || rotationZ < -30)
        {
            Debug.Log("Car rotated out of bounds! Ending episode.");
            SetReward(-100f);
            EndEpisode();
        }

        if (reward <= -50f) { EndEpisode(); }

        Debug.Log(reward);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0];
        int turn = actions.DiscreteActions[1];

        if (move == (int)MovementAction.Forward)
        {
            velocity += acceleration * Time.deltaTime;
        }
        else if (move == (int)MovementAction.Backward)
        {
            velocity += deceleration * Time.deltaTime;
        }
        else
        {
            velocity = Mathf.Lerp(velocity, 0, 0.01f);
        }

        velocity = Mathf.Clamp(velocity, min_velocity, max_velocity);

        Vector3 movement = transform.forward * velocity * Time.deltaTime;
        car.MovePosition(car.position + movement);

        if (turn == (int)TurnAction.Right)
        {
            car.MoveRotation(car.rotation * Quaternion.Euler(0, rotation_unit * Time.deltaTime, 0));
        }
        else if (turn == (int)TurnAction.Left)
        {
            car.MoveRotation(car.rotation * Quaternion.Euler(0, -rotation_unit * Time.deltaTime, 0));
        }
    }

    private void OnRoadReward(Collision collision)
    {
        if (number_collisions == 50) { number_collisions = 0; EndEpisode(); }
        
        if (collision.gameObject.name != "Road (1)")
        {
            Debug.Log("Not on Road");
            number_collisions++;
            reward -= 5f;
            AddReward(-5f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "CoveredArea")
        {
            reward -= 0.0005f;
            AddReward(-0.0005f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "CoveredArea")
        {
            reward = Mathf.Min(30, reward + 1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Finish")
        {
            Debug.Log("Car has crossed the finish line!"); 
            SetReward(100f);
            EndEpisode();
        }
        if (other.gameObject.name == "CoveredArea")
        {
            reward -= 0.5f;
            AddReward(-0.5f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnRoadReward(collision);

        Rigidbody otherCar = collision.rigidbody;

        float delta_velocity;

        if (otherCar != null)
        {
            float mA = car.mass;
            float mB = otherCar.mass;
            Vector3 vA = car.velocity;
            Vector3 vB = otherCar.velocity;

            Vector3 newVA = (vA * (mA - mB) + 2 * mB * vB) / (mA + mB);
            Vector3 newVB = (vB * (mB - mA) + 2 * mA * vA) / (mA + mB);

            delta_velocity = (newVA - vA).magnitude;

            car.velocity = newVA;
            otherCar.velocity = newVB;
        }
        else
        {
            delta_velocity = 0 - velocity;
            velocity = -2;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> actions = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W)) { actions[0] = (int)MovementAction.Forward; }
        else if (Input.GetKey(KeyCode.S)) { actions[0] = (int)MovementAction.Backward; }
        else { actions[0] = (int)MovementAction.None; }

        if (Input.GetKey(KeyCode.D)) { actions[1] = (int)TurnAction.Right; }
        else if (Input.GetKey(KeyCode.A)) { actions[1] = (int)TurnAction.Left; }
        else { actions[1] = (int)TurnAction.None; }
    }
}
