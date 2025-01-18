using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float offSetX = 0f;
    public float offsSetY = 0f;

    void Update()
    {
        transform.position = new Vector3(target.position.x + offSetX, target.position.y + offsSetY, -1f);       //always -1f for Z because its 2D
    }
}
