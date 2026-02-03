using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

public class ProceduralRoomTest : MonoBehaviour
{
    [Header("Noise Settings")]
    public int seed = 12345;
    public int noiseResolution = 64;
    public float noiseScale = 10f;
    
    [Space(5)]
    [Tooltip("Guardar la imagen de ruido generada")]
    public bool saveNoiseImage = true;
    
    [Tooltip("Nombre base para las imágenes guardadas")]
    public string noiseImageBaseName = "noise_map";

    [Header("Room Settings")]
    public float minRoomSize = 6f;
    public float maxRoomSize = 12f;
    public float minWallHeight = 2.5f;
    public float maxWallHeight = 5f;

    [Header("Object Spawning")]
    public int minObjects = 3;
    public int maxObjects = 8;
    public GameObject[] spawnablePrefabs;
    
    [Space(5)]
    [Tooltip("Añadir física a los objetos generados")]
    public bool addPhysicsToObjects = true;
    
    [Tooltip("Añadir componente VR interactable (requiere Basis SDK)")]
    public bool addVRInteractable = false;

    [Header("Internal Structures - Noise Based")]
    [Tooltip("Generar estructuras basadas en el mapa de ruido")]
    public bool useNoiseBasedStructures = true;
    
    [Space(5)]
    [Tooltip("Umbral para generar estructuras (0-1). Valores del ruido por debajo de este umbral generan paredes")]
    [Range(0f, 1f)]
    public float structureThreshold = 0.4f;
    
    [Tooltip("Altura de las estructuras generadas por ruido")]
    public float noiseStructureHeight = 2.5f;
    
    [Tooltip("Tamaño de cada celda del ruido en el mundo")]
    public float cellSize = 0.5f;
    
    [Space(5)]
    [Tooltip("Usar colores aleatorios para las estructuras basadas en ruido")]
    public bool useRandomColorsForNoise = true;
    
    [Tooltip("Color para estructuras basadas en ruido")]
    public Color noiseStructureColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Internal Structures - Random (Legacy)")]
    [Tooltip("Usar el sistema antiguo de estructuras aleatorias")]
    public bool useLegacyStructures = false;
    
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
        
        // Guardar la imagen de ruido
        if (saveNoiseImage)
        {
            SaveNoiseAsImage(noise);
        }
        
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

