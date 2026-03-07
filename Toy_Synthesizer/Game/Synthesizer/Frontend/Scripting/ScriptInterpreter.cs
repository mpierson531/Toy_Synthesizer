using System;
using System.Linq.Expressions;

using DynamicExpresso;

using GeoLib.GeoUtils.Collections;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting
{
    public class ScriptInterpreter
    {
        private readonly Interpreter interpreter;

        public readonly ViewableList<FunctionMetaData> Functions;
        public readonly ViewableList<string> Variables;
        public readonly ViewableList<Expression> Expressions;

        public ScriptInterpreter(InterpreterOptions options, AssignmentOperators assignment, Type[] references)
        {
            interpreter = new Interpreter(options);

            Functions = new ViewableList<FunctionMetaData>(100);
            Variables = new ViewableList<string>(100);
            Expressions = new ViewableList<Expression>(100);

            if ((options & InterpreterOptions.CaseInsensitive) == InterpreterOptions.CaseInsensitive) // Disabling this no matter what. 
            {
                options &= ~InterpreterOptions.CaseInsensitive;
            }

            interpreter.EnableAssignment(assignment);

            for (int index = 0; index != references.Length; index++)
            {
                interpreter.Reference(references[index]);
            }
        }

        public object Evaluate(string script, params Parameter[] parameters)
        {
            return interpreter.Eval(script, parameters);
        }

        public object TryEvaluate(string script, params Parameter[] parameters)
        {
            object returnValue;

            try
            {
                returnValue = Evaluate(script, parameters);
            }
            catch (Exception e)
            {
                returnValue = e.ToString();
            }

            return returnValue;
        }

        public IdentifiersInfo GetIdentifiers(string script)
        {
            return interpreter.DetectIdentifiers(script);
        }

        public ScriptInterpreter AddTypeReferences(Type[] types)
        {
            for (int index = 0; index != types.Length; index++)
            {
                AddTypeReference(types[index]);
            }

            return this;
        }

        public ScriptInterpreter AddTypeReference(Type type)
        {
            interpreter.Reference(type);

            return this;
        }

        public ScriptInterpreter AddFunctions(FunctionEntry[] functions)
        {
            for (int index = 0; index != functions.Length; index++)
            {
                AddFunction(functions[index]);
            }

            return this;
        }

        public ScriptInterpreter AddFunction(FunctionMetaData metaData, Delegate value)
        {
            return AddFunction(new FunctionEntry(metaData, value));
        }

        public ScriptInterpreter AddFunction(FunctionEntry function)
        {
            interpreter.SetFunction(function.MetaData.Name, function.Value);

            Functions.Add(function.MetaData);

            return this;
        }

        public ScriptInterpreter AddVariable(string name, object value)
        {
            return AddVariable(new Variable(name, value));
        }

        public ScriptInterpreter AddVariables(Variable[] variables)
        {
            for (int index = 0; index != variables.Length; index++)
            {
                AddVariable(variables[index]);
            }

            return this;
        }

        public ScriptInterpreter AddVariable(Variable variable)
        {
            interpreter.SetVariable(variable.Name, variable.Value);

            Variables.Add(variable.Name);

            return this;
        }

        public ScriptInterpreter AddExpressions(ValueTuple<string, Expression>[] expressions)
        {
            for (int index = 0; index != expressions.Length; index++)
            {
                AddExpression(expressions[index].Item1, expressions[index].Item2);
            }

            return this;
        }

        public ScriptInterpreter AddExpression(string name, Expression expression)
        {
            interpreter.SetExpression(name, expression);

            Expressions.Add(expression);

            return this;
        }

        public ScriptInterpreter ClearAll()
        {
            ClearFunctions();
            ClearVariables();

            return this;
        }

        public ScriptInterpreter ClearFunctions()
        {
            for (int index = 0; index != Functions.Count; index++)
            {
                interpreter.UnsetFunction(Functions.GetUnchecked(index).Name);
            }

            Functions.Clear();

            return this;
        }

        public ScriptInterpreter ClearVariables()
        {
            for (int index = 0; index != Variables.Count; index++)
            {
                interpreter.UnsetVariable(Variables.GetUnchecked(index));
            }

            Variables.Clear();

            return this;
        }
    }
}
