using UnityEngine;

public class CarController : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if(transform.position.x < -25)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            // call die and transfer the hit vector to the player controller for ragdoll effect
            VRPlayerController playerController = other.gameObject.GetComponent<VRPlayerController>();
            if (playerController != null)
            {
                Vector3 hitDirection = (other.transform.position - transform.position).normalized;
                playerController.Die(hitDirection);
            }
            Debug.Log("Game Over");
        }
    }
}