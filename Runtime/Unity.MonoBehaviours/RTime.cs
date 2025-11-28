using UnityEngine;

namespace BBBirder.UnityVue
{
    public static class RTime
    {
        public static float time => UnityVueDriver.time;
        public static float fixedTime => UnityVueDriver.fixedTime;
        public static float unscaledTime => UnityVueDriver.unscaledTime;
        public static float fixedUnscaledTime => UnityVueDriver.fixedUnscaledTime;
    }
}