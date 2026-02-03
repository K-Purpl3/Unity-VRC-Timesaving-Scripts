using UnityEngine;

public class ProceduralRoomTest : MonoBehaviour
{
    [Header("Noise Settings")]
    public int seed = 12345;
    public int noiseResolution = 64;
    public float noiseScale = 10f;

    [Header("Room Settings")]
    public float minRoomSize = 6f;
    public float maxRoomSize = 12f;
    public float minWallHeight = 2.5f;
    public float maxWallHeight = 5f;

    void Start()
    {
        Random.InitState(seed);

        float[,] noise = GenerateNoise();
        GenerateRoom(noise);
    }

    float[,] GenerateNoise()
    {
        float[,] noiseMap = new float[noiseResolution, noiseResolution];

        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int x = 0; x < noiseResolution; x++)
        {
            for (int y = 0; y < noiseResolution; y++)
            {
                float nx = (x / (float)noiseResolution) * noiseScale + offsetX;
                float ny = (y / (float)noiseResolution) * noiseScale + offsetY;
                noiseMap[x, y] = Mathf.PerlinNoise(nx, ny);
            }
        }

        return noiseMap;
    }

    void GenerateRoom(float[,] noise)
    {
        float avgNoise = GetAverageNoise(noise);

        float roomSize = Mathf.Lerp(minRoomSize, maxRoomSize, avgNoise);
        float wallHeight = Mathf.Lerp(minWallHeight, maxWallHeight, avgNoise);

        Color baseColor = Color.Lerp(
            new Color(0.4f, 0.4f, 0.4f),
            new Color(0.7f, 0.7f, 0.7f),
            avgNoise
        );

        // FLOOR
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.parent = transform;
        floor.transform.localScale = Vector3.one * (roomSize / 10f);
        floor.GetComponent<Renderer>().material.color = baseColor;

        // WALLS
        CreateWall(new Vector3(0, wallHeight / 2f, roomSize / 2f), Quaternion.identity, roomSize, wallHeight, baseColor);
        CreateWall(new Vector3(0, wallHeight / 2f, -roomSize / 2f), Quaternion.identity, roomSize, wallHeight, baseColor);
        CreateWall(new Vector3(roomSize / 2f, wallHeight / 2f, 0), Quaternion.Euler(0, 90, 0), roomSize, wallHeight, baseColor);
        CreateWall(new Vector3(-roomSize / 2f, wallHeight / 2f, 0), Quaternion.Euler(0, 90, 0), roomSize, wallHeight, baseColor);

        // LIGHT
        GameObject lightObj = new GameObject("RoomLight");
        lightObj.transform.parent = transform;
        lightObj.transform.position = new Vector3(0, wallHeight - 0.5f, 0);

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = Mathf.Lerp(0.6f, 1.2f, avgNoise);
        light.range = roomSize * 1.5f;
        light.color = new Color(1f, 0.95f, 0.85f);
    }

    void CreateWall(Vector3 position, Quaternion rotation, float roomSize, float height, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.transform.rotation = rotation;

        wall.transform.localScale = new Vector3(roomSize / 10f, 1, height / 10f);
        wall.GetComponent<Renderer>().material.color = color;
    }

    float GetAverageNoise(float[,] noise)
    {
        float sum = 0f;

        foreach (float n in noise)
            sum += n;

        return sum / (noiseResolution * noiseResolution);
    }
}
