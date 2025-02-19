using UnityEngine;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using SensorMsgs = RosMessageTypes.Sensor;

public class ImageSubscriber : MonoBehaviour
{
    public string TopicName = "camera/image_raw";
    private ROSConnection rosConnection;
    private Material quadMaterial;
    private Texture2D texture;
    private RenderTexture renderTexture;
    private byte[] currentImageBytes = null;

    void Start()
    {
        // Initialize ROS connection
        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.Subscribe<SensorMsgs.ImageMsg>(TopicName, ReceiveImage);

        // Initialize RenderTexture and Texture2D for the Quad
        renderTexture = new RenderTexture(640, 480, 24);
        texture = new Texture2D(640, 480, TextureFormat.RGB24, false);
        quadMaterial = GetComponent<Renderer>().material;

        // Apply RenderTexture to Quad material
        quadMaterial.mainTexture = renderTexture;
    }

    private void ReceiveImage(SensorMsgs.ImageMsg rosImage)
    {
        Debug.Log("Received image with size: " + rosImage.data.Length);

        // Ensure the encoding is rgb8 as expected
        if (rosImage.encoding != "rgb8")
        {
            Debug.LogError("Received image is not in rgb8 format!");
            return;
        }

        byte[] imageBytes = rosImage.data;

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("Received image data is empty!");
            return;
        }

        // Store the image bytes for later processing
        currentImageBytes = imageBytes;

        // Start processing the image in the next frame
        StartCoroutine(UpdateTexture());
    }

    private IEnumerator UpdateTexture()
    {
        // Wait until the end of the current frame
        yield return null;

        if (currentImageBytes != null && currentImageBytes.Length > 0)
        {
            // Create the Texture2D using the image data
            texture.LoadRawTextureData(currentImageBytes);
            texture.Apply();  // Apply the changes to the texture

            // Update the RenderTexture (ensure this happens on the main thread)
            Graphics.Blit(texture, renderTexture);
        }
    }

    void OnApplicationQuit()
    {
        rosConnection.Unsubscribe(TopicName);
    }
}
