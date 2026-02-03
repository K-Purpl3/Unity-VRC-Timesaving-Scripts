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

    [Header("Object Spawning")]
    public int minObjects = 3;
    public int maxObjects = 8;
    public GameObject[] spawnablePrefabs;

    [Header("Internal Structures")]
    public int minInternalStructures = 1;
    public int maxInternalStructures = 4;
    
    [Space(5)]
    [Tooltip("Altura mínima de las estructuras internas")]
    public float minInternalHeight = 1.5f;
    
    [Tooltip("Altura máxima de las estructuras internas")]
    public float maxInternalHeight = 3.5f;
    
    [Space(5)]
    [Tooltip("Tamaño mínimo (ancho y profundidad)")]
    public float minInternalSize = 1.2f;
    
    [Tooltip("Tamaño máximo (ancho y profundidad)")]
    public float maxInternalSize = 4.0f;
    
    [Space(5)]
    [Tooltip("Usar colores aleatorios para las estructuras internas")]
    public bool useRandomColors = true;
    
    [Tooltip("Color base si no se usan colores aleatorios")]
    public Color internalStructureColor = new Color(0.7f, 0.4f, 0.9f);

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

        // ===== FLOOR =====
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = transform;
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(roomSize, 0.1f, roomSize);
        floor.GetComponent<Renderer>().material.color = baseColor;

        // ===== OUTER WALLS =====
        float wallThickness = 0.2f;

        CreateWall(new Vector3(0, wallHeight / 2f, roomSize / 2f),
                   new Vector3(roomSize, wallHeight, wallThickness),
                   baseColor);

        CreateWall(new Vector3(0, wallHeight / 2f, -roomSize / 2f),
                   new Vector3(roomSize, wallHeight, wallThickness),
                   baseColor);

        CreateWall(new Vector3(roomSize / 2f, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, roomSize),
                   baseColor);

        CreateWall(new Vector3(-roomSize / 2f, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, roomSize),
                   baseColor);

        // ===== INTERNAL STRUCTURES =====
        GenerateInternalStructures(roomSize, wallHeight);

        // ===== LIGHT =====
        GameObject lightObj = new GameObject("RoomLight");
        lightObj.transform.parent = transform;
        lightObj.transform.position = new Vector3(0, wallHeight - 0.5f, 0);

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = Mathf.Lerp(0.6f, 1.1f, avgNoise);
        light.range = roomSize * 1.5f;
        light.color = new Color(1f, 0.95f, 0.85f);

        // ===== SPAWN RANDOM OBJECTS =====
        SpawnRandomObjects(roomSize);
    }

    void CreateWall(Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material.color = color;
    }

void GenerateInternalStructures(float roomSize, float wallHeight)
{
    int count = Random.Range(minInternalStructures, maxInternalStructures + 1);

    for (int i = 0; i < count; i++)
    {
        GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
        structure.transform.parent = transform;

        float width  = Random.Range(minInternalSize, maxInternalSize);
        float depth  = Random.Range(minInternalSize, maxInternalSize);
        float height = Random.Range(minInternalHeight, maxInternalHeight);

        // seguridad: que no atraviesen paredes
        float x = Random.Range(
            -roomSize / 2f + width / 2f + 0.5f,
             roomSize / 2f - width / 2f - 0.5f
        );

        float z = Random.Range(
            -roomSize / 2f + depth / 2f + 0.5f,
             roomSize / 2f - depth / 2f - 0.5f
        );

        structure.transform.position = new Vector3(x, height / 2f, z);
        structure.transform.localScale = new Vector3(width, height, depth);

        Renderer r = structure.GetComponent<Renderer>();
        
        // ===== SOLUCIÓN: Usar el material del objeto primitivo ya existente =====
        // En lugar de crear un nuevo material, usar el que viene con el primitivo
        // y solo cambiar su color
        
        // ===== ASIGNAR COLOR ALEATORIO O FIJO =====
        if (useRandomColors)
        {
            // Generar color aleatorio vibrante
            r.material.color = GetRandomColor();
        }
        else
        {
            // Usar el color definido en el Inspector
            r.material.color = internalStructureColor;
        }
    }
}

    // Método para generar colores aleatorios vibrantes
    Color GetRandomColor()
    {
        // Opción 1: Colores completamente aleatorios
        // return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        
        // Opción 2: Colores más vibrantes y saturados (recomendado)
        return Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);
    }

    void SpawnRandomObjects(float roomSize)
    {
        int objectCount = Random.Range(minObjects, maxObjects + 1);

        for (int i = 0; i < objectCount; i++)
        {
            GameObject obj;

            if (spawnablePrefabs != null && spawnablePrefabs.Length > 0 && Random.value > 0.4f)
            {
                obj = Instantiate(
                    spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)]
                );
            }
            else
            {
                PrimitiveType type = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                obj = GameObject.CreatePrimitive(type);
            }

            obj.transform.parent = transform;

            float margin = 1f;
            float x = Random.Range(-roomSize / 2f + margin, roomSize / 2f - margin);
            float z = Random.Range(-roomSize / 2f + margin, roomSize / 2f - margin);

            obj.transform.position = new Vector3(x, 0.6f, z);
            obj.transform.rotation = Random.rotation;
            obj.transform.localScale *= Random.Range(0.4f, 1.2f);

            // ===== ADD PHYSICS =====
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = Random.Range(0.5f, 3f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // ===== ADD VR INTERACTION =====
            //obj.AddComponent<BasisPickupInteractable>();
        }
    }

    float GetAverageNoise(float[,] noise)
    {
        float sum = 0f;

        foreach (float n in noise)
            sum += n;

        return sum / (noiseResolution * noiseResolution);
    }
}
