using System;
using System.Collections;
using System.Collections.Generic;
using DynamicExpresso;
using DynamicExpresso.Exceptions;
using UnityEngine;

namespace BBBirder.UnityVue
{
    public class DataBinder : MonoBehaviour
    {
        [Serializable]
        public struct ExpressionItem
        {
            public bool enable;
            public string expression;
        }
        public Component target;
        public List<ExpressionItem> expressions = new() { new() { enable = true } };
        public DataProvider dataProvider;
        private Dictionary<string, WatchScope> activeScopes = new();
        // Start is called before the first frame update
        void Start()
        {
            var data = dataProvider.TypelessData;
            dataProvider.listeners.Add(this);
            if (data != null)
            {
                CSReactive.Reactive(data);
            }
            foreach (var expression in expressions)
            {
                if (!expression.enable) continue;
                StartExpression(expression.expression);
            }
        }

        void OnDestroy()
        {
            if (dataProvider)
            {
                dataProvider.listeners.Remove(this);
            }
        }
        public void StartExpression(string expression)
        {
            if (!Application.isPlaying) return;
            var data = dataProvider?.TypelessData;
            if (data is null) return;
            CSReactive.Reactive(data);
            var action = CreatePreparedInterpreter().Parse(expression, new Parameter("this", dataProvider.DataType, null));
            if (!activeScopes.ContainsKey(expression))
            {
                var scp = CSReactive.WatchEffect(() => action.Invoke(data));
                activeScopes.TryAdd(expression, scp);
            }
        }

        public void StopExpression(string expression)
        {
            if (!Application.isPlaying) return;
            if (activeScopes.TryGetValue(expression, out var scp))
            {
                scp.Dispose();
            }
            activeScopes.Remove(expression);
        }

        public Interpreter CreatePreparedInterpreter()
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Reference(typeof(Vector2));
            interpreter.Reference(typeof(Vector3));
            interpreter.Reference(typeof(Debug));
            interpreter.SetVariable("target", target, target.GetType());
            return interpreter;
        }

        internal string CompileTest(string expression)
        {
            if (!dataProvider) return null;
            try
            {
                CreatePreparedInterpreter().Parse(expression, new Parameter("this", dataProvider.DataType, null));
            }
            catch (ParseException e)
            {
                return e.GetType().Name + ": " + expression.Insert(e.Position, "<color=#ff0000>") + "</color>";
            }
            return null;
        }

        public void Refresh()
        {
            foreach (var expression in expressions)
            {
                StopExpression(expression.expression);
            }
            foreach (var expression in expressions)
            {
                if (!expression.enable) continue;
                StartExpression(expression.expression);
            }
        }
    }
}
