using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FutureValue<T> : CustomYieldInstruction
{
    private bool isSet = false;
    private T _value;

    public override bool keepWaiting => !isSet;
    public T value { get { return _value; } }
    public void SetValue(T value)
    {
        _value = value;
        isSet = true;
    }
}
