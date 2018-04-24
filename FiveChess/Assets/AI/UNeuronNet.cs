using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 神经网络基类
/// </summary>
public abstract class UNeuronNet {

    public List<UNeuronLayer> AllLayers;

    public class ConfigData
    {
        //public int NumInputsPerNeuron = 10;
        public int NumNeuronPerHiddenLayer = 10;
        public int NumHiddenLayer = 1;
        public int NumInputs = 10;
        public int NumOutputs = 3;
    }

    protected ConfigData Config;

    public void Init(ConfigData config)
    {
        Config = config;
        AllLayers = new List<UNeuronLayer>();
        if (Config.NumHiddenLayer > 0)
        {
            AllLayers.Add(new UNeuronLayer(Config.NumNeuronPerHiddenLayer, Config.NumInputs));

            for (int i = 0; i < Config.NumHiddenLayer - 1; i++)
            {
                AllLayers.Add(new UNeuronLayer(Config.NumNeuronPerHiddenLayer, Config.NumNeuronPerHiddenLayer));
            }

            AllLayers.Add(new UNeuronLayer(Config.NumOutputs, Config.NumNeuronPerHiddenLayer));
        }
        else
        {
            AllLayers.Add(new UNeuronLayer(Config.NumOutputs, Config.NumInputs));
        }
    }

    //从神经网络读取权重
    public List<double> GetWeights()
    {
        List<double> Weights = new List<double>();
        for (int i = 0; i < AllLayers.Count; i++)
        {
            for (int j = 0; j < AllLayers[i].Neurons.Length; j++)
            {
                Weights.AddRange(AllLayers[i].Neurons[j].InputWeights);
            }
        }
        return Weights;
    }

    //替换神经网络的权重
    public void PutWeights(List<double> Weights)
    {
        int index = 0;
        for (int i = 0; i < AllLayers.Count; i++)
        {
            for (int j = 0; j < AllLayers[i].Neurons.Length; j++)
            {
                AllLayers[i].Neurons[j].InputWeights = Weights.GetRange(index, AllLayers[i].Neurons[j].InputWeights.Length).ToArray();
                index += AllLayers[i].Neurons[j].InputWeights.Length;
            }
        }
    }

    //统计权重数量
    public int GetWeightCount()
    {
        int count = 0;
        for (int i = 0; i < AllLayers.Count; i++)
        {
            for (int j = 0; j < AllLayers[i].Neurons.Length; j++)
            {
                count += AllLayers[i].Neurons[j].InputWeights.Length;
            }
        }
        return count;
    }
}


/// <summary>
/// 《游戏编程中的AI算法》书中的算法，参考用
/// </summary>
public class UNeuronNet_Test : UNeuronNet
{

    public static double Bias = -1.0;
    public static double ActivationResponse = 1.0f;


    double Sigmod(double activation, double response)
    {
        return 1.0 / (1.0 + System.Math.Exp(-activation / response));
    }

    public double[] Update(double[] inputs)
    {
        List<double> outputs = new List<double>();

        for (int i = 0; i < AllLayers.Count; i++)
        {
            if (i > 0)
            {
                inputs = outputs.ToArray();
            }
            outputs.Clear();


            for (int j = 0; j < AllLayers[i].Neurons.Length; j++)
            {
                double NetInputs = 0;
                int WeightIndex = 0;
                //算权重和输入的和
                for (int k = 0; k < AllLayers[i].Neurons[j].InputWeights.Length - 1; k++)
                {
                    NetInputs += AllLayers[i].Neurons[j].InputWeights[k] * inputs[WeightIndex++];
                }
                //加入偏移
                NetInputs += AllLayers[i].Neurons[j].InputWeights[AllLayers[i].Neurons[j].InputWeights.Length - 1] * Bias;

                //输出
                outputs.Add(Sigmod(NetInputs, ActivationResponse));
            }
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
    double[] GetInputs(Map input)
    {
        double[] outputs = new double[Config.NumInputs];

        int output_index = 0;
        for (int i = 0; i < input.map.Length; i++)
        {
            for (int j = 0; j < input.map[i].Length; j++)
            {
                outputs[output_index++] = (input.map[i][j]);
            }
        }

        outputs[output_index++] = Map.GetCampMapValue( input.currentCamp);

        return outputs;
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
        List<Map.Cmd> CmdList = GetCommandList(inputs);
        if (CmdList.Count == 0)
        {
            return null;
        }

        double[] outputs = Update(GetInputs(inputs));
        int index = Mathf.FloorToInt((float)(outputs[0] * CmdList.Count));
        index = Mathf.Clamp(index, 0, CmdList.Count - 1);
        return CmdList[index];
    }
}