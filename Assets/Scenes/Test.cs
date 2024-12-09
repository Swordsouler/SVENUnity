using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(GetComponent<MeshFilter>().mesh.uv.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv2.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv3.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv4.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv5.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv6.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv7.Length);
        Debug.Log(GetComponent<MeshFilter>().mesh.uv8.Length);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
