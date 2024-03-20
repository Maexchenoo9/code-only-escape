using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class Instantiation
{
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        //Create GameScript
        GameObject obj = new GameObject("GameScript");
        obj.AddComponent<GameScript>();
        SceneManager.MoveGameObjectToScene(obj, SceneManager.GetSceneByBuildIndex(0));
    }
}

public class GameScript : MonoBehaviour
{
    public enum SceneRestartState
    {
        None,
        MoveToDontDestroyOnLoadAndLoadNewScene,
        WaitForNewScene,
        MoveToNewScene,
        RunAwakeAndStart
    }
    public bool restartScene = false;
    SceneRestartState sceneRestartState;

    private void Awake()
    {
        if (restartScene)
            return;

        SetCodeMade();
        CreateCamera();
    }

    private void Start()
    {
        if (restartScene)
            return;

        GenerationStartBahviour();
        AudioStartBehaviour();

        CreatePlayer(new Vector2(0f, 2f));

        float camHeight = 2f * CodeMade.MainCamera.camera.orthographicSize;
        float camWidth = camHeight * CodeMade.MainCamera.camera.aspect;
        CreateScore(CodeMade.bestScore, new Vector2(camWidth * -0.5f + 1f, camHeight * 0.5f - 1f), 0.75f, CodeMade.Colors.b);
        CreateScore(CodeMade.score, new Vector2(camWidth * -0.5f + 1f, camHeight * 0.5f - 2f), 0.75f, CodeMade.Colors.d);
    }

    private void Update()
    {
        HandleSceneRestart();
        if (restartScene)
            return;

        DebugUpdateBehaviour();
        CameraUpdateBehaviour();
        PlayerUpdateBehaviour();
    }

    private void FixedUpdate()
    {
        if (restartScene)
            return;

        PlayerFixedUpdateBehaviour();
    }

