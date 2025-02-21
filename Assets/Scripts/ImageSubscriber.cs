using UnityEngine;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;

public class ImageSubscriber : MonoBehaviour
{
    private Thread clientThread;
    private bool running = true;
    private SubscriberSocket subscriber;
    public GameObject quadObject; // Assign a Quad in Unity to display the image
    private Material quadMaterial;
    private Texture2D receivedTexture;

    void Start()
    {
        quadMaterial = quadObject.GetComponent<MeshRenderer>().material;
        clientThread = new Thread(ReceiveData);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    private void ReceiveData()
    {
        ForceDotNet.Force(); // Required for NetMQ to work in Unity

        using (subscriber = new SubscriberSocket())
        {
            subscriber.Connect("tcp://172.22.243.38:4401"); // Replace with your ROS 2 bridge address
            subscriber.Subscribe("/camera/image_raw"); // Replace with your ROS 2 topic

            Debug.Log("Connected to ROS 2 Bridge!");

            while (running)
            {
                try
                {
                    if (subscriber.TryReceiveFrameString(out string message))
                    {
                        Debug.Log("Received Image Message");
                        ProcessMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("NetMQ Error: " + ex.Message);
                }
            }
        }
        NetMQConfig.Cleanup(); // Ensures proper cleanup of NetMQ
    }

    private void ProcessMessage(string message)
    {
        try
        {
            JObject json = JObject.Parse(message);
            string base64Image = json["data"].ToString(); // Extract image data
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(imageBytes);
            texture.Apply();

            receivedTexture = texture;

            // Start coroutine to apply texture in the main thread
            StartCoroutine(UpdateTexture());
        }
        catch (Exception ex)
        {
            Debug.LogError("Error processing image message: " + ex.Message);
        }
    }

    IEnumerator UpdateTexture()
    {
        yield return new WaitForEndOfFrame(); // Ensures this runs on the main thread
        if (receivedTexture != null)
        {
            quadMaterial.mainTexture = receivedTexture;
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        if (subscriber != null) subscriber.Dispose();
        clientThread.Join();
    }
}
