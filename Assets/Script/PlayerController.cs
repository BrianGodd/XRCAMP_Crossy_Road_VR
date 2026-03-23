using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    private bool moving = false;

    private Vector3 targetPos;

    void Update()
    {
        if(moving) return;

        if(Input.GetKeyDown(KeyCode.W))
            Move(Vector3.forward);

        if(Input.GetKeyDown(KeyCode.S))
            Move(Vector3.back);

        if(Input.GetKeyDown(KeyCode.A))
            Move(Vector3.left);

        if(Input.GetKeyDown(KeyCode.D))
            Move(Vector3.right);
    }

    void Move(Vector3 dir)
    {
        targetPos = transform.position + dir;

        StartCoroutine(MoveRoutine());
    }

    System.Collections.IEnumerator MoveRoutine()
    {
        moving = true;

        Vector3 start = transform.position;
        float t = 0;

        while(t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, targetPos, t);

            yield return null;
        }

        moving = false;
    }
}