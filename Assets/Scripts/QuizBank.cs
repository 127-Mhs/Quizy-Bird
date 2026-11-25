using System.Collections.Generic;
using UnityEngine;

/// <summary>Satu soal pilihan ganda. Jawaban benar selalu ada di Options[0] pada data (diacak saat ditampilkan).</summary>
public class QuizQuestion
{
    public string Topic;        // topik (lihat QuizBank.Topics)
    public int Level;           // 1 = mudah, 2 = sedang, 3 = sulit
    public string Question;
    public string[] Options;    // Options[0] = jawaban benar
    public QuizQuestion(string topic, int level, string q, params string[] o) { Topic = topic; Level = level; Question = q; Options = o; }
}

/// <summary>Bank soal Pancasila dan Kewarganegaraan (PKn). Tambah soal baru di daftar All (jawaban benar ditulis PERTAMA, lalu topik dan level 1-3).
/// Soal dipilih secara adaptif: menyesuaikan kemampuan pemain dan topik yang paling sering salah dijawab.</summary>
public static class QuizBank
{
    public static readonly string[] Topics = { "Pancasila", "Simbol Negara", "UUD 1945", "Lembaga Negara", "Pemilu & Demokrasi", "Hak & Kewajiban", "Wawasan Kebangsaan", "Budaya Nusantara" };

