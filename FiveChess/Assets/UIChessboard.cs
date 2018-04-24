using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.GZip;




public class UIChessboard : MonoBehaviour {
    public UnityEngine.UI.Image chessPrefabWhite;
    public UnityEngine.UI.Image chessPrefabBlack;
    public Transform chessRoot;

    public float chessSpacing;

    Map _map;

    public void InitMap(Map map)
    {
        _map = map;
        _map.onDo = Do;
        _map.onUndo = UnDo;
        _map.onRestart = Restart;
    }

    Vector3 GetChessCoord(int x,int y)
    {
        return new Vector3((x - 8) * chessSpacing, (y - 8) * chessSpacing);
    }

    public void Do(Map.Cmd c)
    {
        if(c.camp == Camp.White)
        {
            c.chessObject = GameObject.Instantiate<GameObject>(chessPrefabWhite.gameObject, chessRoot);
        }
        else
        {
            c.chessObject = GameObject.Instantiate<GameObject>(chessPrefabBlack.gameObject, chessRoot);
        }
        RectTransform rc = c.chessObject.transform as RectTransform;
        rc.anchoredPosition = GetChessCoord(c.x, c.y);
    }

    public void UnDo(Map.Cmd c)
    {
        GameObject.Destroy(c.chessObject);
    }

    void Restart()
    {
        for(int i=chessRoot.childCount-1;i>=0;i--)
        {
            GameObject.Destroy(chessRoot.GetChild(i).gameObject);
        }
    }
}

public class Map
{
    public Camp currentCamp;


    public Gamer whitePlayer;
    public Gamer blackPlayer;

    public System.Action<Cmd> onDo;
    public System.Action<Cmd> onUndo;
    public System.Action onRestart;

    public bool gameOver;
    public Camp winner;

    public class Cmd
    {
        public Camp camp;
        public int x;
        public int y;

        public GameObject chessObject;
    }
    List<Cmd> cmdList = new List<Cmd>();

    public int[][] map;

    public Map()
    {
        Reset();
    }

    void Reset()
    {
        gameOver = false;

        map = new int[15][];
        for (int i = 0; i < map.Length; i++)
        {
            map[i] = new int[15];
        }

        whitePlayer = new Gamer();
        whitePlayer.map = this;
        whitePlayer.camp = Camp.White;
        blackPlayer = new Gamer();
        blackPlayer.map = this;
        blackPlayer.camp = Camp.Black;
        whitePlayer.controller = new UBotAIController();
        blackPlayer.controller = new UBotAIController();

        cmdList.Clear();
    }

    public void Do(Cmd c)
    {
        if (c.camp == Camp.White)
        {
            currentCamp = Camp.Black;
        }
        else
        {
            currentCamp = Camp.White;
        }
        map[c.x - 1][c.y - 1] = GetCampMapValue(c.camp);

        cmdList.Add(c);


        Judge(c);

        if(onDo!=null)
        {
            onDo(c);
        }
    }

    public static int GetCampMapValue(Camp c)
    {
        return  c == Camp.White ? 1 : -1;
    }

    bool Judge(Cmd c)
    {
        if(gameOver)
            return true;

        //横线
        {
            int count = 1;
            bool leftEnd = false;
            bool rightEnd = false;
           
            for(int i=1;i<15;i++)
            {
                if(!rightEnd && (c.x-1+i<15) && map[c.x-1+i][c.y-1] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    rightEnd = true;
                }
                if(!leftEnd && (c.x-1-i>=0) && map[c.x-1-i][c.y-1] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    leftEnd = true;
                }

                if(leftEnd && rightEnd)
                    break;
            }

            if(count>=5)
            {
                gameOver = true;
                winner = c.camp;
                return true;
            }
        }
        
        //竖线
        {
            int count = 1;
            bool leftEnd = false;
            bool rightEnd = false;
            for(int i=1;i<15;i++)
            {
                if(!rightEnd && (c.y-1+i<15) && map[c.x-1][c.y-1+i] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    rightEnd = true;
                }
                if(!leftEnd && (c.y-1-i>=0) && map[c.x-1][c.y-1-i] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    leftEnd = true;
                }

                if(leftEnd && rightEnd)
                    break;
            }

            if(count>=5)
            {
                gameOver = true;
                winner = c.camp;
                return true;
            }
        }
        //斜线
        {
            int count = 1;
            bool leftEnd = false;
            bool rightEnd = false;
            for(int i=1;i<15;i++)
            {
                if(!rightEnd && (c.x-1+i<15) && (c.y-1+i<15) && map[c.x-1+i][c.y-1+i] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    rightEnd = true;
                }
                if(!leftEnd && (c.x-1-i>=0) && (c.y-1-i>=0)  && map[c.x-1-i][c.y-1-i] == GetCampMapValue(c.camp))
                {
                    count++;
                }
                else
                {
                    leftEnd = true;
                }

                if(leftEnd && rightEnd)
                    break;
            }

            if(count>=5)
            {
                gameOver = true;
                winner = c.camp;
                return true;
            }
        }
            
        return false;
    }