    void SetCodeMade()
    {
        if (ColorUtility.TryParseHtmlString("#191A19", out Color a))
            CodeMade.Colors.a = a;
        if (ColorUtility.TryParseHtmlString("#1E5128", out Color b))
            CodeMade.Colors.b = b;
        if (ColorUtility.TryParseHtmlString("#4E9F3D", out Color c))
            CodeMade.Colors.c = c;
        if (ColorUtility.TryParseHtmlString("#D8E9A8", out Color d))
            CodeMade.Colors.d = d;

        if (CodeMade.Textures2D.square == null)
            CodeMade.Textures2D.square = CreateSquareTexture2D();
        if (CodeMade.Sprites.square == null)
            CodeMade.Sprites.square = Texture2DToSprite(CodeMade.Textures2D.square);
        if (CodeMade.Sprites.circle == null)
            CodeMade.Sprites.circle = Texture2DToSprite(CreateCircleTexture2D());

        if (CodeMade.Audio.gameObject == null)
        {
            CodeMade.Audio.gameObject = new GameObject("Audio");
            DontDestroyOnLoad(CodeMade.Audio.gameObject);
        }
        CodeMade.Parents.spikes = new GameObject("Spikes").transform;
        CodeMade.Parents.grounds = new GameObject("Grounds").transform;

        Material mat = new Material(Shader.Find("UI/Default"));
        mat.mainTexture = CodeMade.Textures2D.square;
        CodeMade.Materials.defaultMaterial = mat;

        CodeMade.Generation.generationLayers = new List<CodeMade.Generation.GenerationLayer>();
        CodeMade.Generation.lastUpperGap = (0f, 0f);
        CodeMade.Generation.lastPosition = Vector2.zero;

        CodeMade.Audio.sounds = new List<CodeMade.Audio.Sound>();
    }
    void HandleSceneRestart()
    {
        if (restartScene)
        {
            sceneRestartState = SceneRestartState.MoveToDontDestroyOnLoadAndLoadNewScene;
            restartScene = false;
        }

        if (sceneRestartState != SceneRestartState.None)
            switch (sceneRestartState)
            {
                case SceneRestartState.MoveToDontDestroyOnLoadAndLoadNewScene:

                    DontDestroyOnLoad(gameObject);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

                    sceneRestartState = SceneRestartState.WaitForNewScene;
                    break;

                case SceneRestartState.WaitForNewScene:

                    if (SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0))
                        sceneRestartState = SceneRestartState.MoveToNewScene;
                    break;

                case SceneRestartState.MoveToNewScene:

                    SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

                    sceneRestartState = SceneRestartState.RunAwakeAndStart;
                    break;

                case SceneRestartState.RunAwakeAndStart:

                    Awake();
                    Start();

                    sceneRestartState = SceneRestartState.None;
                    break;
            }
    }

    //Create Textures
    Texture2D CreateSquareTexture2D()
    {
        //Create empty
        Texture2D texture = new Texture2D(256, 256);

        //Fill
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                texture.SetPixel(x, y, Color.white);

        //Apply
        texture.Apply();

        return texture;
    }
    Texture2D CreateCircleTexture2D()
    {
        //Create empty
        Texture2D texture = new Texture2D(256, 256);

        //Fill with null
        for (int sy = 0; sy < texture.height; sy++)
            for (int sx = 0; sx < texture.width; sx++)
                texture.SetPixel(sx, sy, new Color(0f, 0f, 0f, 0f));

        //Draw Circle
        int cx = texture.width / 2, cy = texture.height / 2, r = (texture.width + texture.height) / 4;
        int x, y, px, nx, py, ny, d;
        for (x = 0; x <= r; x++)
        {
            d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
            for (y = 0; y <= d; y++)
            {
                px = cx + x;
                nx = cx - x;
                py = cy + y;
                ny = cy - y;

                texture.SetPixel(px, py, Color.white);
                texture.SetPixel(nx, py, Color.white);

                texture.SetPixel(px, ny, Color.white);
                texture.SetPixel(nx, ny, Color.white);
            }
        }

        //Apply
        texture.Apply();

        return texture;
    }
    Sprite Texture2DToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 256);
    }

    //Audio
    AudioClip CreateAudioClip(CodeMade.Audio.Type type, int sampleFreq = 44000, float frequency = 440)
    {
        string name = "Clip";
        float[] samples = new float[44000];
        if (type == CodeMade.Audio.Type.Sine)
        {
            name = "Sine";
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(Mathf.PI * 2 * i * frequency / sampleFreq);
        }
        else if (type == CodeMade.Audio.Type.Rectangle)
        {
            name = "Rectangle";
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (Mathf.Repeat(i * frequency / sampleFreq, 1) > 0.5f) ? 1f : -1f;
        }
        else if (type == CodeMade.Audio.Type.Sawtooth)
        {
            name = "Sawtooth";
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Repeat(i * frequency / sampleFreq, 1) * 2f - 1f;
        }
        else if (type == CodeMade.Audio.Type.Triangle)
        {
            name = "Triangle";
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.PingPong(i * 2f * frequency / sampleFreq, 1) * 2f - 1f;
        }

        AudioClip ac = AudioClip.Create(name, samples.Length, 1, sampleFreq, false);
        ac.SetData(samples, 0);
        return ac;
    }
    CodeMade.Audio.Sound CreateSound(string name, AudioClip clip, float volume, float pitch)
    {
        AudioSource audioSource = CodeMade.Audio.gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;

        CodeMade.Audio.Sound sound = new CodeMade.Audio.Sound();
        sound.name = name;
        sound.source = audioSource;

        CodeMade.Audio.sounds.Add(sound);
        return sound;
    }
    CodeMade.Audio.Sound FindSound(string name)
    {
        return System.Array.Find(CodeMade.Audio.sounds.ToArray(), sound => sound.name == name);
    }

    //Create Simple GameObjects
    GameObject CreateSimpleSquare(Vector2 position, Vector2 scale, Color color)
    {
        GameObject obj = new GameObject("Square");
        obj.transform.position = position;
        obj.transform.localScale = scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CodeMade.Sprites.square;
        sr.color = color;

        return obj;
    }
    GameObject CreateSimpleCircle(Vector2 position, Vector2 scale, Color color)
    {
        GameObject obj = new GameObject("Circle");
        obj.transform.position = position;
        obj.transform.localScale = scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CodeMade.Sprites.circle;
        sr.color = color;

        return obj;
    }
    GameObject CreateSimpleTriangle(Vector2 position, Vector2 scale, Color color)
    {
        GameObject obj = new GameObject("Triangle");
        obj.transform.position = position;
        obj.transform.localScale = new Vector2(scale.x, scale.y * 2f);

        //Main
        GameObject main = new GameObject("Main");
        main.transform.SetParent(obj.transform);
        main.transform.localPosition = new Vector2(0f, -0.25f);
        main.transform.localRotation = Quaternion.AngleAxis(45f, Vector3.forward);
        main.transform.localScale = new Vector2(0.7f, 0.7f);

        SpriteRenderer mainSR = main.AddComponent<SpriteRenderer>();
        mainSR.sprite = CodeMade.Sprites.square;
        mainSR.color = color;
        mainSR.sortingOrder = -1;

        return obj;
    }

    //Create GameObjects
    GameObject CreateCamera()
    {
        GameObject obj = new GameObject("MainCamera");
        obj.transform.position = new Vector3(0f, 0f, -10f);
        obj.tag = "MainCamera";
        CodeMade.MainCamera.gameObject = obj;

        Camera cam = obj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = CodeMade.Colors.a;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.nearClipPlane = 0f;
        cam.farClipPlane = 25f;
        cam.depth = -1f;
        CodeMade.MainCamera.camera = cam;

        obj.AddComponent<AudioListener>();

        return obj;
    }
    GameObject CreateGround(Vector2 position, Vector2 scale)
    {
        GameObject obj = CreateSimpleSquare(position, scale, CodeMade.Colors.c);
        obj.transform.SetParent(CodeMade.Parents.grounds);
        obj.name = "Ground";
        obj.AddComponent<BoxCollider2D>();

        return obj;
    }
    GameObject CreatePlayer(Vector2 position)
    {
        GameObject obj = CreateSimpleCircle(position, Vector2.one, CodeMade.Colors.d);
        obj.name = "Player";
        obj.tag = "Player";
        CodeMade.Player.gameObject = obj;

        obj.AddComponent<CircleCollider2D>();

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 5f;
        CodeMade.Player.rb = rb;

        CreateTrail(Vector3.zero, 0.75f, CodeMade.Colors.d, obj.transform);
        CreateTrail(Vector2.one, 0.25f, CodeMade.Colors.b, obj.transform);

        return obj;
    }
    GameObject CreateSpike(Vector2 position, Quaternion rotation)
    {
        GameObject obj = CreateSimpleTriangle(position, Vector2.one, CodeMade.Colors.b);
        obj.transform.rotation = rotation;
        obj.transform.SetParent(CodeMade.Parents.spikes);
        obj.name = "Spike";

        GameObject child = obj.transform.GetChild(0).gameObject;
        child.tag = "Spike";
        child.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;

        return obj;
    }
    GameObject CreateGenerationLayer(CodeMade.Generation.GenerationLayer generationLayer)
    {
        Transform parent = new GameObject("GenerationLayer").transform;

        (float, float) bottomScaleLPosX = (generationLayer.position.x + generationLayer.width * -0.5f, generationLayer.bottomGap.Item2 + generationLayer.bottomGap.Item1 * -0.5f);
        (float, float) bottomScaleRPosX = (generationLayer.bottomGap.Item2 + generationLayer.bottomGap.Item1 * 0.5f, generationLayer.position.x + generationLayer.width * 0.5f);
        (float, float) upperScaleLPosX = (generationLayer.position.x + generationLayer.width * -0.5f, generationLayer.upperGap.Item2 + generationLayer.upperGap.Item1 * -0.5f);
        (float, float) upperScaleRPosX = (generationLayer.upperGap.Item2 + generationLayer.upperGap.Item1 * 0.5f, generationLayer.position.x + generationLayer.width * 0.5f);
        //from xPos, to xPos

        GameObject floorL = CreateGround(new Vector2((bottomScaleLPosX.Item1 + bottomScaleLPosX.Item2) * 0.5f, generationLayer.position.y), new Vector2(Mathf.Abs(bottomScaleLPosX.Item2 - bottomScaleLPosX.Item1), 1f));
        GameObject floorR = CreateGround(new Vector2((bottomScaleRPosX.Item1 + bottomScaleRPosX.Item2) * 0.5f, generationLayer.position.y), new Vector2(Mathf.Abs(bottomScaleRPosX.Item2 - bottomScaleRPosX.Item1), 1f));
        GameObject ceilingL = CreateGround(new Vector2((upperScaleLPosX.Item1 + upperScaleLPosX.Item2) * 0.5f, generationLayer.position.y + CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)), new Vector2(Mathf.Abs(upperScaleLPosX.Item2 - upperScaleLPosX.Item1), 1f));
        GameObject ceilingR = CreateGround(new Vector2((upperScaleRPosX.Item1 + upperScaleRPosX.Item2) * 0.5f, generationLayer.position.y + CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)), new Vector2(Mathf.Abs(upperScaleRPosX.Item2 - upperScaleRPosX.Item1), 1f));
        GameObject wallL = CreateGround(generationLayer.position + new Vector2(generationLayer.width * -0.5f, CodeMade.Generation.height * 0.5f * (generationLayer.subLayersAmount + 1)), new Vector2(1f, CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)));
        GameObject wallR = CreateGround(generationLayer.position + new Vector2(generationLayer.width * 0.5f, CodeMade.Generation.height * 0.5f * (generationLayer.subLayersAmount + 1)), new Vector2(1f, CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)));
        floorL.transform.SetParent(parent);
        floorR.transform.SetParent(parent);
        ceilingL.transform.SetParent(parent);
        ceilingR.transform.SetParent(parent);
        wallL.transform.SetParent(parent);
        wallR.transform.SetParent(parent);

        CreateSimpleSquare(generationLayer.position + new Vector2(generationLayer.width * -0.5f, 0f), Vector2.one, CodeMade.Colors.c).transform.SetParent(parent);
        CreateSimpleSquare(generationLayer.position + new Vector2(generationLayer.width * 0.5f, 0f), Vector2.one, CodeMade.Colors.c).transform.SetParent(parent);
        CreateSimpleSquare(generationLayer.position + new Vector2(generationLayer.width * -0.5f, CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)), Vector2.one, CodeMade.Colors.c).transform.SetParent(parent);
        CreateSimpleSquare(generationLayer.position + new Vector2(generationLayer.width * 0.5f, CodeMade.Generation.height * (generationLayer.subLayersAmount + 1)), Vector2.one, CodeMade.Colors.c).transform.SetParent(parent);

        if (generationLayer.subLayersAmount > 0)
        {
            for (int atLayer = 0; atLayer < generationLayer.subLayers.Length; atLayer++)
            {
                int maxGroundAmount = generationLayer.subLayers[atLayer];
                for (int atGround = 1; atGround < maxGroundAmount + 1; atGround++)
                {
                    float widthPart = (generationLayer.width - 1f) / maxGroundAmount;

                    float scaleMax = widthPart - generationLayer.minSubLayerGapWidth - (atGround == 1 ? generationLayer.minSubLayerGapWidth * 0.25f : 0f) - (atGround == maxGroundAmount ? generationLayer.minSubLayerGapWidth * 0.25f : 0f);
                    ExtraClass.InitRandomSeed();
                    float randomScale = Random.Range(generationLayer.minSubLayerWidth, scaleMax);

                    float centerPos = widthPart * atGround - widthPart * 0.5f + (atGround == 1 ? generationLayer.minSubLayerGapWidth * 0.25f : 0f) + (atGround == maxGroundAmount ? generationLayer.minSubLayerGapWidth * -0.25f : 0f) - (generationLayer.width - 1f) * 0.5f;
                    float posRangeMin = scaleMax * -0.5f + randomScale * 0.5f + centerPos;
                    float posRangeMax = scaleMax * 0.5f + randomScale * -0.5f + centerPos;
                    ExtraClass.InitRandomSeed();
                    float randomPos = Random.Range(posRangeMin, posRangeMax);

                    GameObject ground = CreateGround(generationLayer.position + new Vector2(randomPos, CodeMade.Generation.height * (atLayer + 1)), new Vector2(randomScale, 1f));

                    for (int i = 0; i < (int)Mathf.Floor(ground.transform.localScale.x / 1f); i++)
                    {
                        if (ExtraClass.RandomChance(1f + CodeMade.score * 0.25f / (generationLayer.subLayers.Length - atLayer + 1) * 50f))
                        {
                            float spikePosRangeMin = ground.transform.localScale.x * -0.5f + 0.5f + ground.transform.position.x;
                            float spikePosRangeMax = ground.transform.localScale.x * 0.5f + -0.5f + ground.transform.position.x;
                            float randomSpikePos = Random.Range(spikePosRangeMin, spikePosRangeMax);
                            CreateSpike(new Vector2(randomSpikePos, ground.transform.position.y + 1f), Quaternion.identity);
                        }
                        if (ExtraClass.RandomChance(1f + CodeMade.score * 0.25f / (generationLayer.subLayers.Length - atLayer + 1) * 75f))
                        {
                            float spikePosRangeMin = ground.transform.localScale.x * -0.5f + 0.5f + ground.transform.position.x;
                            float spikePosRangeMax = ground.transform.localScale.x * 0.5f + -0.5f + ground.transform.position.x;
                            float randomSpikePos = Random.Range(spikePosRangeMin, spikePosRangeMax);
                            CreateSpike(new Vector2(randomSpikePos, ground.transform.position.y - 1f), Quaternion.AngleAxis(180f, Vector3.forward));
                        }

                        if (ExtraClass.RandomChance(1f + CodeMade.score * 0.25f / (generationLayer.subLayers.Length - atLayer + 1 + i) * 25f) && atLayer < 1 + CodeMade.score * 0.25f)
                            CreateEnemy(generationLayer.position + new Vector2(Random.Range(generationLayer.width * -0.5f + 1f, generationLayer.width * 0.5f - 1f), CodeMade.Generation.height * (generationLayer.subLayersAmount + 1) - CodeMade.Generation.height * 0.5f));
                    }
                }
            }
        }

        return parent.gameObject;
    }
    void CreateScore(int value, Vector2 offset, float distance, Color color)
    {
        for (int i = 0; i < value; i++)
        {
            GameObject obj = CreateSimpleCircle((Vector2)CodeMade.MainCamera.gameObject.transform.position + offset + new Vector2(i * distance, 0f), Vector2.one * 0.5f, color);
            obj.GetComponent<SpriteRenderer>().sortingOrder = 10;
            obj.transform.SetParent(CodeMade.MainCamera.gameObject.transform);

            if (i > 0 && i % 5 == 0)
            {
                GameObject dot = CreateSimpleCircle((Vector2)CodeMade.MainCamera.gameObject.transform.position + offset + new Vector2(((i - 1) * distance + i * distance) * 0.5f, -0.25f), Vector2.one * (i % 10 == 0 ? 0.2f : 0.1f), color);
                dot.GetComponent<SpriteRenderer>().sortingOrder = 10;
                dot.transform.SetParent(CodeMade.MainCamera.gameObject.transform);
            }
        }
        return;
    }
    GameObject CreateTrail(Vector3 localPosition, float width, Color color, Transform parent)
    {
        GameObject obj = new GameObject("Trail");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;

        TrailRenderer tr = obj.AddComponent<TrailRenderer>();
        tr.startWidth = width;
        tr.endWidth = width;
        tr.time = 0.15f;
        tr.material = CodeMade.Materials.defaultMaterial;
        Gradient gradient = new Gradient();
        gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) };
        gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
        tr.colorGradient = gradient;
        tr.sortingOrder = -2;

        CodeMade.Player.tr = tr;

        return obj;
    }
    GameObject CreateEnemy(Vector2 position)
    {
        CreateSimpleCircle(position, Vector2.one * 0.5f, CodeMade.Colors.b.ChangeAlpha(0.25f));
        GameObject obj = CreateSimpleCircle(position, Vector2.one * Random.Range(0.5f, 1.5f), CodeMade.Colors.b);
        obj.name = "Enemy";
        obj.tag = "Enemy";

        obj.AddComponent<CircleCollider2D>().radius = 0.4f;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 3f;
        rb.AddForce(new Vector2(Random.value * -Random.value, Random.value * -Random.value), ForceMode2D.Impulse);

        CreateTrail(Vector3.zero, 0.75f, CodeMade.Colors.b, obj.transform);

        return obj;
    }

    //Behaviours
    void DebugUpdateBehaviour()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            if (sceneRestartState == SceneRestartState.None)
            {
                restartScene = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (sceneRestartState == SceneRestartState.None)
            {
                CodeMade.score++;
                if (CodeMade.bestScore < CodeMade.score)
                    CodeMade.bestScore = CodeMade.score;
                restartScene = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            CodeMade.Player.Values.die = true;
        }
    }

    void AudioStartBehaviour()
    {
        if (CodeMade.Audio.sounds.Count > 0)
        {
            return;
        }

        CreateSound("Jump", CreateAudioClip(CodeMade.Audio.Type.Triangle, 176000), 0.5f, 0.9f);
        CreateSound("ExtraJump", CreateAudioClip(CodeMade.Audio.Type.Triangle, 176000), 0.5f, 1f);
        CreateSound("End", CreateAudioClip(CodeMade.Audio.Type.Rectangle, 176000), 0.6f, 1.5f);
        CreateSound("Death", CreateAudioClip(CodeMade.Audio.Type.Sawtooth, 176000), 0.6f, 0.5f);
    }

    void PlayerUpdateBehaviour()
    {
        if (CodeMade.Player.gameObject == null)
        {
            return;
        }

        //Movement Input
        CodeMade.Player.Values.moveDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (CodeMade.Player.Values.moveDir != Vector2.zero)
            CodeMade.Player.Values.lastMoveDir = CodeMade.Player.Values.moveDir;

        if (!CodeMade.Player.Values.die)
        {
            //IsGrounded
            CodeMade.Player.Values.isGrounded = Mathf.Abs(CodeMade.Player.rb.velocity.y) < 0.005f;

            //Jump
            if (Input.GetButtonDown("Jump"))
            {
                if (CodeMade.Player.Values.extraJump && !CodeMade.Player.Values.isGrounded)
                {
                    CodeMade.Player.rb.AddForce(new Vector2(0f, CodeMade.Player.Values.jumpForce * CodeMade.Player.rb.mass), ForceMode2D.Impulse);
                    FindSound("ExtraJump").source.Play();

                    CodeMade.Player.Values.extraJump = false;
                }
                else if (CodeMade.Player.Values.isGrounded)
                {
                    CodeMade.Player.rb.AddForce(new Vector2(0f, CodeMade.Player.Values.jumpForce * CodeMade.Player.rb.mass), ForceMode2D.Impulse);
                    FindSound("Jump").source.Play();

                    CodeMade.Player.Values.extraJump = true;
                }
            }

            //Detection
            Collider2D[] colliders = new Collider2D[32];
            colliders = Physics2D.OverlapCircleAll(CodeMade.Player.gameObject.transform.position, 2f);
            if (colliders.Length > 0)
            {
                foreach (Collider2D c in colliders)
                {
                    if (c.CompareTag("Spike"))
                    {
                        foreach (Collider2D c2 in Physics2D.OverlapBoxAll(
                            c.transform.position,
                            new Vector2(0.25f, 0.5f),
                            0f
                            ))
                        {
                            if (c2.gameObject == CodeMade.Player.gameObject)
                                CodeMade.Player.Values.die = true;
                        }
                    }
                }
            }

            //End
            if (CodeMade.Player.gameObject.transform.position.y > CodeMade.Generation.lastPosition.y + CodeMade.Generation.height * (CodeMade.Generation.generationLayers.GetLast().subLayersAmount + 1) + 0.99f)
            {
                if (sceneRestartState == SceneRestartState.None)
                {
                    FindSound("End").source.Play();
                    CodeMade.score++;
                    if (CodeMade.bestScore < CodeMade.score)
                        CodeMade.bestScore = CodeMade.score;
                    restartScene = true;
                }
            }

            //Glitch
            if (CodeMade.Player.gameObject.transform.position.y < -1f)
            {
                if (sceneRestartState == SceneRestartState.None)
                    restartScene = true;
            }
        }
        //Death
        else
        {
            CodeMade.Player.rb.isKinematic = true;

            FindSound("Death").source.Play();

            CodeMade.score = 1;

            if (sceneRestartState == SceneRestartState.None)
                restartScene = true;

            CodeMade.Player.Values.die = false;
        }
    }
    void PlayerFixedUpdateBehaviour()
    {
        if (CodeMade.Player.gameObject == null)
        {
            return;
        }

        //Movement
        float speedDelta = CodeMade.Player.Values.movementSpeed * 60f * Time.fixedDeltaTime;
        CodeMade.Player.rb.velocity = new Vector3(CodeMade.Player.Values.moveDir.x * speedDelta, CodeMade.Player.rb.velocity.y, 0f);

        //Down Drag
        if (CodeMade.Player.Values.moveDir.y < 0f && !CodeMade.Player.Values.isGrounded && CodeMade.Player.rb.velocity.y > -16f)
        {
            CodeMade.Player.rb.AddForce(new Vector2(0f, CodeMade.Player.Values.moveDir.y * CodeMade.Player.Values.downDragForce) * 60f * Time.fixedDeltaTime, ForceMode2D.Impulse);
        }

        //Up Drag
        if (CodeMade.Player.Values.moveDir.y > 0f && !CodeMade.Player.Values.isGrounded)
        {
            CodeMade.Player.rb.AddForce(new Vector2(0f, -CodeMade.Player.rb.velocity.y * CodeMade.Player.Values.upDragPercentage) * 60f * Time.fixedDeltaTime, ForceMode2D.Impulse);
        }

        //Detection
        Collider2D[] colliders = new Collider2D[32];
        colliders = Physics2D.OverlapCircleAll(CodeMade.Player.gameObject.transform.position, 4f);
        if (colliders.Length > 0)
        {
            foreach (Collider2D c in colliders)
            {
                if (c.CompareTag("Enemy"))
                {
                    foreach (Collider2D c2 in Physics2D.OverlapCircleAll(
                        c.transform.position,
                        0.5f
                        ))
                    {
                        if (c2.gameObject == CodeMade.Player.gameObject)
                            CodeMade.Player.Values.die = true;
                    }
                    EnemyMoveBehaviour(c.transform);
                }
            }
        }
    }

    void CameraUpdateBehaviour()
    {
        if (CodeMade.MainCamera.gameObject == null)
        {
            return;
        }

        Vector3 lerpTo = CodeMade.MainCamera.gameObject.transform.position;
        if (CodeMade.Player.gameObject != null)
            lerpTo = new Vector3(CodeMade.Player.gameObject.transform.position.x, CodeMade.Player.gameObject.transform.position.y, -10f);
        CodeMade.MainCamera.gameObject.transform.position = Vector3.Lerp(CodeMade.MainCamera.gameObject.transform.position, lerpTo, CodeMade.MainCamera.Values.lerpSpeed * Time.deltaTime);
    }

    void GenerationStartBahviour()
    {
        for (int i = 0; i < CodeMade.score; i++)
        {
            CodeMade.Generation.GenerationLayer generationLayer = new CodeMade.Generation.GenerationLayer();

            ExtraClass.InitRandomSeed();
            generationLayer.width = Random.Range(10f, 20f);

            if (CodeMade.Generation.generationLayers.Count <= 0)
            {
                generationLayer.position = Vector2.zero;
            }
            else
            {
                generationLayer.position = new Vector2(CodeMade.Generation.lastUpperGap.Item2, CodeMade.Generation.lastPosition.y + CodeMade.Generation.height * (CodeMade.Generation.generationLayers.GetLast().subLayersAmount + 1));
            }
            CodeMade.Generation.lastPosition = generationLayer.position;

            generationLayer.bottomGap = CodeMade.Generation.lastUpperGap;

            generationLayer.subLayersAmount = ExtraClass.HighRandomRange(1, 5) - ExtraClass.HighRandomRange(0, 1);
            generationLayer.minSubLayerGapWidth = 1.5f;
            generationLayer.minSubLayerWidth = 1f;
            generationLayer.SetRandomSubLayers(1);

            generationLayer.SetRandomUpperGap();
            CodeMade.Generation.lastUpperGap = generationLayer.upperGap;

            CreateGenerationLayer(generationLayer);
            CodeMade.Generation.generationLayers.Add(generationLayer);
        }
    }

    void EnemyMoveBehaviour(Transform enemy)
    {
        enemy.GetComponent<Rigidbody2D>().AddForce((CodeMade.Player.gameObject.transform.position - enemy.position).normalized * 60f * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }
}

