using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 神经网络基类
/// </summary>
public abstract class UNeuronNet {

    public List<UNeuron> OutputLayer;
    public List<UNeuron> BaseLayer;

    //public class ConfigData
    //{
    //    //public int NumInputsPerNeuron = 10;
    //    public int NumNeuronPerHiddenLayer = 10;
    //    public int NumHiddenLayer = 1;
    //    public int NumInputs = 10;
    //    public int NumOutputs = 3;
    //}

    //protected ConfigData Config;

    //public void Init(ConfigData config)
    //{
    //    Config = config;
    //    AllLayers = new List<UNeuronLayer>();
    //    if (Config.NumHiddenLayer > 0)
    //    {
    //        AllLayers.Add(new UNeuronLayer(Config.NumNeuronPerHiddenLayer, Config.NumInputs));

    //        for (int i = 0; i < Config.NumHiddenLayer - 1; i++)
    //        {
    //            AllLayers.Add(new UNeuronLayer(Config.NumNeuronPerHiddenLayer, Config.NumNeuronPerHiddenLayer));
    //        }

    //        AllLayers.Add(new UNeuronLayer(Config.NumOutputs, Config.NumNeuronPerHiddenLayer));
    //    }
    //    else
    //    {
    //        AllLayers.Add(new UNeuronLayer(Config.NumOutputs, Config.NumInputs));
    //    }
    //}

    //从神经网络读取权重
    public List<double> GetWeights()
    {
        List<double> Weights = new List<double>();
        for (int i = 0; i < OutputLayer.Count; i++)
        {
            OutputLayer[i].GetWeights(Weights);
        }
        return Weights;
    }

    //替换神经网络的权重
    public void PutWeights(List<double> Weights)
    {
        for (int i = 0; i < OutputLayer.Count; i++)
        {
            OutputLayer[i].PutWeights(Weights);
        }
    }

    //统计权重数量
    public int GetWeightCount()
    {
        return GetWeights().Count;
    }
}


/// <summary>
/// 《游戏编程中的AI算法》书中的算法，参考用
/// </summary>
public class UNeuronNet_Test : UNeuronNet
{

    public double[] Update(double[] inputs)
    {
        for(int i=0;i<BaseLayer.Count;i++)
        {
            BaseLayer[i].Output = inputs[i];
        }

        foreach(var o in OutputLayer)
        {
            o.SetDirty();
        }

        List<double> outputs = new List<double>();

        for (int j = 0; j < OutputLayer.Count; j++)
        {
            //输出
            outputs.Add(OutputLayer[j].GetOutput());
        }

        return outputs.ToArray();
    }
}

///// <summary>
///// 棋子AI，输入棋子，返回棋子的行走指令
///// </summary>
//public class UNeuronNet_Chess:UNeuronNet
//{
//    public UIChessboard.Cmd Update(UIChessboard input)
//    {
//        return null;
//    }
//}

/// <summary>
/// 控制器AI，输入棋盘的所有棋子，返回可走的指令
/// </summary>
public class UNeuronNet_Controller: UNeuronNet_Test
{
    UNeuronRate _outputNode;
    public UNeuronNet_Controller()
    {
        //输入
        BaseLayer = new List<UNeuron>();
        for (int i = 0; i < 15 * 15 + 1; i++)
        {
            BaseLayer.Add(new UNeuron(0));
        }

        
        //第一个隐藏层
        List<UNeuron> layer1 = new List<UNeuron>();
        for (int i = 0; i < 5;i++)
        {
            UNeuronSigmod node = new UNeuronSigmod(BaseLayer.Count);

            for (int j = 0; j < node.Inputs.Length; j++)
            {
                node.Inputs[j] = BaseLayer[j];
            }

            layer1.Add(node);
        }

        //第二个隐藏层
        List<UNeuron> layer2 = new List<UNeuron>();
        for (int i = 0; i < 15 * 15; i++)
        {
            UNeuronSigmod node = new UNeuronSigmod(layer1.Count);

            for (int j = 0; j < node.Inputs.Length; j++)
            {
                node.Inputs[j] = layer1[j];
            }

            layer2.Add(node);
        }

        //第三层是一个几率

        OutputLayer = new List<UNeuron>();
        _outputNode = new UNeuronRate(15 * 15);
        OutputLayer.Add(_outputNode);
        for (int i = 0; i < _outputNode.Inputs.Length; i++)
        {
            _outputNode.Inputs[i] = layer2[i];
        }
    }

    double[] GetInputs(Map input)
    {
        List<double> outputs = new List<double>();

        for (int i = 0; i < input.map.Length; i++)
        {
            for (int j = 0; j < input.map[i].Length; j++)
            {
                outputs.Add(input.map[i][j]);
            }
        }

        outputs.Add(Map.GetCampMapValue( input.currentCamp));

        return outputs.ToArray();
    }

    int[] GetInputsRate(Map input)
    {
        List<int> outputs = new List<int>();

        for (int i = 0; i < input.map.Length; i++)
        {
            for (int j = 0; j < input.map[i].Length; j++)
            {
                outputs.Add(input.map[i][j]==0?1:0);
            }
        }
        return outputs.ToArray();
    }

    List<Map.Cmd> GetCommandList(Map input)
    {
        List<Map.Cmd> R = new List<Map.Cmd>();
        for (int i = 0; i < input.map.Length; i++)
        {
            for (int j = 0; j < input.map[i].Length; j++)
            {
                if (input.map[i][j] == 0)
                {
                    Map.Cmd cmd = new Map.Cmd();
                    cmd.x = i + 1;
                    cmd.y = j + 1;
                    cmd.camp = input.currentCamp;
                    R.Add(cmd);
                }
            }
        }
        return R;
    }

    public Map.Cmd Update(Map inputs)
    {
        //List<Map.Cmd> CmdList = GetCommandList(inputs);
        //if (CmdList.Count == 0)
        //{
        //    return null;
        //}
        _outputNode.SetInputPos(GetInputsRate(inputs));
        double[] outputs = Update(GetInputs(inputs));

        int index = Mathf.FloorToInt((float)(outputs[0]));

        Map.Cmd cmd = new Map.Cmd();
        cmd.x = index/15 + 1;
        cmd.y = index%15 + 1;
        cmd.camp = inputs.currentCamp;

        return cmd;
    }
}