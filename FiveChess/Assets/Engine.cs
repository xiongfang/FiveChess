using ICSharpCode.SharpZipLib.GZip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Engine : MonoBehaviour {

    public static int start_fitness_score = 10;

    public UIChessboard board;

    //是否启动的时候自动加载上次运算的AI基因组
    public bool LoadWeights;
    //遗传算法
    UGenAlg Gen;

    List<Map> MapList = new List<Map>();

    public bool showFisrtGame;  //显示第一对AI对弈

    public int step_count_per_gen = 10;
    //自动保存的时间(秒)
    //public float auto_save_time = 30;

    //神经网络个数
    public int NetSize = 1000;

    //double _auto_save_timer;

    double _learn_timer;

    bool _inited;
    bool _started;
    bool _learn;
    Map _gameMap;
    int _stepCount;

    public Button btnStartLearn;
    public Button btnStartGame;
    public Button btnPause;

    public Text textTime;
    public Text textTuns;

    public static byte[] Compress(byte[] inputBytes)
    {
        MemoryStream ms = new MemoryStream();
        GZipOutputStream gzip = new GZipOutputStream(ms);
        gzip.Write(inputBytes, 0, inputBytes.Length);
        gzip.Close();
        return ms.ToArray();
    }

    /// <summary>
    /// 解压缩字节数组
    /// </summary>
    /// <param name="str"></param>
    public static byte[] Decompress(byte[] inputBytes)
    {
        GZipInputStream gzi = new GZipInputStream(new MemoryStream(inputBytes));

        MemoryStream re = new MemoryStream();
        int count = 0;
        byte[] data = new byte[4096];
        while ((count = gzi.Read(data, 0, data.Length)) != 0)
        {
            re.Write(data, 0, count);
        }
        return re.ToArray();
    }


	// Use this for initialization
	void Start () {
        btnStartGame.onClick.AddListener(StartGame);
        btnStartLearn.onClick.AddListener(StartLearn);
        btnPause.onClick.AddListener(Stop);
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
	}


    void StartLearn()
    {
        if (LoadWeights)
        {
            try
            {
                LoadGenFromFile();
            }
            catch (Exception E)
            {
                Debug.Log("Load Gen Failed " + E.Message);
                LoadWeights = false;
            }
        }

        if (!LoadWeights)
        {
            UGenAlg.ConfigData Config = new UGenAlg.ConfigData();
            Config.PopSize = NetSize;
            Config.NumWeights = new UBotAIController().Net.GetWeightCount();
            Gen = new UGenAlg();
            Gen.Init(Config);
        }

        //更新适应性分数代理
        Gen.onUpdateFitnessScores = UpdateFitnessScores;


        MapList = new List<Map>();

        //创建其他棋盘
        for (int i = 0; i < Gen.Config.PopSize / 2; i++)
        {
            Map map = new Map();

            map.whitePlayer = new Gamer();
            map.whitePlayer.map = map;
            map.whitePlayer.camp = Camp.White;
            map.blackPlayer = new Gamer();
            map.blackPlayer.map = map;
            map.blackPlayer.camp = Camp.Black;
            map.whitePlayer.controller = new UBotAIController();
            map.blackPlayer.controller = new UBotAIController();

            MapList.Add(map);
            if (i == 0 && showFisrtGame)
            {
                board.InitMap(map);
            }
        }

        //将加载的适应性分数保存
        if (LoadWeights)
            PutFitnessScores();

        _learn_timer = 0;
        _stepCount = 0;

        _started = true;
        _inited = true;
        _learn = true;
    }


    void StartGame()
    {
        Map map = new Map();

        map.whitePlayer = new Gamer();
        map.whitePlayer.map = map;
        map.whitePlayer.camp = Camp.White;
        map.blackPlayer = new Gamer();
        map.blackPlayer.map = map;
        map.blackPlayer.camp = Camp.Black;
        map.whitePlayer.controller = new GameLocalPlayerController();

        UBotAIController BotAI = new UBotAIController();
        map.blackPlayer.controller = BotAI;

        //设置AI
        BotAI.Net.PutWeights(new List<double>(LoadBestWeightsFromFileForUse()[0]));

        board.InitMap(map);
        _gameMap = map;


        _started = true;
        _learn = false;
        _inited = true;
        
    }

    void Stop()
    {
        if(_inited)
            _started = !_started;

        if(_learn)
        {
            SaveToFile();
        }
    }


    void LoadGenFromFile()
    {
        string FileName = Path.Combine(Application.streamingAssetsPath, "Gen.txt");
        Debug.Log("加载基因:" + FileName);

        string jsonData = System.Text.Encoding.UTF8.GetString(( File.ReadAllBytes(FileName)));
        Gen = JsonUtility.FromJson<UGenAlg>(jsonData);
    }

    /// <summary>
    /// 加载保存的AI权重， 公有的给外部使用
    /// </summary>
    /// <returns></returns>
    public static List<double[]> LoadBestWeightsFromFileForUse()
    {
        string FileName = Path.Combine(Application.streamingAssetsPath, "Weights.bin");
        Debug.Log(FileName);

        byte[] data = File.ReadAllBytes(FileName);

        MemoryStream MS = new MemoryStream(data);
        BinaryReader BR = new BinaryReader(MS);

        int Count = BR.ReadInt32();
        int Length = BR.ReadInt32();

        List<double[]> WeightList = new List<double[]>();

        for (int i = 0; i < Count; i++)
        {
            WeightList.Add(new double[Length]);
            for (int j = 0; j < Length; j++)
            {
                double v = BR.ReadDouble();
                WeightList[i][j] = v;
            }
        }

        return WeightList;
    }

    void SaveToFile()
    {
        //保存基因
        {
            string FileName = Path.Combine(Application.streamingAssetsPath, "Gen.txt");
            Debug.Log(System.DateTime.Now.ToString() + FileName);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(UnityEngine.JsonUtility.ToJson(Gen));

            //保存当前优秀的AI
            System.IO.File.WriteAllBytes(FileName,( bytes));
        }

        //保存最好的AI的权重
        {
            string FileName = Path.Combine(Application.streamingAssetsPath, "Weights.bin");
            Debug.Log(System.DateTime.Now.ToString() + FileName);

            int Count = 5; //5个AI差不多了
            List<double[]> WeightList = GetBestWeights(Count);

            MemoryStream MS = new MemoryStream();
            BinaryWriter BW = new BinaryWriter(MS);

            //基因数量
            BW.Write((int)WeightList.Count);
            //权重长度
            BW.Write(WeightList[0].Length);

            for (int i = 0; i < WeightList.Count; i++)
            {
                for (int j = 0; j < WeightList[i].Length; j++)
                {
                    BW.Write(WeightList[i][j]);
                }
            }

            //保存当前优秀的AI
            System.IO.File.WriteAllBytes(FileName, MS.ToArray());
        }
    }
    //赋值初始神经网络权重
    void PutWeightsToNet(List<double[]> weights)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            int index = i / 2;
            int red = i % 2;
            if (red == 0)
                (MapList[index].whitePlayer.controller as UBotAIController).Net.PutWeights(new List<double>(weights[i]));
            else
                (MapList[index].blackPlayer.controller as UBotAIController).Net.PutWeights(new List<double>(weights[i]));
        }
    }

    List<double[]> GetBestWeights(int count)
    {
        //先更新积分，否则排序出问题
        UpdateFitnessScores();
        //克隆出新的副本列表来排序，否则出问题
        List<UGenome> CloneGenomeList = new List<UGenome>();
        CloneGenomeList.AddRange(Gen.Genomes);
        CloneGenomeList.Sort();

        List<double[]> Gens = new List<double[]>();
        for (int i = 0; i < count; i++)
        {
            Gens.Add(Gen.Genomes[i].Weights);
        }
        return Gens;
    }

    /// <summary>
    /// 将AI控制器的适应性分数更新到基因里面
    /// </summary>
    void UpdateFitnessScores()
    {
        double TotleFitness = 0;

        for (int i = 0; i < Gen.Genomes.Length; i++)
        {
            int index = i / 2;
            int red = i % 2;
            if (index >= MapList.Count)
                Debug.Log(index + " " + MapList.Count);

            if (red == 0)
                Gen.Genomes[i].Fidness = (MapList[index].whitePlayer.controller as UBotAIController).Fitness;
            else
                Gen.Genomes[i].Fidness = (MapList[index].blackPlayer.controller as UBotAIController).Fitness;

            TotleFitness += Gen.Genomes[i].Fidness;
        }

        Gen.TotleFintnessScore = TotleFitness;
    }

    /// <summary>
    /// 将基因的适应性分数更新到AI控制器里面
    /// </summary>
    void PutFitnessScores()
    {
        for (int i = 0; i < Gen.Genomes.Length; i++)
        {
            int index = i / 2;
            int red = i % 2;

            if (red == 0)
                (MapList[index].whitePlayer.controller as UBotAIController).Fitness = Gen.Genomes[i].Fidness;
            else
                (MapList[index].blackPlayer.controller as UBotAIController).Fitness = Gen.Genomes[i].Fidness;
        }
    }

    /// <summary>
    /// 重置所有控制器的适应性分数
    /// </summary>
    void ResetFitnessScores()
    {
        for (int i = 0; i < Gen.Genomes.Length; i++)
        {
            int index = i / 2;
            int red = i % 2;

            if (red == 0)
                (MapList[index].whitePlayer.controller as UBotAIController).Fitness = start_fitness_score;
            else
                (MapList[index].blackPlayer.controller as UBotAIController).Fitness = start_fitness_score;
        }
    }

	// Update is called once per frame
	void Update () {
        if (!_started)
            return;

        if(!_learn)
        {
            _gameMap.Update(Time.deltaTime);
            return;
        }


        try
        {
            //_auto_save_timer += Time.deltaTime;
            //if (_auto_save_timer >= auto_save_time)
            //{
            //    _auto_save_timer = 0.0;
            //    SaveToFile();
            //}

            _learn_timer += Time.deltaTime;

            //下棋，计算适应性分数
            {
                //重置适应性分数
                ResetFitnessScores();

                //下完一步(每边一步)
                if (step_count_per_gen>0)
                {
                    //for (int step = 0; step < step_count_per_gen; step++)
                    {
                        for (int i = 0; i < MapList.Count; i++)
                        {
                            MapList[i].Update(Time.deltaTime);
                            if (MapList[i].gameOver)
                            {
                                MapList[i].Restart();
                            }
                        }
                    }
                    _stepCount++;

                    if(_stepCount>=step_count_per_gen)
                    {
                        _stepCount = 0;
                        //迭代一次
                        Gen.Epoch();

                        //将基因更新给神经网络
                        PutWeightsToNet(Gen.GetWeights());
                    }
                }
                else
                {
                    bool gameOver = true;
                    int overCount = 0;
                    //下完所有的棋子
                    for (int i = 0; i < MapList.Count; i++)
                    {
                        if (!MapList[i].gameOver)
                            MapList[i].Update(Time.deltaTime);
                        else
                            overCount++;

                        if(!MapList[i].gameOver)
                        {
                            gameOver = false;
                        }
                    }

                    if (gameOver || (float)overCount / MapList.Count>2.0f/3.0f)
                    {
                        //迭代一次
                        Gen.Epoch();

                        //将基因更新给神经网络
                        PutWeightsToNet(Gen.GetWeights());

                        for (int i = 0; i < MapList.Count; i++)
                        {
                            if (MapList[i].gameOver)
                            {
                                MapList[i].Restart();
                            }
                        }
                    }

                }
            }


            //刷新界面
            textTuns.text = string.Format("{0}", Gen.Generation.ToString());
            textTime.text = string.Format("{0}:{1}:{2}", (int)(_learn_timer / 3600), ((int)_learn_timer % 3600) / 60, (int)_learn_timer % 60);

        }
        catch (Exception E)
        {
            Debug.LogError(E.Message);
            Debug.LogError(E.StackTrace);
            //OnClickStop(null);
        }
	}
}