public static class CodeMade
{
    public static class Colors
    {
        public static Color a, b, c, d;
    }

    public static class Textures2D
    {
        public static Texture2D square;
    }

    public static class Sprites
    {
        public static Sprite square, circle;
    }

    public static class Materials
    {
        public static Material defaultMaterial;
    }

    public static class MainCamera
    {
        public static GameObject gameObject;
        public static Camera camera;

        public static class Values
        {
            public static float lerpSpeed = 5f;
        }
    }

    public static class Parents
    {
        public static Transform grounds, spikes;
    }

    public static class Player
    {
        public static GameObject gameObject;
        public static Rigidbody2D rb;
        public static TrailRenderer tr;

        public static class Values
        {
            //Movement
            public static float movementSpeed = 6f;
            public static Vector2 moveDir;
            public static Vector2 lastMoveDir;

            //Jumping
            public static float jumpForce = 12f;
            public static bool isGrounded;
            public static bool extraJump;

            //Drags
            public static float downDragForce = 6f;
            public static float upDragPercentage = 0.5f;

            //Death
            public static bool die;
        }

    }

    public static int score = 1, bestScore = 1;

    public static class Generation
    {
        public static (float, float) lastUpperGap; //scale, xPosition
        public static Vector2 lastPosition;
        public static List<GenerationLayer> generationLayers = new List<GenerationLayer>();
        public static float height = 4f;

