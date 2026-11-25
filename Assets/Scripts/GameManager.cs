using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Ready, Playing, Quiz, QuizFeedback, Resume, GameOver }

/// <summary>Jenis kuis: Gate = gerbang di tengah jalur (hadiah power-up), Rescue = setelah menabrak (penyelamat).</summary>
public enum QuizKind { Gate, Rescue }

/// <summary>
/// Pengatur utama Quizy Bird: state permainan, kamera, dunia hutan Papua, spawner pipa / musuh / gerbang kuis,
/// power-up, score, suara, kuis Pancasila &amp; PKn, dan antarmuka (OnGUI).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const float GroundTop = -4f;
    public const int ObstacleLayer = 8;     // pipa dan musuh
    public const int BirdLayer = 9;         // burung (dipakai agar bisa menembus rintangan saat kebal)

    const float BaseSpeed = 3.2f;
    const float PipeSpacing = 5.2f;
    const int StartLives = 3;               // kesempatan kuis penyelamat di awal
    const int MaxLives = 5;
    const int GateEvery = 4;                // gerbang kuis muncul setiap 4 pipa
    const int EnemyStartScore = 3;          // musuh mulai muncul setelah score ini
    const float QuizTime = 15f;
    const float FeedbackTime = 2f;
    const float ShieldTime = 15f;
    const float SlowTime = 8f;
    const float SlowFactor = 0.6f;
    const string BestKey = "QuizyBird.HighScore";

    public GameState State { get; private set; }
    public int Score { get; private set; }
    public int Best { get; private set; }
    public bool NewRecord { get; private set; }
    public int QuizLives { get; private set; }
    public float HalfWidth { get; private set; }
    public float BirdY { get { return bird != null ? bird.transform.position.y : 0f; } }
    public float TopY { get { return cam != null ? cam.orthographicSize : 5f; } }

    public float CurrentSpeed { get { return BaseSpeed + Mathf.Min(Score * 0.04f, 1.6f); } }
    public float GameSpeedMul { get { return slowTimer > 0f ? SlowFactor : 1f; } }

    public float WorldSpeed
    {
        get
        {
            if (State == GameState.GameOver || State == GameState.Quiz || State == GameState.QuizFeedback) return 0f;
            if (State == GameState.Playing) return CurrentSpeed * GameSpeedMul;
            return BaseSpeed * 0.6f;
        }
    }

    Camera cam;
    BirdController bird;
    Sprite pipeBody, pipeCap;
    readonly List<PipePair> pipes = new List<PipePair>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<QuizGate> gates = new List<QuizGate>();
    float spawnTimer, lastCenter, nextCenter, gameOverTimer, flashStart = -10f;
    bool enemyPending, hasNext;
    int pipesSpawned;

    // power-up
    float shieldTimer, slowTimer;
    string toast = "";
    float toastUntil;

    // kuis
    QuizQuestion quiz;
    QuizKind quizKind;
    string[] shownOptions = new string[4];
    int correctIdx, selectedIdx = -1;
    float quizTimer, resumeTimer, feedbackT, quizShownAt;
    string correctText = "", feedbackMsg = "", feedbackNote = "";
    bool timedOut, answeredRight, endedByQuiz;

    AudioSource audioSrc;
    AudioClip sFlap, sScore, sHit, sDie, sPower;
    bool muted;
    public bool Muted { get { return muted; } }

    // UI
    GUIStyle labelBody, labelTitle;
    GUIStyle pnWood, pnDark, pnSoft, pnCream, pnLeaf, pnChip, pnTrack, pnBarGreen, pnBarAmber, pnBarRed, pnBadge, pnGood, pnBad;
    GUIStyle btnGreen, btnBrown, ansNormal, ansSelected, ansCorrect, ansWrong, ansDim;
    Texture2D white, heartTex, shieldTex, hourTex;

    // palet warna alam Nusantara
    static readonly Color Wood = new Color32(112, 72, 42, 255);
    static readonly Color WoodMid = new Color32(146, 96, 54, 255);
    static readonly Color WoodDark = new Color32(66, 40, 24, 255);
    static readonly Color Leaf = new Color32(52, 128, 74, 255);
    static readonly Color LeafLight = new Color32(92, 176, 104, 255);
    static readonly Color LeafDark = new Color32(30, 86, 50, 255);
    static readonly Color Gold = new Color32(244, 192, 66, 255);
    static readonly Color Cream = new Color32(255, 245, 216, 255);
    static readonly Color Ink = new Color32(58, 36, 22, 255);
    static readonly Color Red = new Color32(214, 74, 62, 255);
    static readonly Color RedDark = new Color32(132, 38, 34, 255);

    // ------------------------------------------------------------ setup
    void Awake()
    {
        Instance = this;
        Best = PlayerPrefs.GetInt(BestKey, 0);
        Physics2D.IgnoreLayerCollision(BirdLayer, ObstacleLayer, false);
        SetupCamera();
        SetupAudio();
        BuildWorld();
        ShowMenu();
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(255, 238, 196, 255);
        HalfWidth = cam.orthographicSize * cam.aspect;
    }

    void SetupAudio()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        sFlap = SoundFactory.Flap();
        sScore = SoundFactory.Score();
        sHit = SoundFactory.Hit();
        sDie = SoundFactory.Die();
        sPower = SoundFactory.PowerUp();
    }

    void BuildWorld()
    {
        // latar berlapis (dari yang terjauh): langit, awan, pegunungan bersalju, hutan jauh, hutan dekat, kampung Honai, tanah
        var cloudSprite = SpriteFactory.Clouds();
        var cloudsFar = ScrollingLayer.Create("CloudsFar", cloudSprite, 3.9f, -11, 0.05f, 0.5f, -1f);
        var mountains = ScrollingLayer.Create("Mountains", SpriteFactory.Mountains(), -1f, -10, 0.05f, 1.5f, -8f);
        var cloudsMid = ScrollingLayer.Create("CloudsMid", cloudSprite, 1.6f, -9, 0.09f, 1f, -3f);
        var clouds = ScrollingLayer.Create("Clouds", cloudSprite, 3.2f, -8, 0.12f);
        var farForest = ScrollingLayer.Create("ForestFar", SpriteFactory.FarForest(), GroundTop - 0.2f + 1.5f, -7, 0.15f);
        var midForest = ScrollingLayer.Create("ForestMid", SpriteFactory.MidForest(), GroundTop - 0.2f + 2f, -5, 0.3f);
        var village = ScrollingLayer.Create("Honai", SpriteFactory.HonaiVillage(), GroundTop - 0.2f + 1.5f, -3, 0.55f, 1f, -3f);
        var ground = ScrollingLayer.Create("Ground", SpriteFactory.Ground(), GroundTop - 0.5f, 5, 1f);
        var cycle = DayNightCycle.Create(new[] { cloudsFar, mountains, cloudsMid, clouds, farForest, midForest, village, ground });
        MusicPlayer.Create(cycle);

        var g = new GameObject("GroundCollider");
        g.transform.position = new Vector3(0f, GroundTop - 0.5f, 0f);
        g.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

        pipeBody = SpriteFactory.PipeBody();
        pipeCap = SpriteFactory.PipeCap();
        white = SpriteFactory.WhiteTexture();
        heartTex = SpriteFactory.Heart();
        shieldTex = SpriteFactory.ShieldIcon();
        hourTex = SpriteFactory.HourglassIcon();
        Enemy.LoadSprites();
        QuizGate.LoadSprites();

        bird = BirdController.Create(SpriteFactory.BirdFrames(), new Vector3(-BirdX(), 0.5f, 0f));
    }

    float BirdX() { return Mathf.Min(2.5f, HalfWidth * 0.4f); }

    // ------------------------------------------------------------ state
    void ClearObstacles()
    {
        foreach (var p in pipes) if (p != null) Destroy(p.gameObject);
        pipes.Clear();
        foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
        enemies.Clear();
        foreach (var q in gates) if (q != null) Destroy(q.gameObject);
        gates.Clear();
        enemyPending = false;
        hasNext = false;
    }

    void ResetPowerUps()
    {
        shieldTimer = 0f;
        slowTimer = 0f;
        toastUntil = 0f;
        if (bird != null) bird.SetShield(false, false);
    }

    void Toast(string msg)
    {
        toast = msg;
        toastUntil = Time.time + 1.8f;
    }

    void ShowMenu()
    {
        State = GameState.Menu;
        ClearObstacles();
        ResetPowerUps();
        Score = 0;
        bird.Hover();
    }

    void GoReady()
    {
        State = GameState.Ready;
        ClearObstacles();
        ResetPowerUps();
        Score = 0;
        NewRecord = false;
        QuizLives = StartLives;
        timedOut = false;
        endedByQuiz = false;
        correctText = "";
        bird.Hover();
    }

    void StartPlaying()
    {
        State = GameState.Playing;
        bird.Begin();
        bird.Flap();
        Play(sFlap);
        lastCenter = 0.5f;
        spawnTimer = 999f; // pipa pertama muncul langsung
        enemyPending = false;
        hasNext = false;
        pipesSpawned = 0;
        QuizLives = StartLives;
        QuizBank.BeginRun();
    }

    public void AddScore()
    {
        if (State != GameState.Playing) return;
        Score++;
        Play(sScore);
    }

    /// <summary>
    /// Burung menabrak. Urutan: perisai menyerap tabrakan; jika tidak ada, pakai satu kesempatan
    /// untuk kuis penyelamat; jika kesempatan habis, Game Over.
    /// </summary>
    public void OnBirdHit()
    {
        if (State != GameState.Playing || bird.Invulnerable) return;
        flashStart = Time.time;
        Play(sHit);

        if (shieldTimer > 0f)
        {
            shieldTimer = 0f;
            bird.SetShield(false, false);
            bird.GrantInvulnerable(1.5f);
            Toast("Perisai pecah!");
            return;
        }

        if (QuizLives > 0)
        {
            QuizLives--;
            StartQuiz(QuizKind.Rescue);
        }
        else
        {
            endedByQuiz = false;
            EndRun();
        }
    }

    /// <summary>Burung melewati Gerbang Kuis.</summary>
    public void OnGatePassed()
    {
        if (State != GameState.Playing) return;     // tidak pernah dua kuis bersamaan
        StartQuiz(QuizKind.Gate);
    }

    void StartQuiz(QuizKind kind)
    {
        State = GameState.Quiz;
        quizKind = kind;
        bird.Freeze();
        quiz = QuizBank.Next(Score);
        quizTimer = QuizTime;
        quizShownAt = Time.time;
        selectedIdx = -1;
        feedbackT = 0f;

        // acak urutan pilihan; jawaban benar ada di Options[0]
        var order = new List<int> { 0, 1, 2, 3 };
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = order[i]; order[i] = order[j]; order[j] = t;
        }
        for (int i = 0; i < 4; i++)
        {
            shownOptions[i] = quiz.Options[order[i]];
            if (order[i] == 0) correctIdx = i;
        }
        correctText = quiz.Options[0];
    }

    void AnswerQuiz(int choice)
    {
        if (State != GameState.Quiz) return;          // timer hanya berjalan pada state Quiz, berhenti setelah menjawab
        selectedIdx = choice;
        timedOut = choice < 0;
        answeredRight = choice == correctIdx;
        QuizBank.Record(quiz, answeredRight);
        State = GameState.QuizFeedback;
        feedbackT = 0f;

        if (answeredRight)
        {
            Play(sScore);
            feedbackMsg = quizKind == QuizKind.Gate ? GrantPowerUp() : "BENAR!  Kamu lanjut terbang";
            feedbackNote = "Klik atau tekan SPASI untuk lanjut.";
        }
        else
        {
            Play(sHit);
            feedbackMsg = (timedOut ? "WAKTU HABIS!" : "SALAH!") + "  Jawaban: " + correctText;
            feedbackNote = quizKind == QuizKind.Gate
                ? "Tidak ada hadiah kali ini. Klik atau tekan SPASI untuk lanjut."
                : "Permainan berakhir. Klik atau tekan SPASI untuk melihat hasil.";
        }
    }

    void FinishFeedback()
    {
        if (quizKind == QuizKind.Gate)
        {
            State = GameState.Playing;          // rintangan tetap di tempatnya; burung lanjut terbang
            bird.Unfreeze();
            bird.Flap();
            bird.GrantInvulnerable(0.8f);
        }
        else if (answeredRight)
        {
            ClearObstacles();
            bird.Hover();                       // burung kembali ke posisi awal
            State = GameState.Resume;
            resumeTimer = 1.5f;
        }
        else
        {
            endedByQuiz = true;
            EndRun();
        }
    }

    string GrantPowerUp()
    {
        var options = new List<int>();
        if (shieldTimer <= 0f) options.Add(0);
        if (slowTimer <= 0f) options.Add(1);
        if (QuizLives < MaxLives)
        {
            options.Add(2);
            if (QuizLives <= 1) { options.Add(2); options.Add(2); }   // hampir kehabisan nyawa: peluang nyawa lebih besar
        }
        Play(sPower);
        if (options.Count == 0)
        {
            slowTimer = SlowTime;
            return "BENAR!  Slow motion diperpanjang";
        }
        switch (options[Random.Range(0, options.Count)])
        {
            case 0:
                shieldTimer = ShieldTime;
                bird.SetShield(true, false);
                return "BENAR!  Perisai aktif";
            case 1:
                slowTimer = SlowTime;
                return "BENAR!  Slow motion aktif";
            default:
                QuizLives = Mathf.Min(MaxLives, QuizLives + 1);
                return "BENAR!  Nyawa +1";
        }
    }

    void EndRun()
    {
        State = GameState.GameOver;
        gameOverTimer = 0f;
        shieldTimer = 0f;
        slowTimer = 0f;
        bird.SetShield(false, false);
        bird.Fall();
        Play(sDie);
        if (Score > Best)
        {
            Best = Score;
            NewRecord = true;
            PlayerPrefs.SetInt(BestKey, Best);
        }
        PlayerPrefs.Save();
    }

    void Play(AudioClip c)
    {
        if (!muted && c != null) audioSrc.PlayOneShot(c);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------ loop
    void Update()
    {
        HalfWidth = cam.orthographicSize * cam.aspect;
        if (InputHelper.MutePressed()) muted = !muted;

        bool flap = InputHelper.FlapPressed();
        switch (State)
        {
            case GameState.Menu:
                if (InputHelper.ConfirmPressed()) GoReady();
                else if (InputHelper.EscapePressed()) QuitGame();
                break;

            case GameState.Ready:
                if (flap) StartPlaying();
                else if (InputHelper.EscapePressed()) ShowMenu();
                break;

            case GameState.Playing:
                if (flap)
                {
                    bird.Flap();
                    Play(sFlap);
                }
                UpdatePowerUps();
                spawnTimer += Time.deltaTime;
                float interval = PipeSpacing / (CurrentSpeed * GameSpeedMul);
                if (spawnTimer >= interval)
                {
                    spawnTimer = 0f;
                    SpawnPipe();
                    pipesSpawned++;
                    enemyPending = true;           // musuh / gerbang muncul di tengah jarak antara dua pipa
                }
                else if (enemyPending && spawnTimer >= interval * 0.5f)
                {
                    enemyPending = false;
                    if (pipesSpawned % GateEvery == 0) SpawnGate();
                    else TrySpawnEnemy();
                }
                if (bird.transform.position.y > cam.orthographicSize + 0.4f) OnBirdHit(); // menabrak batas atas
                break;

            case GameState.Quiz:
                quizTimer -= Time.deltaTime;
                int key = InputHelper.AnswerKeyPressed();
                if (key >= 0) AnswerQuiz(key);
                else if (quizTimer <= 0f) AnswerQuiz(-1);      // waktu habis dianggap salah
                break;

            case GameState.QuizFeedback:
                feedbackT += Time.deltaTime;
                if (feedbackT >= FeedbackTime || (feedbackT > 0.7f && flap)) FinishFeedback();
                break;

            case GameState.Resume:
                resumeTimer -= Time.deltaTime;
                if (resumeTimer <= 0f)
                {
                    State = GameState.Playing;
                    bird.Begin();
                    bird.Flap();
                    bird.GrantInvulnerable(2f);
                    spawnTimer = PipeSpacing / (CurrentSpeed * GameSpeedMul) - 0.5f;
                    enemyPending = false;
                    hasNext = false;
                }
                break;

            case GameState.GameOver:
                gameOverTimer += Time.deltaTime;
                if (gameOverTimer > 0.5f && InputHelper.ConfirmPressed()) GoReady();
                else if (InputHelper.EscapePressed()) ShowMenu();
                break;
        }
    }

    void UpdatePowerUps()
    {
        if (shieldTimer > 0f)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
            {
                shieldTimer = 0f;
                Toast("Perisai habis");
            }
        }
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowTimer = 0f;
                Toast("Slow motion selesai");
            }
        }
        bird.SetShield(shieldTimer > 0f, shieldTimer > 0f && shieldTimer < 3f);   // berkedip saat hampir habis
    }

    // memilih titik tengah celah pipa berikutnya (tidak terlalu jauh dari pipa sebelumnya)
    float PickCenter(float from, float gap)
    {
        float min = GroundTop + 1.0f + gap * 0.5f;
        float max = cam.orthographicSize - 1.0f - gap * 0.5f;
        float lo = Mathf.Max(min, from - 2.2f);
        float hi = Mathf.Min(max, from + 2.2f);
        if (lo > hi) lo = hi;
        return Random.Range(lo, hi);
    }

    void SpawnPipe()
    {
        float gap = Mathf.Max(2.6f, 3.0f - Score * 0.012f);
        float c = hasNext ? nextCenter : PickCenter(lastCenter, gap);
        lastCenter = c;
        nextCenter = PickCenter(c, gap);   // dipakai juga untuk menaruh musuh / gerbang di antara dua pipa
        hasNext = true;

        pipes.RemoveAll(p => p == null);
        var go = new GameObject("PipePair");
        go.transform.position = new Vector3(HalfWidth + 1.5f, 0f, 0f);
        var pair = go.AddComponent<PipePair>();
        pair.Build(pipeBody, pipeCap, c, gap);
        pipes.Add(pair);
    }

    /// <summary>Gerbang kuis di tengah antara pipa terakhir dan pipa berikutnya (menggantikan musuh pada giliran itu).</summary>
    void SpawnGate()
    {
        float mid = (lastCenter + nextCenter) * 0.5f;
        gates.RemoveAll(q => q == null);
        gates.Add(QuizGate.Create(HalfWidth + 1.5f, mid));
    }

    /// <summary>Munculkan musuh di tengah antara pipa terakhir dan pipa berikutnya.</summary>
    void TrySpawnEnemy()
    {
        if (Score < EnemyStartScore) return;
        float chance = Mathf.Min(0.75f, 0.4f + Score * 0.02f);
        if (Random.value > chance) return;

        float mid = (lastCenter + nextCenter) * 0.5f;
        float top = cam.orthographicSize - 1.2f, bottom = GroundTop + 1.2f;
        float r = Random.value;
        float x = HalfWidth + 1.5f;
        Enemy e;

        if (Score >= 12 && r < 0.25f)
        {
            // Bondol: muncul di tepi kanan setinggi burung, ada tanda peringatan dulu
            e = Enemy.Create(EnemyKind.Bondol, HalfWidth - 0.7f, BirdY, 0f);
        }
        else if (Score >= 8 && r < 0.6f)
        {
            // Kawung naik-turun lebar di tengah jalur
            float ampK = Mathf.Max(0.4f, Mathf.Min(1.5f, Mathf.Min(top - mid, mid - bottom)));
            e = Enemy.Create(EnemyKind.Kawung, x, mid, ampK);
        }
        else
        {
            // Enggang di sisi jalur yang lebih lapang
            const float ampE = 0.45f;
            float upRoom = (top - ampE) - mid, downRoom = mid - (bottom + ampE);
            float y = upRoom > downRoom ? mid + Mathf.Min(1.5f, upRoom) : mid - Mathf.Min(1.5f, downRoom);
            e = Enemy.Create(EnemyKind.Enggang, x, y, ampE);
        }

        enemies.RemoveAll(en => en == null);
        enemies.Add(e);
    }

    // ------------------------------------------------------------ UI: gaya
    static string Medal(int s)
    {
        if (s >= 40) return "Platinum";
        if (s >= 30) return "Emas";
        if (s >= 20) return "Perak";
        if (s >= 10) return "Perunggu";
        return "-";
    }

    void EnsureStyles()
    {
        if (labelBody != null) return;
        UiKit.Init();
        labelBody = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true, font = UiKit.BodyFont };
        labelTitle = new GUIStyle(labelBody) { font = UiKit.TitleFont, wordWrap = false };

        Color softBlack = new Color(0.1f, 0.07f, 0.05f, 0.62f);
        pnWood = UiKit.Panel(Wood, Gold, 18, 4);
        pnDark = UiKit.Panel(new Color(0.1f, 0.07f, 0.05f, 0.86f), Gold, 14, 2);
        pnSoft = UiKit.Panel(softBlack, softBlack, 10, 1);
        pnCream = UiKit.Panel(Cream, WoodDark, 14, 3);
        pnLeaf = UiKit.Panel(Leaf, LeafDark, 14, 3);
        pnChip = UiKit.Panel(new Color(0.26f, 0.16f, 0.1f, 0.95f), Gold, 10, 2);
        pnTrack = UiKit.Panel(new Color(0.16f, 0.11f, 0.08f, 1f), WoodDark, 5, 2);
        pnBarGreen = UiKit.Panel(LeafLight, LeafLight, 5, 1);
        pnBarAmber = UiKit.Panel(Gold, Gold, 5, 1);
        pnBarRed = UiKit.Panel(Red, Red, 5, 1);
        pnBadge = UiKit.Panel(Gold, WoodDark, 12, 3);
        pnGood = UiKit.Panel(Leaf, LeafDark, 12, 3);
        pnBad = UiKit.Panel(Red, RedDark, 12, 3);

        btnGreen = UiKit.Button(Leaf, LeafLight, LeafDark, LeafDark, Cream, UiKit.TitleFont, 16);
        btnBrown = UiKit.Button(WoodMid, new Color32(172, 118, 66, 255), WoodDark, WoodDark, Cream, UiKit.TitleFont, 16);

        Color border = WoodDark;
        ansNormal = UiKit.Button(Cream, new Color32(255, 236, 170, 255), new Color32(240, 214, 140, 255), border, Ink, UiKit.BodyFont, 12);
        ansSelected = UiKit.Button(new Color32(72, 102, 192, 255), new Color32(72, 102, 192, 255), new Color32(72, 102, 192, 255), new Color32(36, 56, 128, 255), Color.white, UiKit.BodyFont, 12);
        ansCorrect = UiKit.Button(new Color32(64, 168, 88, 255), new Color32(64, 168, 88, 255), new Color32(64, 168, 88, 255), LeafDark, Color.white, UiKit.BodyFont, 12);
        ansWrong = UiKit.Button(Red, Red, Red, RedDark, Color.white, UiKit.BodyFont, 12);
        ansDim = UiKit.Button(new Color32(222, 214, 194, 255), new Color32(222, 214, 194, 255), new Color32(222, 214, 194, 255), new Color32(160, 150, 130, 255), new Color32(128, 118, 102, 255), UiKit.BodyFont, 12);
        GUIStyle[] answers = { ansNormal, ansSelected, ansCorrect, ansWrong, ansDim };
        foreach (var a in answers) a.alignment = TextAnchor.MiddleLeft;
    }

    void Box(Rect r, GUIStyle st)
    {
        GUI.Box(r, GUIContent.none, st);
    }

    /// <summary>Teks satu lapis (bayangan tipis hanya untuk judul besar, supaya tidak terlihat dobel).</summary>
    void Txt(Rect r, string text, float size, Color color, float s, bool title = false, TextAnchor anchor = TextAnchor.MiddleCenter, bool shadow = false)
    {
        var st = title ? labelTitle : labelBody;
        st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s));
        st.alignment = anchor;
        if (shadow)
        {
            st.normal.textColor = new Color(0f, 0f, 0f, 0.45f);
            var sh = r;
            sh.x += 2f * s;
            sh.y += 2f * s;
            GUI.Label(sh, text, st);
        }
        st.normal.textColor = color;
        GUI.Label(r, text, st);
    }

    bool Btn(Rect r, string text, int size, GUIStyle st, float s)
    {
        st.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * s));
        return GUI.Button(r, text, st);
    }

    /// <summary>Teks font piksel di tengah kotak (px = ukuran satu kotak font pada skala 1).</summary>
    void PTxt(Rect r, string text, float px, Color c, float s, bool shadow = true)
    {
        PixelFont.Draw(r, text, px * s, c, white, shadow);
    }

    /// <summary>Tombol bergaya membulat dengan label font piksel (hover/pressed dari GUIStyle).</summary>
    bool PBtn(Rect r, string text, float px, GUIStyle st, float s)
    {
        bool hit = GUI.Button(r, GUIContent.none, st);
        PixelFont.Draw(r, text, px * s, Cream, white, true);
        return hit;
    }

    static Rect ScaleRect(Rect r, float k)
    {
        float w = r.width * k, h = r.height * k;
        return new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
    }

    static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t) - 1f;
        return 1f + 2.70158f * t * t * t + 1.70158f * t * t;
    }

    // ------------------------------------------------------------ UI: layar
    void DrawScore(float W, float H, float s)
    {
        PTxt(new Rect(0, H * 0.05f, W, 84f * s), Score.ToString(), 10f, Color.white, s);
    }

    void DrawHud(float s)
    {
        // nyawa = kesempatan kuis penyelamat
        float hh = 32f * s, hw = hh * 20f / 18f, gap = 6f * s, x0 = 20f * s, y0 = 16f * s;
        Box(new Rect(x0 - 10f * s, y0 - 7f * s, MaxLives * (hw + gap) + 14f * s, hh + 14f * s), pnSoft);
        for (int i = 0; i < MaxLives; i++)
        {
            GUI.color = i < QuizLives ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTexture(new Rect(x0 + i * (hw + gap), y0, hw, hh), heartTex);
        }
        GUI.color = Color.white;

        float y = y0 + hh + 14f * s;
        if (shieldTimer > 0f) DrawChip(ref y, s, shieldTex, "PERISAI", shieldTimer / ShieldTime, new Color(0.4f, 0.75f, 1f));
        if (slowTimer > 0f) DrawChip(ref y, s, hourTex, "SLOW MOTION", slowTimer / SlowTime, Gold);
    }

    void DrawChip(ref float y, float s, Texture2D icon, string label, float frac, Color barColor)
    {
        float x = 10f * s, w = 224f * s, h = 40f * s;
        Box(new Rect(x, y, w, h), pnSoft);
        float ih = 24f * s, iw = ih * icon.width / icon.height;
        GUI.DrawTexture(new Rect(x + 10f * s, y + 5f * s, iw, ih), icon);
        Txt(new Rect(x + iw + 20f * s, y + 2f * s, w - iw - 28f * s, 22f * s), label, 15, Cream, s, false, TextAnchor.MiddleLeft);
        float bx = x + iw + 20f * s, bw = w - iw - 30f * s;
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(bx, y + h - 11f * s, bw, 5f * s), white);
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(bx, y + h - 11f * s, bw * Mathf.Clamp01(frac), 5f * s), white);
        GUI.color = Color.white;
        y += h + 6f * s;
    }

    void DrawToast(float W, float H, float s)
    {
        if (Time.time >= toastUntil) return;
        float a = Mathf.Clamp01((toastUntil - Time.time) / 0.4f);
        float w = Mathf.Min(420f * s, W * 0.9f), h = 52f * s;
        var r = new Rect((W - w) * 0.5f, H * 0.27f, w, h);
        GUI.color = new Color(1f, 1f, 1f, a);
        Box(r, pnDark);
        Txt(r, toast, 24, Gold, s);
        GUI.color = Color.white;
    }

    void DrawMenu(float W, float H, float cx, float s)
    {
        float y = H * 0.06f;
        float pw = Mathf.Min(660f * s, W * 0.94f), ph = 128f * s;
        var plaque = new Rect(cx - pw * 0.5f, y, pw, ph);
        Box(plaque, pnWood);
        PTxt(new Rect(plaque.x + 24f * s, plaque.y, pw - 48f * s, ph), "QUIZY BIRD", 9.5f, Gold, s);
        y += ph + 14f * s;

        float tw = Mathf.Min(720f * s, W * 0.94f), th = 76f * s;
        Box(new Rect(cx - tw * 0.5f, y, tw, th), pnSoft);
        Txt(new Rect(cx - tw * 0.5f + 16f * s, y, tw - 32f * s, th),
            "Ayo bertualang bersama Cenderawasih\nHindari rintangan, uji pengetahuanmu dan raih skor tertinggi!", 20, Cream, s);
        y += th + 14f * s;

        float rw = 230f * s, rh = 44f * s;
        Box(new Rect(cx - rw * 0.5f, y, rw, rh), pnChip);
        Txt(new Rect(cx - rw * 0.5f, y, rw, rh), "REKOR   " + Best, 24, Gold, s);
        y += rh + 30f * s;

        float by = Mathf.Max(y, H * 0.55f);
        if (PBtn(new Rect(cx - 150f * s, by, 300f * s, 68f * s), "MULAI", 4f, btnGreen, s)) GoReady();
        if (PBtn(new Rect(cx - 150f * s, by + 82f * s, 300f * s, 56f * s), "KELUAR", 4f, btnBrown, s)) QuitGame();

        float fw = Mathf.Min(460f * s, W * 0.94f), fh = 36f * s;
        var fr = new Rect(cx - fw * 0.5f, H - fh - 20f * s, fw, fh);
        Box(fr, pnSoft);
        Txt(fr, "SPASI / Klik = terbang    |    M = bisu", 18, Cream, s);
    }

    void DrawReady(float W, float H, float cx, float s)
    {
        DrawScore(W, H, s);
        DrawHud(s);
        PTxt(new Rect(0, H * 0.20f, W, 80f * s), "SIAP?", 8f, Gold, s);

        float pw = Mathf.Min(800f * s, W * 0.94f), ph = 136f * s, y = H * 0.60f;
        Box(new Rect(cx - pw * 0.5f, y, pw, ph), pnSoft);
        Txt(new Rect(cx - pw * 0.5f, y + 8f * s, pw, 46f * s), "Tekan SPASI atau Klik untuk terbang", 28, Cream, s);
        Txt(new Rect(cx - pw * 0.5f, y + 54f * s, pw, 32f * s), "Awas burung penghalang: Enggang, Kawung, dan Bondol!", 20, new Color(1f, 0.78f, 0.55f), s);
        Txt(new Rect(cx - pw * 0.5f, y + 90f * s, pw, 32f * s), "Lewati Gerbang Kuis dan jawab benar untuk dapat perisai, slow motion, atau nyawa.", 18, Cream, s);
    }

    void DrawResume(float W, float H, float cx, float s)
    {
        DrawScore(W, H, s);
        DrawHud(s);
        float pw = Mathf.Min(520f * s, W * 0.92f), ph = 120f * s;
        var r = new Rect(cx - pw * 0.5f, H * 0.26f, pw, ph);
        Box(r, pnDark);
        PTxt(new Rect(r.x, r.y + 10f * s, pw, 56f * s), "BENAR!", 6f, LeafLight, s);
        Txt(new Rect(r.x, r.y + 72f * s, pw, 36f * s), "Bersiap terbang lagi...", 22, Cream, s);
    }

    // ------------------------------------------------------------ halaman kuis
    // Tata letak (satuan desain, panel 760 x 704), semua dihitung berurutan agar tidak ada yang bertumpuk:
    //   14-110  judul + subjudul        124-160 topik | tingkat | timer     172-188 bilah waktu
    //   200-320 kartu pertanyaan        336-612 empat pilihan (60 + jarak 12)
    //   624-660 pesan hasil / aturan    664-690 petunjuk (satu baris)
    void DrawQuiz(float W, float H, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, W, H), white);
        GUI.color = Color.white;
        DrawHud(s);

        float qs = Mathf.Min(s, W * 0.96f / 760f, H * 0.98f / 704f);
        float pw = 760f * qs, ph = 704f * qs;
        float px = (W - pw) * 0.5f, py = (H - ph) * 0.5f;
        float inX = px + 24f * qs, inW = pw - 48f * qs;      // area isi (padding 24 di kiri dan kanan)

        // animasi muncul (pop-in)
        float pop = Mathf.Clamp01((Time.time - quizShownAt) / 0.22f);
        float k = Mathf.Lerp(0.9f, 1f, 1f - (1f - pop) * (1f - pop));
        GUIUtility.ScaleAroundPivot(new Vector2(k, k), new Vector2(W * 0.5f, H * 0.5f));

        Box(new Rect(px, py, pw, ph), pnWood);

        // judul (tengah atas panel) dan subjudul tepat di bawahnya
        Box(new Rect(px + 14f * qs, py + 14f * qs, pw - 28f * qs, 96f * qs), pnLeaf);
        PTxt(new Rect(inX, py + 22f * qs, inW, 46f * qs), "KUIS TIME", 5f, Cream, qs);
        Txt(new Rect(inX, py + 72f * qs, inW, 30f * qs),
            quizKind == QuizKind.Gate ? "GERBANG KUIS - jawab benar untuk power-up" : "KUIS PENYELAMAT - jawab benar untuk lanjut bermain", 18, Cream, qs);

        // topik | tingkat | timer: tiga kotak sama tinggi, sejajar, jarak 12
        float iy = py + 124f * qs, ih = 36f * qs;
        var topicR = new Rect(inX, iy, 300f * qs, ih);
        var levelR = new Rect(topicR.xMax + 12f * qs, iy, 190f * qs, ih);
        var timerR = new Rect(levelR.xMax + 12f * qs, iy, inX + inW - (levelR.xMax + 12f * qs), ih);
        Box(topicR, pnChip);
        Txt(topicR, "Topik: " + quiz.Topic, 17, Cream, qs);
        Box(levelR, pnChip);
        Txt(levelR, "Tingkat: " + QuizBank.LevelName(quiz.Level), 17, Gold, qs);

        float left = Mathf.Max(0f, quizTimer);
        bool urgent = State == GameState.Quiz && left <= 5f;
        Color tc = left <= 5f ? Red : (left <= 8f ? Gold : Cream);
        Box(timerR, pnChip);
        PTxt(timerR, Mathf.CeilToInt(left) + " DTK", urgent && Mathf.Sin(Time.time * 8f) > 0.3f ? 3.5f : 3f, tc, qs);

        // bilah waktu (hijau -> kuning -> merah)
        float by = py + 172f * qs, bh = 16f * qs;
        Box(new Rect(inX, by, inW, bh), pnTrack);
        float frac = Mathf.Clamp01(quizTimer / QuizTime);
        if (frac > 0.01f)
        {
            GUIStyle bar = left <= 5f ? pnBarRed : (left <= 8f ? pnBarAmber : pnBarGreen);
            Box(new Rect(inX, by, Mathf.Max(inW * frac, 20f), bh), bar);
        }

        // kartu pertanyaan: teks di tengah (horizontal dan vertikal), padding 20
        float qy = py + 200f * qs;
        var card = new Rect(inX, qy, inW, 120f * qs);
        Box(card, pnCream);
        float qsz = quiz.Question.Length > 95 ? 22f : (quiz.Question.Length > 70 ? 24f : 26f);
        Txt(new Rect(card.x + 20f * qs, card.y + 8f * qs, card.width - 40f * qs, card.height - 16f * qs), quiz.Question, qsz, Ink, qs);

        // pilihan jawaban A - D: tinggi, font, padding, dan jarak sama
        string[] letters = { "A", "B", "C", "D" };
        float ay0 = py + 336f * qs, ah = 60f * qs, gap = 12f * qs;
        for (int i = 0; i < 4; i++)
        {
            bool isCorrect = i == correctIdx, isChosen = i == selectedIdx;
            GUIStyle st = ansNormal;
            float dx = 0f, grow = 0f;
            if (State == GameState.QuizFeedback)
            {
                float ft = feedbackT;
                if (ft < 0.3f) st = isChosen ? ansSelected : ansNormal;        // sesaat: tombol terpilih
                else if (isCorrect) st = ansCorrect;
                else if (isChosen) st = ansWrong;
                else st = ansDim;

                if (ft >= 0.3f && isChosen && !isCorrect && ft < 0.75f)        // jawaban salah bergetar
                    dx = Mathf.Sin((ft - 0.3f) * 60f) * 8f * qs * (1f - (ft - 0.3f) / 0.45f);
                if (ft >= 0.3f && isCorrect)                                   // jawaban benar berdenyut
                    grow = Mathf.Max(0f, Mathf.Sin((ft - 0.3f) * 9f)) * 3f * qs * Mathf.Clamp01(1.2f - (ft - 0.3f));
            }

            var r = new Rect(inX + dx - grow, ay0 + i * (ah + gap) - grow, inW + 2f * grow, ah + 2f * grow);
            st.fontSize = Mathf.Max(8, Mathf.RoundToInt(22f * qs));
            st.padding.left = Mathf.RoundToInt(68f * qs);
            st.padding.right = Mathf.RoundToInt(16f * qs);
            if (State == GameState.Quiz)
            {
                if (GUI.Button(r, shownOptions[i], st)) AnswerQuiz(i);
            }
            else GUI.Box(r, shownOptions[i], st);

            var badge = new Rect(r.x + 10f * qs, r.y + 9f * qs, 42f * qs, 42f * qs);
            Box(badge, pnBadge);
            PTxt(badge, letters[i], 3f, Ink, qs, false);
        }

        // pesan hasil (muncul dengan animasi) atau ringkasan aturan
        var fr = new Rect(inX, py + 624f * qs, inW, 36f * qs);
        if (State == GameState.Quiz)
        {
            Box(fr, pnSoft);
            Txt(fr, quizKind == QuizKind.Gate ? "Benar: dapat power-up   |   Salah: tidak ada hadiah" : "Benar: lanjut bermain   |   Salah: permainan berakhir", 17, Cream, qs);
        }
        else
        {
            float a = Mathf.Clamp01(feedbackT / 0.35f);
            var br = ScaleRect(fr, 0.6f + 0.4f * EaseOutBack(a));
            Box(br, answeredRight ? pnGood : pnBad);
            Txt(new Rect(br.x + 8f * qs, br.y, br.width - 16f * qs, br.height), feedbackMsg, feedbackMsg.Length > 46 ? 15 : 17, Color.white, qs);
        }

        // satu petunjuk saja di bagian bawah panel
        string hint = State == GameState.Quiz ? "Klik pilihan jawaban atau tekan tombol 1–4 (A–D)." : feedbackNote;
        Txt(new Rect(inX, py + 664f * qs, inW, 26f * qs), hint, 17, Cream, qs);

        GUI.matrix = Matrix4x4.identity;
    }

    string DescribeWeak()
    {
        if (QuizBank.RunTotal == 0) return "Belum ada kuis yang dijawab pada permainan ini. Lewati Gerbang Kuis untuk berlatih!";
        string w = QuizBank.RunWeakTopics(2);
        string all = QuizBank.WeakestTopic();
        string t = w.Length == 0 ? "Hebat! Semua jawabanmu benar pada permainan ini." : "Perlu dipelajari lagi: " + w + ".";
        if (all.Length > 0) t += "\nTopik tersulit sepanjang waktu: " + all + ".";
        return t;
    }

    // ------------------------------------------------------------ halaman Game Over
    // Urutan: GAME OVER, Score | Terbaik, Medali, (REKOR BARU!), Benar / Salah / Akurasi, Evaluasi belajar,
    // lalu tombol MAIN LAGI dan MENU tersusun vertikal di bawah. Semua rata tengah dan berada di dalam panel.
    void DrawGameOver(float W, float H, float cx, float s)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, W, H), white);
        GUI.color = Color.white;

        float contentH = 18f + 56f + 12f + 46f + 10f + 36f + 12f + (NewRecord ? 46f : 0f) + 126f + 12f + 156f + 16f + 54f + 12f + 46f + 22f;
        float gs = Mathf.Min(s, H * 0.98f / contentH, W * 0.96f / 560f);
        float pw = 560f * gs, ph = contentH * gs, px = cx - pw * 0.5f, py = (H - ph) * 0.5f;
        float inX = px + 24f * gs, inW = pw - 48f * gs;
        Box(new Rect(px, py, pw, ph), pnWood);

        float y = py + 18f * gs;
        PTxt(new Rect(inX, y, inW, 56f * gs), "GAME OVER", 7f, Gold, gs);
        y += 56f * gs + 12f * gs;

        // Score | Terbaik (dua kotak sama ukuran)
        float cw = (inW - 16f * gs) * 0.5f;
        var c1 = new Rect(inX, y, cw, 46f * gs);
        var c2 = new Rect(c1.xMax + 16f * gs, y, cw, 46f * gs);
        Box(c1, pnChip);
        Txt(c1, "Score: " + Score, 24, Cream, gs);
        Box(c2, pnChip);
        Txt(c2, "Terbaik: " + Best, 24, Cream, gs);
        y += 46f * gs + 10f * gs;

        // Medali
        var mr = new Rect(cx - 150f * gs, y, 300f * gs, 36f * gs);
        Box(mr, pnChip);
        Txt(mr, "Medali: " + Medal(Score), 22, Gold, gs);
        y += 36f * gs + 12f * gs;

        if (NewRecord)
        {
            PTxt(new Rect(inX, y, inW, 46f * gs), "REKOR BARU!", 3.5f, Gold, gs);
            y += 46f * gs;
        }

        // statistik kuis: Benar, Salah, Akurasi (data aktual dari putaran ini)
        var sr = new Rect(inX, y, inW, 126f * gs);
        Box(sr, pnCream);
        string acc = QuizBank.RunTotal == 0 ? "-" : Mathf.RoundToInt(100f * QuizBank.RunCorrect / QuizBank.RunTotal) + "%";
        Txt(new Rect(sr.x, sr.y + 12f * gs, sr.width, 34f * gs), "Benar: " + QuizBank.RunCorrect, 22, Ink, gs);
        Txt(new Rect(sr.x, sr.y + 46f * gs, sr.width, 34f * gs), "Salah: " + QuizBank.RunWrong, 22, Ink, gs);
        Txt(new Rect(sr.x, sr.y + 80f * gs, sr.width, 34f * gs), "Akurasi: " + acc, 22, Ink, gs);
        y += 126f * gs + 12f * gs;

        // evaluasi belajar (+ jawaban benar soal terakhir bila berakhir karena kuis)
        var er = new Rect(inX, y, inW, 156f * gs);
        Box(er, pnCream);
        Txt(new Rect(er.x, er.y + 8f * gs, er.width, 26f * gs), "EVALUASI BELAJAR", 18, LeafDark, gs);
        string ev = DescribeWeak();
        if (endedByQuiz && correctText.Length > 0) ev += "\nJawaban benar soal terakhir: " + correctText;
        Txt(new Rect(er.x + 16f * gs, er.y + 36f * gs, er.width - 32f * gs, 112f * gs), ev, ev.Length > 190 ? 15 : 17, Ink, gs);
        y += 156f * gs + 16f * gs;

        // tombol vertikal di tengah
        if (PBtn(new Rect(cx - 150f * gs, y, 300f * gs, 54f * gs), "MAIN LAGI", 4f, btnGreen, gs)) GoReady();
        y += 54f * gs + 12f * gs;
        if (PBtn(new Rect(cx - 110f * gs, y, 220f * gs, 46f * gs), "MENU", 4f, btnBrown, gs)) ShowMenu();
    }

    void OnGUI()
    {
        EnsureStyles();
        float W = Screen.width, H = Screen.height, cx = W * 0.5f;
        float s = Mathf.Min(H / 720f, W / 1000f);
        GUI.color = Color.white;

        switch (State)
        {
            case GameState.Menu:
                DrawMenu(W, H, cx, s);
                break;

            case GameState.Ready:
                DrawReady(W, H, cx, s);
                break;

            case GameState.Playing:
                if (slowTimer > 0f)
                {
                    GUI.color = new Color(0.4f, 0.6f, 1f, 0.12f);     // sedikit nuansa biru saat slow motion
                    GUI.DrawTexture(new Rect(0, 0, W, H), white);
                    GUI.color = Color.white;
                }
                DrawScore(W, H, s);
                DrawHud(s);
                DrawToast(W, H, s);
                break;

            case GameState.Resume:
                DrawResume(W, H, cx, s);
                break;

            case GameState.Quiz:
            case GameState.QuizFeedback:
                DrawScore(W, H, s);
                DrawQuiz(W, H, s);
                break;

            case GameState.GameOver:
                DrawGameOver(W, H, cx, s);
                break;
        }

        // kilatan putih saat menabrak
        float f = 1f - (Time.time - flashStart) / 0.25f;
        if (f > 0f)
        {
            GUI.color = new Color(1f, 1f, 1f, f * 0.8f);
            GUI.DrawTexture(new Rect(0, 0, W, H), white);
            GUI.color = Color.white;
        }
    }
}
