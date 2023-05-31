using UnityEngine;

public class LevelSnapshot : MonoBehaviour
{
    public int textureSize = 1024;
    public GameObject snapshotPlanePrefab;

    private Camera snapshotCamera;
    private RenderTexture renderTexture;

    void Start()
    {
        snapshotCamera = GetComponent<Camera>();
        renderTexture = new RenderTexture(textureSize, textureSize, 24);
        snapshotCamera.targetTexture = renderTexture;
    }

    public void TakeSnapshot()
    {
        Texture2D snapshot = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
        snapshotCamera.Render();
        RenderTexture.active = renderTexture;
        snapshot.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        snapshot.Apply();

        GameObject snapshotPlane = Instantiate(snapshotPlanePrefab, transform.position, Quaternion.Euler(90, 0, 0));
        snapshotPlane.GetComponent<Renderer>().material.mainTexture = snapshot;
    }
}