    public static readonly QuizQuestion[] All =
    {
        // ---- Pancasila: bunyi, lambang, dan pengamalan sila ----
        new QuizQuestion("Pancasila", 1, "Sila pertama Pancasila berbunyi...", "Ketuhanan Yang Maha Esa", "Kemanusiaan yang adil dan beradab", "Persatuan Indonesia", "Keadilan sosial bagi seluruh rakyat Indonesia"),
        new QuizQuestion("Pancasila", 1, "Sila ketiga Pancasila berbunyi...", "Persatuan Indonesia", "Ketuhanan Yang Maha Esa", "Kemanusiaan yang adil dan beradab", "Keadilan sosial bagi seluruh rakyat Indonesia"),
        new QuizQuestion("Pancasila", 2, "Sila keempat Pancasila adalah sila tentang...", "Kerakyatan", "Persatuan", "Kemanusiaan", "Keadilan sosial"),
        new QuizQuestion("Pancasila", 1, "Sila kelima Pancasila berbunyi...", "Keadilan sosial bagi seluruh rakyat Indonesia", "Persatuan Indonesia", "Kemanusiaan yang adil dan beradab", "Ketuhanan Yang Maha Esa"),
        new QuizQuestion("Pancasila", 2, "Lambang sila pertama Pancasila adalah...", "Bintang", "Rantai", "Pohon beringin", "Kepala banteng"),
        new QuizQuestion("Pancasila", 2, "Lambang sila kedua Pancasila adalah...", "Rantai", "Bintang", "Kepala banteng", "Padi dan kapas"),
        new QuizQuestion("Pancasila", 2, "Lambang sila keempat Pancasila adalah...", "Kepala banteng", "Pohon beringin", "Rantai", "Bintang"),
        new QuizQuestion("Pancasila", 2, "Padi dan kapas adalah lambang sila ke...", "Kelima", "Keempat", "Ketiga", "Kedua"),
        new QuizQuestion("Pancasila", 1, "Pancasila terdiri dari berapa sila?", "5", "4", "6", "7"),
        new QuizQuestion("Pancasila", 2, "Bunyi Pancasila yang resmi terdapat dalam Pembukaan UUD 1945 pada...", "Alinea ke-4", "Alinea ke-1", "Alinea ke-2", "Alinea ke-3"),
        new QuizQuestion("Pancasila", 1, "Contoh pengamalan sila pertama Pancasila adalah...", "Saling menghormati pemeluk agama lain", "Memaksa teman mengikuti agama kita", "Mengejek ibadah orang lain", "Mementingkan diri sendiri"),
        new QuizQuestion("Pancasila", 2, "Mengambil keputusan bersama lewat musyawarah adalah pengamalan sila ke...", "Keempat", "Pertama", "Kedua", "Kelima"),
        new QuizQuestion("Pancasila", 3, "Tidak berlaku semena-mena dan menghargai martabat orang lain adalah pengamalan sila ke...", "Kedua", "Pertama", "Keempat", "Kelima"),
        new QuizQuestion("Pancasila", 3, "Mengutamakan kepentingan bangsa di atas kepentingan golongan adalah pengamalan sila ke...", "Ketiga", "Pertama", "Kedua", "Keempat"),
        new QuizQuestion("Pancasila", 3, "Bersikap adil, menghormati hak orang lain, dan suka bekerja keras adalah pengamalan sila ke...", "Kelima", "Pertama", "Ketiga", "Keempat"),

        // ---- Simbol negara ----
        new QuizQuestion("Simbol Negara", 1, "Semboyan negara Indonesia adalah...", "Bhinneka Tunggal Ika", "Tut Wuri Handayani", "Jalesveva Jayamahe", "Rastra Sewakottama"),
        new QuizQuestion("Simbol Negara", 1, "Bhinneka Tunggal Ika berarti...", "Berbeda-beda tetapi tetap satu", "Bersatu kita teguh", "Satu nusa satu bangsa", "Gotong royong membangun negeri"),
        new QuizQuestion("Simbol Negara", 1, "Lambang negara Indonesia adalah...", "Garuda Pancasila", "Garuda Wisnu Kencana", "Burung Cendrawasih", "Rajawali Putih"),
        new QuizQuestion("Simbol Negara", 3, "Jumlah bulu pada setiap sayap Garuda Pancasila adalah...", "17", "8", "19", "45"),
        new QuizQuestion("Simbol Negara", 2, "Warna putih pada bendera Merah Putih melambangkan...", "Kesucian", "Keberanian", "Kemakmuran", "Kekuasaan"),
        new QuizQuestion("Simbol Negara", 2, "Pencipta lagu kebangsaan Indonesia Raya adalah...", "W.R. Supratman", "Ismail Marzuki", "C. Simanjuntak", "H. Mutahar"),

        // ---- UUD 1945 dan ketatanegaraan ----
        new QuizQuestion("UUD 1945", 3, "Bahasa negara Indonesia diatur dalam UUD 1945 Pasal...", "36", "28", "31", "33"),
        new QuizQuestion("UUD 1945", 2, "Pasal 1 ayat (3) UUD 1945 menyatakan bahwa Indonesia adalah negara...", "Hukum", "Kekuasaan", "Agama", "Federal"),
        new QuizQuestion("UUD 1945", 1, "Bentuk negara Indonesia adalah...", "Kesatuan (NKRI)", "Federal", "Persemakmuran", "Monarki"),
        new QuizQuestion("UUD 1945", 2, "Sistem pemerintahan Indonesia adalah...", "Presidensial", "Parlementer", "Monarki absolut", "Komunis"),
        new QuizQuestion("Lembaga Negara", 2, "Lembaga negara yang berwenang mengubah dan menetapkan UUD adalah...", "MPR", "DPR", "MA", "BPK"),
        new QuizQuestion("Lembaga Negara", 2, "Lembaga yang berwenang menguji undang-undang terhadap UUD adalah...", "Mahkamah Konstitusi", "Mahkamah Agung", "Komisi Yudisial", "BPK"),
        new QuizQuestion("Lembaga Negara", 1, "Lembaga yang bersama Presiden membahas dan membentuk undang-undang adalah...", "DPR", "MPR", "BPK", "MA"),
        new QuizQuestion("Lembaga Negara", 2, "Lembaga negara yang memeriksa pengelolaan keuangan negara adalah...", "BPK", "KPK", "OJK", "Bank Indonesia"),
        new QuizQuestion("UUD 1945", 2, "Salah satu tujuan negara dalam Pembukaan UUD 1945 adalah...", "Mencerdaskan kehidupan bangsa", "Menguasai wilayah negara lain", "Membentuk negara federal", "Mengutamakan golongan tertentu"),
        new QuizQuestion("Lembaga Negara", 1, "Kepala pemerintahan daerah provinsi disebut...", "Gubernur", "Bupati", "Wali kota", "Camat"),

        // ---- Pemilu dan demokrasi ----
        new QuizQuestion("Pemilu & Demokrasi", 1, "Asas pemilu LUBER meliputi Langsung, Umum, Bebas, dan...", "Rahasia", "Rakyat", "Resmi", "Rapi"),
        new QuizQuestion("Pemilu & Demokrasi", 1, "Warga negara Indonesia berhak memilih dalam pemilu jika berusia minimal...", "17 tahun", "15 tahun", "21 tahun", "25 tahun"),
        new QuizQuestion("Pemilu & Demokrasi", 1, "Presiden dan Wakil Presiden Indonesia dipilih oleh...", "Rakyat secara langsung", "DPR", "MPR", "Mahkamah Agung"),
        new QuizQuestion("Pemilu & Demokrasi", 2, "Masa jabatan Presiden Republik Indonesia adalah...", "5 tahun", "4 tahun", "6 tahun", "7 tahun"),
        new QuizQuestion("Pemilu & Demokrasi", 1, "Jika berbeda pendapat dalam rapat kelas, sikap terbaik adalah...", "Menyelesaikannya dengan musyawarah", "Memaksakan kehendak", "Meninggalkan rapat", "Mengancam teman"),

        // ---- Hak, kewajiban, dan sikap warga negara ----
        new QuizQuestion("Hak & Kewajiban", 1, "Upaya bela negara adalah hak dan kewajiban...", "Setiap warga negara", "Hanya TNI", "Hanya Polri", "Hanya pejabat negara"),
        new QuizQuestion("Hak & Kewajiban", 3, "Hak warga negara untuk mendapat pendidikan tercantum dalam UUD 1945 Pasal...", "31", "29", "33", "34"),
        new QuizQuestion("Hak & Kewajiban", 3, "Kebebasan memeluk agama dan beribadah dijamin dalam UUD 1945 Pasal...", "29", "31", "33", "30"),
        new QuizQuestion("Hak & Kewajiban", 3, "Jaminan hak asasi manusia (HAM) dalam UUD 1945 terdapat pada Pasal...", "28A - 28J", "1 - 5", "30 - 31", "33 - 34"),
        new QuizQuestion("Hak & Kewajiban", 1, "Contoh kewajiban warga negara adalah...", "Menaati hukum dan peraturan yang berlaku", "Mendapat pendidikan", "Memeluk agama", "Mendapat pekerjaan yang layak"),
        new QuizQuestion("Hak & Kewajiban", 1, "Contoh hak warga negara adalah...", "Mendapat pekerjaan dan penghidupan yang layak", "Membayar pajak", "Menaati hukum", "Menjaga fasilitas umum"),
        new QuizQuestion("Hak & Kewajiban", 2, "Pajak yang dibayarkan warga negara digunakan untuk...", "Pembangunan dan pelayanan publik", "Kepentingan pribadi pejabat", "Membiayai negara lain", "Disimpan tanpa digunakan"),
        new QuizQuestion("Wawasan Kebangsaan", 3, "Cara pandang bangsa Indonesia terhadap diri dan lingkungannya berdasarkan Pancasila dan UUD 1945 disebut...", "Wawasan Nusantara", "Ketahanan Nasional", "Otonomi Daerah", "Demokrasi Liberal"),
        new QuizQuestion("Wawasan Kebangsaan", 1, "Sikap yang sesuai dengan semangat Bhinneka Tunggal Ika adalah...", "Menghargai perbedaan suku, agama, dan budaya", "Memilih teman hanya dari suku sendiri", "Mengejek logat daerah lain", "Menganggap budaya sendiri paling benar"),

        // ---- Budaya Nusantara ----
        new QuizQuestion("Budaya Nusantara", 1, "Honai adalah rumah adat yang berasal dari...", "Papua", "Aceh", "Bali", "Sulawesi Selatan"),
        new QuizQuestion("Budaya Nusantara", 1, "Burung Cenderawasih adalah fauna khas daerah...", "Papua", "Sumatera", "Kalimantan", "Jawa"),
        new QuizQuestion("Budaya Nusantara", 1, "Kain tradisional Indonesia yang dibuat dengan teknik lilin (malam) disebut...", "Batik", "Songket", "Ulos", "Lurik"),
        new QuizQuestion("Budaya Nusantara", 2, "Rumah Gadang adalah rumah adat dari...", "Sumatera Barat", "Jawa Tengah", "Kalimantan Timur", "Nusa Tenggara Timur"),
        new QuizQuestion("Budaya Nusantara", 2, "Angklung adalah alat musik tradisional dari...", "Jawa Barat", "Sumatera Utara", "Papua", "Maluku"),
        new QuizQuestion("Budaya Nusantara", 2, "Tari Saman berasal dari daerah...", "Aceh", "Bali", "Jawa Timur", "Papua"),
        new QuizQuestion("Budaya Nusantara", 2, "Tari Kecak merupakan kesenian khas...", "Bali", "Aceh", "Maluku", "Kalimantan"),
        new QuizQuestion("Budaya Nusantara", 2, "Candi Borobudur terletak di provinsi...", "Jawa Tengah", "Jawa Barat", "Bali", "Sumatera Utara"),
        new QuizQuestion("Budaya Nusantara", 3, "Reog adalah kesenian tradisional dari...", "Ponorogo, Jawa Timur", "Pontianak, Kalimantan Barat", "Denpasar, Bali", "Makassar, Sulawesi Selatan"),
        new QuizQuestion("Budaya Nusantara", 3, "Alat musik tradisional Sasando berasal dari...", "Nusa Tenggara Timur", "Sumatera Barat", "Jawa Barat", "Papua"),
        new QuizQuestion("Budaya Nusantara", 3, "Wayang kulit merupakan kesenian tradisional yang banyak berkembang di pulau...", "Jawa", "Papua", "Sulawesi", "Kalimantan"),
    };

