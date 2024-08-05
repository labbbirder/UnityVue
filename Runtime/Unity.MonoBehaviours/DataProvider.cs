using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public abstract class DataProvider : MonoBehaviour
    {
        public abstract IWatchable GetData();
        public abstract Type DataType { get; }
    }
}