        public class GenerationLayer
        {
            public float width;
            public Vector2 position;
            public (float, float) bottomGap, upperGap; //scale, xPosition
            public int subLayersAmount;
            public float minSubLayerWidth, minSubLayerGapWidth;
            public int[] subLayers;

            public GenerationLayer()
            {

            }

            public void SetRandomSubLayers(int min)
            {
                int max = (int)Mathf.Floor(width / (minSubLayerWidth + minSubLayerGapWidth));
                subLayers = new int[subLayersAmount];
                for (int i = 0; i < subLayersAmount; i++)
                    subLayers[i] = ExtraClass.HighRandomRange(min, max);
            }

            public void SetRandomUpperGap()
            {
                float max = width - 1f - minSubLayerGapWidth;
                ExtraClass.InitRandomSeed();
                upperGap.Item1 = Random.Range(minSubLayerGapWidth, max * 0.75f);

                float posRangeMin = max * -0.5f + upperGap.Item1 * 0.5f + position.x;
                float posRangeMax = max * 0.5f + upperGap.Item1 * -0.5f + position.x;
                ExtraClass.InitRandomSeed();
                upperGap.Item2 = Random.Range(posRangeMin, posRangeMax);
            }
        }
    }

    public static class Audio
    {
        public enum Type { Sine, Rectangle, Sawtooth, Triangle }
        public static GameObject gameObject;
        public static List<Sound> sounds;

        public class Sound
        {
            public string name;
            public AudioSource source;

            public Sound()
            {

            }
        }
    }
}

public static class ExtraClass
{
    public static void InitRandomSeed()
    {
        Random.InitState(System.Environment.TickCount * Random.seed / 7);
    }

    public static int HighRandomRange(int min, int max)
    {
        InitRandomSeed();
        return (Random.Range(0, max * 100 + 1) % (max + 1 - min)) + min;
    }

    public static bool RandomChance(float percentage)
    {
        InitRandomSeed();
        return Random.value > 1f - percentage * 0.01f;
    }

    public static T GetLast<T>(this List<T> list)
    {
        return list[list.Count - 1];
    }

    public static Color ChangeAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}