    static readonly HashSet<int> used = new HashSet<int>();
    static float skill = 1f;            // perkiraan kemampuan pemain pada putaran ini (1 - 3,4)

    // riwayat jawaban pada putaran (permainan) yang sedang berjalan
    static readonly int[] runWrongBy = new int[Topics.Length];
    public static int RunCorrect { get; private set; }
    public static int RunWrong { get; private set; }
    public static int RunTotal { get { return RunCorrect + RunWrong; } }

    public static string LevelName(int level)
    {
        return level <= 1 ? "Mudah" : (level == 2 ? "Sedang" : "Sulit");
    }

    public static int TopicIndex(string topic)
    {
        for (int i = 0; i < Topics.Length; i++) if (Topics[i] == topic) return i;
        return -1;
    }

    /// <summary>Dipanggil saat permainan baru dimulai: kesulitan kembali ke awal.</summary>
    public static void BeginRun()
    {
        skill = 1f;
        RunCorrect = 0;
        RunWrong = 0;
        System.Array.Clear(runWrongBy, 0, runWrongBy.Length);
    }

    static int RunWrongOf(string topic)
    {
        int t = TopicIndex(topic);
        return t < 0 ? 0 : runWrongBy[t];
    }

    // peluang salah pada suatu topik berdasarkan riwayat semua permainan (disimpan di PlayerPrefs)
    static float ErrorRate(string topic)
    {
        int t = TopicIndex(topic);
        if (t < 0) return 0f;
        int seen = PlayerPrefs.GetInt("QB.S." + t, 0), wrong = PlayerPrefs.GetInt("QB.W." + t, 0);
        return (wrong + 0.5f) / (seen + 1f);     // dihaluskan agar topik baru tidak ekstrem
    }