    void SaveNoiseAsImage(float[,] noiseMap)
    {
#if UNITY_EDITOR
        try
        {
            // Crear textura a partir del mapa de ruido
            Texture2D texture = new Texture2D(noiseResolution, noiseResolution);
            
            for (int x = 0; x < noiseResolution; x++)
            {
                for (int y = 0; y < noiseResolution; y++)
                {
                    float value = noiseMap[x, y];
                    Color color = new Color(value, value, value); // Escala de grises
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            
            // Convertir a PNG
            byte[] bytes = texture.EncodeToPNG();
            
            // Crear la carpeta si no existe
            string folderPath = "Assets/Noise_Imagenes";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log("Carpeta 'Noise_Imagenes' creada en Assets");
            }
            
            // Generar nombre único con timestamp
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{noiseImageBaseName}_{seed}_{timestamp}.png";
            string filePath = Path.Combine(folderPath, fileName);
            
            // Guardar el archivo
            File.WriteAllBytes(filePath, bytes);
            
            Debug.Log($"Imagen de ruido guardada en: {filePath}");
            
            // Actualizar el Asset Database para que Unity detecte el nuevo archivo
            UnityEditor.AssetDatabase.Refresh();
            
            // Limpiar memoria
            DestroyImmediate(texture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar imagen de ruido: {e.Message}");
        }
#else
        Debug.LogWarning("SaveNoiseAsImage solo funciona en el Editor de Unity");
#endif
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
        
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer != null)
        {
            floorRenderer.material.color = baseColor;
        }

        // ===== OUTER WALLS =====
        float wallThickness = 0.2f;

        CreateWall("Wall_North", new Vector3(0, wallHeight / 2f, roomSize / 2f),
                   new Vector3(roomSize, wallHeight, wallThickness),
                   baseColor);

        CreateWall("Wall_South", new Vector3(0, wallHeight / 2f, -roomSize / 2f),
                   new Vector3(roomSize, wallHeight, wallThickness),
                   baseColor);

        CreateWall("Wall_East", new Vector3(roomSize / 2f, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, roomSize),
                   baseColor);

        CreateWall("Wall_West", new Vector3(-roomSize / 2f, wallHeight / 2f, 0),
                   new Vector3(wallThickness, wallHeight, roomSize),
                   baseColor);

        // ===== INTERNAL STRUCTURES =====
        if (useNoiseBasedStructures)
        {
            // NUEVO: Generar estructuras basadas en el mapa de ruido
            GenerateNoiseBasedStructures(noise, roomSize);
        }
        else if (useLegacyStructures)
        {
            // LEGACY: Sistema antiguo de estructuras aleatorias
            GenerateInternalStructures(roomSize, wallHeight);
        }

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

    void CreateWall(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.parent = transform;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            wallRenderer.material.color = color;
        }
    }

    // ===== NUEVO SISTEMA: ESTRUCTURAS BASADAS EN RUIDO =====
    void GenerateNoiseBasedStructures(float[,] noise, float roomSize)
    {
        // Crear un objeto padre para organizar todas las estructuras de ruido
        GameObject structuresParent = new GameObject("NoiseStructures");
        structuresParent.transform.parent = transform;

        int structuresGenerated = 0;

        // Recorrer el mapa de ruido
        for (int x = 0; x < noiseResolution; x++)
        {
            for (int z = 0; z < noiseResolution; z++)
            {
                float noiseValue = noise[x, z];

                // Si el valor del ruido está por debajo del umbral, crear una estructura
                if (noiseValue < structureThreshold)
                {
                    // Convertir coordenadas del mapa de ruido a coordenadas del mundo
                    // Centrar en la habitación
                    float worldX = ((x / (float)noiseResolution) - 0.5f) * roomSize;
                    float worldZ = ((z / (float)noiseResolution) - 0.5f) * roomSize;

                    // Crear el cubo de la estructura
                    GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    structure.name = $"NoiseStructure_{x}_{z}";
                    structure.transform.parent = structuresParent.transform;

                    // Posicionar y escalar
                    structure.transform.position = new Vector3(
                        worldX,
                        noiseStructureHeight / 2f,
                        worldZ
                    );
                    
                    structure.transform.localScale = new Vector3(
                        cellSize,
                        noiseStructureHeight,
                        cellSize
                    );

                    // Aplicar color
                    Renderer r = structure.GetComponent<Renderer>();
                    if (r != null)
                    {
                        if (useRandomColorsForNoise)
                        {
                            // Color basado en el valor del ruido para variación
                            r.material.color = GetNoiseBasedColor(noiseValue);
                        }
                        else
                        {
                            r.material.color = noiseStructureColor;
                        }
                    }

                    structuresGenerated++;
                }
            }
        }

        Debug.Log($"Generadas {structuresGenerated} estructuras basadas en ruido");
    }

    // Generar color basado en el valor del ruido
    Color GetNoiseBasedColor(float noiseValue)
    {
        // Cuanto más oscuro el ruido, más oscuro el color
        // Pero con variación de tono
        float hue = Random.Range(0f, 1f);
        float saturation = Mathf.Lerp(0.3f, 0.8f, 1f - noiseValue);
        float value = Mathf.Lerp(0.2f, 0.6f, 1f - noiseValue);
        
        return Color.HSVToRGB(hue, saturation, value);
    }

    // ===== SISTEMA LEGACY: ESTRUCTURAS ALEATORIAS =====
    // Este método está comentado pero disponible si se activa useLegacyStructures
    void GenerateInternalStructures(float roomSize, float wallHeight)
    {
        int count = Random.Range(minInternalStructures, maxInternalStructures + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject structure = GameObject.CreatePrimitive(PrimitiveType.Cube);
            structure.name = $"InternalStructure_{i}";
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
            
            if (r != null)
            {
                // ===== ASIGNAR COLOR ALEATORIO O FIJO =====
                if (useRandomColors)
                {
                    r.material.color = GetRandomColor();
                }
                else
                {
                    r.material.color = internalStructureColor;
                }
            }
        }
    }

    Color GetRandomColor()
    {
        return Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);
    }

    void SpawnRandomObjects(float roomSize)
    {
        int objectCount = Random.Range(minObjects, maxObjects + 1);

        for (int i = 0; i < objectCount; i++)
        {
            GameObject obj = null;

            // Intentar usar prefabs si están disponibles
            if (spawnablePrefabs != null && spawnablePrefabs.Length > 0 && Random.value > 0.4f)
            {
                GameObject prefab = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];
                if (prefab != null)
                {
                    obj = Instantiate(prefab);
                }
            }
            
            // Si no hay prefab válido, crear primitivo
            if (obj == null)
            {
                PrimitiveType type = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                obj = GameObject.CreatePrimitive(type);
            }

            obj.name = $"SpawnedObject_{i}";
            obj.transform.parent = transform;

            float margin = 1f;
            float x = Random.Range(-roomSize / 2f + margin, roomSize / 2f - margin);
            float z = Random.Range(-roomSize / 2f + margin, roomSize / 2f - margin);

            obj.transform.position = new Vector3(x, 0.6f, z);
            obj.transform.rotation = Random.rotation;
            obj.transform.localScale *= Random.Range(0.4f, 1.2f);

            // ===== ADD PHYSICS (opcional) =====
            if (addPhysicsToObjects)
            {
                // Verificar si ya tiene un Rigidbody
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody>();
                }
                
                rb.mass = Random.Range(0.5f, 3f);
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // ===== ADD VR INTERACTION (opcional) =====
            if (addVRInteractable)
            {
                // Intentar añadir el componente solo si existe
                try
                {
                    var componentType = System.Type.GetType("BasisPickupInteractable");
                    if (componentType != null)
                    {
                        obj.AddComponent(componentType);
                    }
                    else
                    {
                        Debug.LogWarning("BasisPickupInteractable no encontrado. ¿Está instalado Basis SDK?");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"No se pudo añadir BasisPickupInteractable: {e.Message}");
                }
            }
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
