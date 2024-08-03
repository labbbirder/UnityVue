using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public class DataProvider : MonoBehaviour
    {
        public abstract IWatchable GetData() { get; }
        public abstract Type DataType { get; }
    }
}