    /// <summary>
    /// Soal berikutnya. Bobot pemilihan = kecocokan tingkat kesulitan dengan kemampuan pemain
    /// x (1 + 3 x peluang salah pada topiknya). Soal tidak diulang sampai semua soal terpakai.
    /// </summary>
    public static QuizQuestion Next(int score)
    {
        if (used.Count >= All.Length) used.Clear();
        // tingkat soal = gabungan kemampuan pemain dan perkembangan skor (0-9: mudah, 10-19: sedang, 20+: sulit)
        float scoreLevel = Mathf.Clamp(1f + score / 10f, 1f, 3f);
        int target = Mathf.Clamp(Mathf.RoundToInt(0.5f * skill + 0.5f * scoreLevel), 1, 3);

        var weights = new float[All.Length];
        float total = 0f;
        for (int i = 0; i < All.Length; i++)
        {
            if (used.Contains(i)) continue;
            int d = Mathf.Abs(All[i].Level - target);
            float levelFit = d == 0 ? 1f : (d == 1 ? 0.35f : 0.1f);
            weights[i] = levelFit * (1f + 3f * ErrorRate(All[i].Topic)) * (1f + 1.5f * RunWrongOf(All[i].Topic));
            total += weights[i];
        }

        float roll = Random.value * total;
        int pick = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (weights[i] <= 0f) continue;
            pick = i;
            roll -= weights[i];
            if (roll <= 0f) break;
        }
        used.Add(pick);
        return All[pick];
    }

    /// <summary>Catat hasil jawaban: memperbarui kemampuan pemain dan statistik topik.</summary>
    public static void Record(QuizQuestion q, bool correct)
    {
        int t = TopicIndex(q.Topic);
        if (t >= 0)
        {
            PlayerPrefs.SetInt("QB.S." + t, PlayerPrefs.GetInt("QB.S." + t, 0) + 1);
            if (!correct) PlayerPrefs.SetInt("QB.W." + t, PlayerPrefs.GetInt("QB.W." + t, 0) + 1);
        }
        if (correct) RunCorrect++;
        else
        {
            RunWrong++;
            if (t >= 0) runWrongBy[t]++;
        }
        skill = correct ? Mathf.Min(3.4f, skill + 0.5f) : Mathf.Max(1f, skill - 0.7f);
    }

    /// <summary>Topik yang salah dijawab pada putaran ini, diurutkan dari yang paling sering salah.</summary>
    public static string RunWeakTopics(int max)
    {
        var idx = new List<int>();
        for (int t = 0; t < Topics.Length; t++) if (runWrongBy[t] > 0) idx.Add(t);
        idx.Sort((a, b) => runWrongBy[b].CompareTo(runWrongBy[a]));
        var names = new List<string>();
        for (int i = 0; i < idx.Count && i < max; i++) names.Add(Topics[idx[i]] + " (" + runWrongBy[idx[i]] + "x salah)");
        return string.Join(", ", names.ToArray());
    }

    /// <summary>Topik yang paling sering salah dijawab (minimal 2 kali muncul); kosong jika belum cukup data.</summary>
    public static string WeakestTopic()
    {
        int best = -1;
        float bestRate = 0f;
        for (int t = 0; t < Topics.Length; t++)
        {
            int seen = PlayerPrefs.GetInt("QB.S." + t, 0), wrong = PlayerPrefs.GetInt("QB.W." + t, 0);
            if (seen < 2 || wrong == 0) continue;
            float rate = wrong / (float)seen;
            if (rate > bestRate) { bestRate = rate; best = t; }
        }
        return best < 0 ? "" : Topics[best];
    }
}
