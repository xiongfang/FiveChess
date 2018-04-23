using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIChessboard : MonoBehaviour {
    public UnityEngine.UI.Image chessPrefabWhite;
    public UnityEngine.UI.Image chessPrefabBlack;
    public Transform chessRoot;

    public float chessSpacing;

    public Camp currentCamp;
    Gamer whitePlayer;
    Gamer blackPlayer;

    public int[][] map;
    
    public class Cmd
    {
        public Camp camp;
        public int x;
        public int y;

        public GameObject chessObject;
    }
    List<Cmd> cmdList = new List<Cmd>();

	// Use this for initialization
	void Start () {

        map = new int[15][];
        for (int i = 0; i < map.Length;i++ )
        {
            map[i] = new int[15];
        }

        whitePlayer = new Gamer();
        whitePlayer.board = this;
        whitePlayer.camp = Camp.White;
        blackPlayer = new Gamer();
        blackPlayer.board = this;
        blackPlayer.camp = Camp.Black;
        whitePlayer.controller = new AIController();
        blackPlayer.controller = new AIController();
	}
	
	// Update is called once per frame
	void Update () {
        whitePlayer.Update(Time.deltaTime);
        blackPlayer.Update(Time.deltaTime);
	}


    Vector3 GetChessCoord(int x,int y)
    {
        return new Vector3((x - 8) * chessSpacing, (y - 8) * chessSpacing);
    }

    public void Do(Cmd c)
    {
        if(c.camp == Camp.White)
        {
            c.chessObject = GameObject.Instantiate<GameObject>(chessPrefabWhite.gameObject, chessRoot);
            currentCamp = Camp.Black;
        }
        else
        {
            c.chessObject = GameObject.Instantiate<GameObject>(chessPrefabBlack.gameObject, chessRoot);
            currentCamp = Camp.White;
        }
        RectTransform rc = c.chessObject.transform as RectTransform;
        rc.anchoredPosition = GetChessCoord(c.x, c.y);
        map[c.x - 1][ c.y - 1] = c.camp == Camp.White ? 1 : -1;
        
        cmdList.Add(c);
    }

    public void UnDo()
    {
        if (cmdList.Count>0)
        {
            Cmd c = cmdList[cmdList.Count - 1];
            GameObject.Destroy(c.chessObject);

            cmdList.RemoveAt(cmdList.Count - 1);
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
    public UIChessboard board;

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
    Gamer _target;
    float _waitTimer;

    public override void Attach(Gamer g)
    {
        base.Attach(g);
        _target = g;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        
        if(_target.board.currentCamp == _target.camp)
        {
            _waitTimer += deltaTime;
            if(_waitTimer>=0.5f)
            {
                _waitTimer = 0.0f;

                Step();
            }

        }

    }

    //下一步棋
    void Step()
    {
        //随机一点

        List<int> posList = new List<int>();
        for(int i=0;i<_target.board.map.Length;i++)
        {
            for (int j = 0; j < _target.board.map[i].Length;j++ )
            {
                if (_target.board.map[i][j] == 0)
                    posList.Add(i * 15 + j);
            }
        }

        if(posList.Count>0)
        {
            int pos = posList[UnityEngine.Random.Range(0, posList.Count)];
            int x = pos / 15 +1;
            int y = pos % 15+1;
            UIChessboard.Cmd cmd = new UIChessboard.Cmd();
            cmd.camp = _target.camp;
            cmd.x = x;
            cmd.y =y;
            _target.board.Do(cmd);
        }
        else
        {
            Debug.Log("无地可下");
        }
        
    }
}