    public void Update(float deltaTime)
    {
        if(!gameOver)
        {
            whitePlayer.Update(deltaTime);
            blackPlayer.Update(deltaTime);
        }
    }

    public void UnDo()
    {
        if (cmdList.Count > 0)
        {
            Cmd c = cmdList[cmdList.Count - 1];
            GameObject.Destroy(c.chessObject);
            map[c.x - 1][c.y - 1] = 0;
            cmdList.RemoveAt(cmdList.Count - 1);

            if (onUndo != null)
            {
                onUndo(c);
            }
        }
    }

    public void Restart()
    {
        Reset();
        if(onRestart!=null)
        {
            onRestart();
        }
    }
}


public enum Camp
{
    White,
    Black
}

public class Gamer
{
    Controller _controller;
    Camp _camp;
    public Map map;

    public Camp camp { get { return _camp; } set { _camp = value; } }

    public Controller controller
    {
        get
        {
            return _controller;
        }

        set
        {
            if(_controller!=null)
            {
                _controller.Dettach();
            }
            _controller = value;
            if(_controller!=null)
            {
                _controller.Attach(this);
            }
        }
    }

    public virtual void Update(float deltaTime) 
    {
        if (controller != null)
            controller.Update(deltaTime);
    }
}

public class Controller
{
    public virtual void Attach(Gamer g) { }

    public virtual void Dettach() { }

    public virtual void Update(float deltaTime) { }
}


public class AIController :Controller
{
    protected Gamer _target;
    float _waitTimer;
    public float ThinkingTime = 0.5f;
    public override void Attach(Gamer g)
    {
        base.Attach(g);
        _target = g;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);


        if (_target.map.currentCamp == _target.camp)
        {
            _waitTimer += deltaTime;
            if (_waitTimer >= ThinkingTime)
            {
                _waitTimer = 0.0f;

                Step();
            }

        }

    }

    //下一步棋
    protected virtual void Step()
    {
        //随机一点

        List<int> posList = new List<int>();
        for (int i = 0; i < _target.map.map.Length; i++)
        {
            for (int j = 0; j < _target.map.map[i].Length; j++)
            {
                if (_target.map.map[i][j] == 0)
                    posList.Add(i * 15 + j);
            }
        }

        if(posList.Count>0)
        {
            int pos = posList[UnityEngine.Random.Range(0, posList.Count)];
            int x = pos / 15 +1;
            int y = pos % 15+1;
            Map.Cmd cmd = new Map.Cmd();
            cmd.camp = _target.camp;
            cmd.x = x;
            cmd.y =y;
            _target.map.Do(cmd);
        }
        else
        {
            Debug.Log("无地可下");
        }
        
    }
}


public class UBotAIController : AIController
{
    public UNeuronNet_Controller Net;

    //这个AI的适应性分数(开始都是10分)
    public double Fitness;   

    public UBotAIController()
    {
        Fitness = Engine.start_fitness_score;

        Net = new UNeuronNet_Controller();
        UNeuronNet.ConfigData Config = new UNeuronNet.ConfigData();
        Config.NumInputs = 15*15+1;  //棋盘格子数+阵营
        Config.NumHiddenLayer = 2; //隐藏层
        Config.NumNeuronPerHiddenLayer = 32;    //每层神经元
        Config.NumOutputs = 1;          //1个输出
        Net.Init(Config);
    }

    protected override void Step()
    {
        Map.Cmd Cmd = Net.Update(_target.map);
        if (Cmd != null)
        {
            _target.map.Do(Cmd);

            ////增加适应性分数
            //if (Cmd.EatedHistory != null)
            //{
            //    double changed = 0;
            //    switch (Cmd.EatedHistory.chessType)
            //    {
            //        case EChessType.Bing:
            //            changed += 20;
            //            break;
            //        case EChessType.Ju:
            //            changed += 50;
            //            break;
            //        case EChessType.Ma:
            //            changed += 30;
            //            break;
            //        case EChessType.Pao:
            //            changed += 30;
            //            break;
            //        case EChessType.Shi:
            //            changed += 30;
            //            break;
            //        case EChessType.Xiang:
            //            changed += 30;
            //            break;
            //        case EChessType.Shuai:
            //            changed += 100;
            //            break;
            //        default:
            //            changed -= 1;
            //            break;
            //    }

                if(_target.map.gameOver)
                    Fitness += 10;

                Fitness = Mathf.Max((float)Fitness, 0);
            //}
        }

    }
}