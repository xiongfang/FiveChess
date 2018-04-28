using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 神经细胞
/// </summary>
public class UNeuron  {
    
    public UNeuron[] Inputs;
    public double[] Weights;

    public double Output;

    protected bool _dirty;

    public UNeuron(int NumInputs)
    {
        Inputs = new UNeuron[NumInputs];
        Weights = new double[0];
    }

    public double GetOutput()
    {
        if (_dirty)
        {
            UpdateOutput();
            _dirty = false;
        }
            
        return Output;
    }

    protected virtual void UpdateOutput()
    {
        _dirty = false;
    }

    public void SetDirty()
    {
        _dirty = true;
        foreach(var i in Inputs)
        {
            i.SetDirty();
        }
    }

    public void GetWeights(List<double> w)
    {
        w.AddRange(Weights);
        foreach (var i in Inputs)
        {
            i.GetWeights(w);
        }
    }

    public void PutWeights(List<double> w)
    {
        int index = w.Count - Weights.Length;
        int count = Weights.Length;
        w.CopyTo(index, Weights, 0,count);
        w.RemoveRange(index, count);
        foreach (var i in Inputs)
        {
            i.PutWeights(w);
        }
    }


    protected void RandomWeights()
    {
        for(int i=0;i<Weights.Length;i++)
        {
            Weights[i] = Random.value;
        }
    }
}


public class UNeuronSigmod:UNeuron
{
    public static double Bias = -1.0;
    public static double ActivationResponse = 1.0f;


    public UNeuronSigmod(int NumInputs):base(NumInputs)
    {
        //多一个权重值
        Weights = new double[NumInputs + 1];
        RandomWeights();
    }


    double Sigmod(double activation, double response)
    {
        return 1.0 / (1.0 + System.Math.Exp(-activation / response));
    }

    protected override void UpdateOutput()
    {
        double NetInputs = 0;
        int WeightIndex = 0;
        //算权重和输入的和
        for (int k = 0; k < Weights.Length - 1; k++)
        {
            NetInputs += Weights[k] * Inputs[WeightIndex++].GetOutput();
        }
        //加入偏移
        NetInputs += Weights[Weights.Length - 1] * Bias;

        Output =  Sigmod(NetInputs, ActivationResponse);
    }
}

public class UNeuronSoftmax:UNeuron
{
    public UNeuronSoftmax(int NumInputs)
        : base(NumInputs)
    {
        //多一个权重值
        //Weights = new double[NumInputs];
    }

    protected override void UpdateOutput()
    {
        double NetInputs = 0;

        //算权重和输入的和
        for (int k = 0; k < Inputs.Length; k++)
        {
            NetInputs += System.Math.Exp(Inputs[k].GetOutput());
        }

        double max = System.Math.Exp(Inputs[0].GetOutput()) / NetInputs;
        int max_index = 0;
        for (int k = 1; k < Inputs.Length; k++)
        {
            double v = System.Math.Exp(Inputs[k].GetOutput()) / NetInputs;
            if(max<v)
            {
                max_index = k;
                max = v;
            }
        }

        Output = max_index;
    }
}

public class UNeuronRate:UNeuron
{
    int[] _inputs;
    public UNeuronRate(int NumInputs)
        : base(NumInputs)
    {
        //多一个权重值
        Weights = new double[NumInputs];
        RandomWeights();
    }

    public void SetInputPos(int[] inputs)
    {
        _inputs = inputs;
    }

    protected override void UpdateOutput()
    {
        double NetInputs = 0;

        double[] values = new double[_inputs.Length];

        //算权重和输入的和
        for (int k = 0; k < Inputs.Length; k++)
        {
            values[k] = Inputs[k].GetOutput() * _inputs[k] * Weights[k];
            NetInputs += System.Math.Exp(values[k]);
        }

        double max = System.Math.Exp(values[0]) / NetInputs;
        int max_index = 0;
        for (int k = 1; k < Inputs.Length; k++)
        {
            double v = System.Math.Exp(values[k]) / NetInputs;
            if (max < v)
            {
                max_index = k;
                max = v;
            }
        }

        Output = max_index;
    }
}


//卷积
public class UNeuronConvolution:UNeuron
{
    double[][] matrix;


}