using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
    public event System.Action on_value_updated;
    public bool auto_update;

    protected virtual void OnValidate() { // it is called when value is changed in the unity inspector
        if(auto_update) {
            NotifyOfUpdatedValues();
        }
    }

    public void NotifyOfUpdatedValues() {
        on_value_updated?.Invoke();
    }

}
