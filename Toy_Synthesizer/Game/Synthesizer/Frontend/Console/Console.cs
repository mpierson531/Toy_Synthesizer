using System;
using System.Text;

using Microsoft.Xna.Framework;

using FontStashSharp;

using DynamicExpresso;

using GeoLib;
using GeoLib.GeoInput;
using GeoLib.GeoLogging;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Pooling;
using GeoLib.GeoUtils.Collections;

using Toy_Synthesizer.Game.UI;
using Toy_Synthesizer.Game.Midi;
using Toy_Synthesizer.Game.Synthesizer;
using Toy_Synthesizer.Game.Synthesizer.Backend;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Help;
using Toy_Synthesizer.Game.Synthesizer.Frontend.Scripting;

namespace Toy_Synthesizer.Game.Synthesizer.Frontend.Console
{
    using ShapeType = GeoLib.GeoShapes.ShapeType;


    // This should only be instantiated in Core_Plugin.Initialize, after the GameWorld has been initialized.
    // Still need to handle label wrapping, horizontal scrolling, and text highlighting.
    public class Console
    {
        private static readonly string NewLine;
        private static readonly string DoubleNewLine;
        private static readonly int NewLineLength;
        private const string Tab = Game.Tab; // It appears FontStashSharp treats '\t' as a space. Just doing four spaces for a tab.
        private const string DoubleTab = Game.DoubleTab;
        private const string ColorTypeName = "Color";

        private static readonly ImmutableArray<FunctionParameter> emtpyFunctionParameters;

        static Console()
        {
            NewLine = Game.NewLine;
            DoubleNewLine = Game.DoubleNewLine;
            NewLineLength = Game.NewLineLength;
            emtpyFunctionParameters = ImmutableArray<FunctionParameter>.Empty;
        }

        private bool isInitialized = false;

        private readonly Game game;
        private ScriptInterpreter interpreter;
        private readonly ViewableList<string> history;
        private int historyIndex;
        private ImmutableArray<Command> commands;
        private HelpItemDictionary consoleItems;
        private GroupWidget mainGroup;
        private Color defaultOutputTint;
        private Color defaultOutputTextTint;
        private Color defaultInputTint;
        private Color defaultInputTextTint;
        private Color outputTint;
        private Color outputTextTint;
        private Color inputTint;
        private Color inputTextTint;
        private char inputTerminator;

        public HelpItemDictionary ConsoleItems
        {
            get => consoleItems;
        }

        public Color OutputTint
        {
            get => outputTint;

            set
            {
                outputTint = value;

                SetUITints();
            }
        }

        public Color OutputTextTint
        {
            get => outputTextTint;

            set
            {
                outputTextTint = value;

                SetUITints();
            }
        }

        public Color InputTint
        {
            get => inputTint;

            set
            {
                inputTint = value;

                SetUITints();
            }
        }

        public Color InputTextTint
        {
            get => inputTextTint;

            set
            {
                inputTextTint = value;

                SetUITints();
            }
        }

        private ScrollPane scrollPane;
        private DynamicPlainLabel label;
        private TextField inputField;
        private float targetFontScale;
        private bool isShowing;

        public bool IsShowing
        {
            get => isShowing;
        }

        public Console(Game game)
        {
            this.game = game;

            history = new ViewableList<string>(100);
            historyIndex = -1;

            inputTerminator = '>';

            isShowing = false;
        }

        public void Init()
        {
            commands = GetCommands();

            consoleItems = new HelpItemDictionary(MakeConsoleItems());
            interpreter = InitScriptIntepreter();
        }

        private ImmutableArray<Command> GetCommands()
        {
            return new ImmutableArray<Command>
            (
                new Command[]
                {
                    Command.GetNewCls(),
                    Command.GetNewClear(),
                    Command.GetNewHelp(),
                    Command.GetNewIndividualHelp(),
                }
            );
        }

        private HelpItem[] MakeConsoleItems()
        {
            return new HelpItem[]
            {
                MakeBaseConsoleItem(),
                MakeGameConsoleItem(),

                MakeVec2fConsoleItem(),
                MakeVector2ConsoleItem(),
                MakeColorConsoleItem(),
                MakeBuiltinColorsConsoleItem(),
                MakeLogLevelConsoleItem(),
            };
        }

        private static HelpItem MakeBaseConsoleItem()
        {
            ViewableList<HelpItem> childItems = new ViewableList<HelpItem>
            (
                HelpItem.NotReadonlyProperty("OutputTint", ColorTypeName, "The tint of the console's background", "tint"),
                HelpItem.NotReadonlyProperty("OutputTextTint", ColorTypeName, "The tint of the text in the console output", "tint"),
                HelpItem.NotReadonlyProperty("InputTint", ColorTypeName, "The tint of the input field's background", "tint"),
                HelpItem.NotReadonlyProperty("InputTextTint", ColorTypeName, "The tint of the input field's text", "tint"),
                HelpItem.FunctionNoParams("Clear", "Clears the console output")
            );

            return HelpItem.ReadonlyProperty("Console", "GeoSimConsole", "The console", childItems);
        }

        private static HelpItem MakeGameConsoleItem()
        {
            ViewableList<HelpItem> childItems = new ViewableList<HelpItem>
            (
                HelpItem.NotReadonlyProperty("UITint", ColorTypeName, "The overall UI tint. This will be blended with the tint of individual UI elements", "tint"),
                HelpItem.NotReadonlyProperty("WorldTint", ColorTypeName, "The overall World tint. This will be blended with the tint of individual entities", "tint")
            );

            return HelpItem.ReadonlyProperty("Game", "Game", "The component that drives the core updating and rendering.", childItems);
        }

        private static HelpItem MakeColorConsoleItem()
        {
            ImmutableArray<string> constructorExamples = new ImmutableArray<string>
            (
                "new Color(int r, int g, int b)",
                "new Color(int r, int g, int b, int a)",
                "new Color(int packedValue)"
            );

            ImmutableArray<string> usageExamples = new ImmutableArray<string>
            (
                "Color.R",
                "Color.G",
                "Color.B",
                "Color.A",

                Game.NewLine,

                "Color.Red",
                "Color.White",
                "Color.Blue",
                "etc."
            );

            return HelpItem.TypeReference("Color", "Color", "A struct that represents a color as 4 bytes (RGBA) in a range of (0..255)", constructorExamples, usageExamples, memberItems: null);
        }

        private static HelpItem MakeVec2fConsoleItem()
        {
            ImmutableArray<string> constructorExamples = new ImmutableArray<string>
            (
                "new Vec2f(float x, float y)",
                "new Vec2f(float value)",
                "new Vec2f(Vector2 value)"
            );

            ImmutableArray<string> usageExamples = new ImmutableArray<string>
            (
                "vector.X",
                "vector.Y"
            );

            return HelpItem.TypeReference("Vec2f", "Vec2f", "A struct that represents a vector consisting of two 32-bit floating point components", constructorExamples, usageExamples, memberItems: null);
        }

        private static HelpItem MakeVector2ConsoleItem()
        {
            ImmutableArray<string> constructorExamples = new ImmutableArray<string>
            (
                "new Vector2(float x, float y)",
                "new Vector2(float value)"
            );

            ImmutableArray<string> usageExamples = new ImmutableArray<string>
            (
                "vector.X",
                "vector.Y"
            );

            return HelpItem.TypeReference("Vector2", "Vector2", "A struct that represents a vector consisting of two 32-bit floating point components", constructorExamples, usageExamples, memberItems: null);
        }

        private static HelpItem MakeBuiltinColorsConsoleItem()
        {
            ViewableList<HelpItem> childItems = new ViewableList<HelpItem>
            (
                HelpItem.FunctionOneParam("ToName", "Tries to convert the argument to a matching color name", "color", "Color or string")
            );

            return HelpItem.ReadonlyProperty("BuiltinColors", "BuiltinColors", "Provides a few utilities for converting Color to a more readable format", childItems);
        }

        private static HelpItem MakeLogLevelConsoleItem()
        {
            const string typeName = "LogLevel";

            ImmutableArray<string> examples = new ImmutableArray<string>
            (
                "LogLevel.Info",
                "LogLevel.Warning",
                "LogLevel.Error",
                "LogLevel.Debug"
            );

            ViewableList<HelpItem> members = new ViewableList<HelpItem>
            (
                HelpItem.ReadonlyProperty("Info", "LogLevel", "Log level of general information"),
                HelpItem.ReadonlyProperty("Warning", "LogLevel", "Log level of a warning"),
                HelpItem.ReadonlyProperty("Error", typeName, "Log level of an error"),
                HelpItem.ReadonlyProperty("Debug", typeName, "Log level of debug information")
            );

            return HelpItem.TypeReference("LogLevel", "LogLevel", "An enum indicating the level or severity of log information", null, examples, members);
        }

        public void Toggle()
        {
            if (game.HasUIWidget(mainGroup))
            {
                game.UnfocusUI();
                game.RemoveUIWidget(mainGroup);

                isShowing = false;
            }
            else
            {
                game.AddUIWidget(mainGroup);
                game.FocusUI(inputField, true);

                CheckAndSetBounds();

                isShowing = true;
            }
        }

        public void InitUI()
        {
            if (isInitialized)
            {
                throw new InvalidOperationException("GeoSimConsole was already initialized!");
            }

            isInitialized = true;

            UIManager uiManager = game.UIManager;

            mainGroup = new GroupWidget(Vec2f.Zero, Vec2f.Zero) { UpdateInvisible = false };

            Vec2f size = GetConsoleSize();
            Vec2f position = GetConsolePosition(size);
            Vec2f inputFieldSize = GetConsoleInputSize(size);
            Vec2f inputFieldPosition = GetConsoleInputPosition(position, size, inputFieldSize);
            Vec2fValue inputFieldTextPadding = Vec2fValue.Absolute(game.ScaleByDisplayResolution_Min(10f), 0f);
            Vec2f scrollPaneSize = GetConsoleScrollPaneSize(size, inputFieldSize);
            Vec2f scrollPanePosition = GetConsoleScrollPanePosition(position);

            mainGroup.Position = position;
            mainGroup.Size = size;

            scrollPane = uiManager.ScrollPane(scrollPanePosition, scrollPaneSize);

            targetFontScale = GetTargetFontScale(uiManager.MainFont.FontSize);

            Vec2f labelSize = GetConsoleLabelSize(uiManager, targetFontScale);

            inputField = uiManager.GeneralTextField_SharpCorners(inputFieldPosition, inputFieldSize, maxCharacters: int.MaxValue, defaultText: null);
            inputField.TintCaret = false;
            UIManager.SetTextPadding(inputField.Style, inputFieldTextPadding);
            inputField.FitText = false;
            inputField.ScaleTextOnResize = false;
            inputField.ScaleTextOnScale = false;
            inputField.FontScale = GetTargetFontScale(inputField.Font.FontSize);
            label = uiManager.DynamicPlainLabel(scrollPanePosition, labelSize, Utils.EmptyString);
            label.Alignment = Alignment.TopLeft;
            label.FitText = false;
            label.ScaleTextOnResize = false;
            label.ScaleTextOnScale = false;
            label.FontScale = targetFontScale;
            label.LineSpacing = -1;
            label.Append(inputTerminator);

            scrollPane.AddChild(label);

            mainGroup.AddChild(scrollPane);
            mainGroup.AddChild(inputField);

            inputField.Layout();

            inputField.OnEnter = EnterInput;
            inputField.OnUp = InputFieldGoBackwardInHistory;
            inputField.OnDown = InputFieldGoForwardInHistory;

            UIManager.AddDefaultMouseClickListener(mainGroup);
            UIManager.AddEscapeListener(mainGroup, delegate
            {
                if (IsShowing)
                {
                    Toggle();
                }
            },
            isCapture: true);

            defaultOutputTint = uiManager.ScrollPaneColor;
            defaultOutputTint.A = 185;
            defaultOutputTextTint = uiManager.GeneralTextColor;
            defaultInputTint = uiManager.TextFieldTint;
            defaultInputTextTint = uiManager.TextFieldTextTint;
            outputTint = defaultOutputTint;
            outputTextTint = defaultOutputTextTint;
            inputTint = defaultInputTint;
            inputTextTint = defaultInputTextTint;

            SetUITints();
        }

        private float GetTargetFontScale(float currentFontSize)
        {
            float targetFontSize = GetTargetFontSize();

            return targetFontSize / currentFontSize;
        }

        private float GetTargetFontSize()
        {
            return game.ScaleByDisplayResolution_Min(11f);
        }

        public bool GoForwardInHistory()
        {
            return TraverseHistory(1);
        }

        public bool GoBackwardInHistory()
        {
            return TraverseHistory(-1);
        }

        public void AddConsoleItem(HelpItem item)
        {
            ConsoleItems.Add(item);
        }

        public void AddInterpreterVariable(string name, object value)
        {
            interpreter.AddVariable(new Variable(name, value));
        }

        public void AddInterpreterVariable(Variable variable)
        {
            interpreter.AddVariable(variable);
        }

        public void AddInterpreterFunction(FunctionEntry function)
        {
            interpreter.AddFunction(function);
        }

        /**
         * <summary>
         * 
         * Returns true if it successfully switched to a historical input.
         * 
         * </summary>
         **/
        public bool TraverseHistory(int direction)
        {
            if (history.IsEmpty)
            {
                return false;
            }

            // If called internally, direction will only ever be -1 or 1.
            // If called publicly, it's possible to be something else; doing simple sign check to make sure it is valid.
            direction = Math.Sign(direction);

            if (direction == 0)
            {
                return false;
            }

            string input = inputField.Text;

            if (!history.Contains(input))
            {
                historyIndex = -1;
            }

            if (historyIndex == -1)
            {
                historyIndex = direction == -1 ? 0 : history.Count - 1;
            }
            else
            {
                historyIndex = Math.Clamp(historyIndex + direction, 0, history.Count - 1);
            }

            string historyText = history[historyIndex];

            if (historyText == input)
            {
                return false;
            }

            inputField.Text = history[historyIndex];

            inputField.SetCaretPosition(inputField.Text.Length);

            return true;
        }

        private bool InputFieldGoForwardInHistory(TextField _)
        {
            if (ShiftOrControlIsDown())
            {
                return false;
            }

            GoForwardInHistory();

            return true; // Return true no matter what, for UX purposes
        }

        private bool InputFieldGoBackwardInHistory(TextField _)
        {
            if (ShiftOrControlIsDown())
            {
                return false;
            }

            GoBackwardInHistory();

            return true; // Return true no matter what, for UX purposes
        }

        private bool ShiftOrControlIsDown()
        {
            KeyStates keyboard = game.Geo.Input.keyboard;

            return keyboard.Shift() || keyboard.Control();
        }

        public void WindowResized(int width, int height)
        {
            CheckAndSetBounds();

            if (!game.HasUIWidget(mainGroup))
            {
                Vec2f previousWindowSize = (Vec2f)game.Geo.Display.PreviousWindowSize;
                Vec2f windowSize = (Vec2f)game.Geo.Display.WindowSize;

                // This will trigger Stage.OnResize, which has LayoutUI in it.
                mainGroup.Size *= windowSize / previousWindowSize;
            }
        }

        private void LayoutUI()
        {
            Vec2f windowSize = (Vec2f)game.Geo.Display.WindowSize;

            // When Stage resizes, it should handle the sizing of the label, inputField, and scrollpane. 
            // It should also handle the sizing of the scrollpane.

            mainGroup.Position = windowSize - mainGroup.Size;
            //inputField.Size = GetConsoleInputSize(Stage.Size);
            inputField.Position = GetConsoleInputPosition(mainGroup.Position, mainGroup.Size, inputField.Size);
            //scrollPane.Position = GetConsoleScrollPanePosition(Stage.Position);
            //scrollPane.Size = GetConsoleScrollPaneSize(Stage.Size, inputField.Size);

            //label.Position = scrollPane.Position;

            //scrollPane.Layout();
        }

        public void NewFont(DynamicSpriteFont font)
        {
            targetFontScale = GetTargetFontScale(font.FontSize);

            label.Font = font;
            inputField.Font = font;

            inputField.FontScale = targetFontScale;
            label.FontScale = targetFontScale;
        }

        private ScriptInterpreter InitScriptIntepreter()
        {
            ScriptInterpreter interpreter = new ScriptInterpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions, AssignmentOperators.All, Array.Empty<Type>());

            Type[] typeReferences = GetIntepreterTypeReferences();
            FunctionEntry[] functions = GetInterpreterFunctions();
            Variable[] variables = GetInterpreterVariables();

            interpreter.AddTypeReferences(typeReferences);
            interpreter.AddFunctions(functions);
            interpreter.AddVariables(variables);

            for (int index = 0; index != commands.Count; index++)
            {
                Command command = commands[index];

                string name = "_" + command.Name;

                interpreter.AddVariable(name, command);
            }

            return interpreter;
        }

        private Type[] GetIntepreterTypeReferences()
        {
            return new Type[]
            {
                typeof(Voice),
                typeof(Oscillator),
                typeof(MidiUtils),
                typeof(WaveformType),
                typeof(EnvelopeStage),
                typeof(AdsrEnvelope),
                typeof(ChromaticScaleUtils),
                typeof(Key),
                typeof(Note),


                typeof(Vector2),
                typeof(Vec2f),
                typeof(ShapeType),
                typeof(Color),
                typeof(Colors),
                typeof(BuiltinColors),
                typeof(LogLevel)
            };
        }

        private FunctionEntry[] GetInterpreterFunctions()
        {
            return new FunctionEntry[]
            {
                new FunctionEntry(new FunctionMetaData("CollectGarbage", emtpyFunctionParameters, typeof(void)), () => GC.Collect())
            };
        }

        private Variable[] GetInterpreterVariables()
        {
            return new Variable[]
            {
                new Variable("Game", game),
                new Variable("Console", this),
                new Variable("Synthesizer", game.Synthesizer)
            };
        }

        public void ResetTint()
        {
            outputTint = defaultOutputTint;
            outputTextTint = defaultOutputTextTint;
            inputTint = defaultInputTint;
            inputTextTint = defaultInputTextTint;

            SetUITints();
        }

        public void Clear()
        {
            label.Clear();
            label.Append(inputTerminator);
            label.Size = GetConsoleLabelSize(game.UIManager, targetFontScale);
            scrollPane.ScrollVerticalBy(scrollPane.CurrentOffset.Y);
        }

        public string Help()
        {
            PoolableStringBuilder poolableBuilder = Pools.Common.StringBuilders.Get();

            Help(poolableBuilder.builder);

            string helpString = poolableBuilder.builder.ToString();

            Pools.Common.StringBuilders.Return(poolableBuilder);

            return helpString;
        }

        public void Help(StringBuilder builder)
        {
            builder.EnsureCapacity(500);

            builder.Append(NewLine);
            builder.Append("Commands:");
            builder.Append(NewLine);

            for (int commandIndex = 0; commandIndex != commands.Count; commandIndex++)
            {
                Command command = commands[commandIndex];

                builder.Append(command.Name);
                builder.Append(": ");
                builder.Append(command.Description);

                if (command.Parameters.Count != 0)
                {
                    builder.Append(NewLine);
                    builder.Append(Tab);

                    builder.Append("Parameters:");

                    for (int parameterIndex = 0; parameterIndex != command.Parameters.Count; parameterIndex++)
                    {
                        Help.Parameter parameter = command.Parameters[parameterIndex];

                        builder.Append(NewLine);
                        builder.Append(DoubleTab);

                        builder.Append(parameter.Name);
                        builder.Append(": ");
                        builder.Append(parameter.Type);
                    }
                }

                if (commandIndex != commands.Count - 1)
                {
                    builder.Append(DoubleNewLine);
                }
            }

            builder.Append(DoubleNewLine);

            if (!interpreter.Functions.IsEmpty)
            {
                builder.Append("Functions:");
                builder.Append(NewLine);

                for (int index = 0; index != interpreter.Functions.Count; index++)
                {
                    FunctionMetaData function = interpreter.Functions.GetUnchecked(index);

                    builder.Append(Tab);

                    builder.Append(function.Name);

                    if (function.Parameters is not null && function.Parameters.Count != 0)
                    {
                        builder.Append('(');

                        for (int paramIndex = 0; paramIndex != function.Parameters.Count; paramIndex++)
                        {
                            FunctionParameter parameter = function.Parameters[paramIndex];

                            builder.Append(parameter.Name);
                            builder.Append(": ");
                            builder.Append(parameter.Type.Name);

                            if (paramIndex != function.Parameters.Count - 1)
                            {
                                builder.Append(", ");
                            }
                        }

                        builder.Append(')');
                    }
                    else
                    {
                        builder.Append("()");
                    }

                    builder.Append(" -> ");
                    builder.Append(function.ReturnType is null ? "void" : function.ReturnType.Name);

                    builder.Append(DoubleNewLine);
                }
            }

            if (ConsoleItems.Count != 0)
            {
                builder.Append("Console Help Items:");
                builder.Append(DoubleNewLine);

                ConsoleItems.DisplayAllWithChildren(builder, newLinesBefore: 0, newLinesAfter: 1);
            }
        }

        // This needs to be an instance method for the interpreter to pick it up.
        public static string Help(HelpItem consoleItem)
        {
            PoolableStringBuilder poolableBuilder = Pools.Common.StringBuilders.Get();

            Help(consoleItem, poolableBuilder.builder);

            string help = poolableBuilder.builder.ToString();

            Pools.Common.StringBuilders.Return(poolableBuilder);

            return help;
        }

        public static void Help(Command command, StringBuilder stringBuilder)
        {
            stringBuilder.Append(command.Name);
            stringBuilder.Append(": ");
            stringBuilder.Append(command.Description);

            if (command.Parameters.Count != 0)
            {
                stringBuilder.Append(NewLine);
                stringBuilder.Append(Tab);

                stringBuilder.Append("Parameters:");

                for (int parameterIndex = 0; parameterIndex != command.Parameters.Count; parameterIndex++)
                {
                    Help.Parameter parameter = command.Parameters[parameterIndex];

                    stringBuilder.Append(NewLine);
                    stringBuilder.Append(DoubleTab);

                    stringBuilder.Append(parameter.Name);
                    stringBuilder.Append(": ");
                    stringBuilder.Append(parameter.Type);
                }
            }
        }

        public static void Help(HelpItem item, StringBuilder builder)
        {
            item.AppendToBuilder(builder, 0, 0, 0);
        }

        private void EnterInput()
        {
            string input = inputField.Text;

            // If a command was found and it was parsed successfully, command will be the command transformed into C#. 
            // If a command was found but could not be parsed completely, command will be the error.
            // If a command was not found, command will be input, and will be irrelevant.
            // If a command is found, name will be the name of the command regardless. Else, it will be input.
            if (!TryCommand(input, commands, consoleItems, out string command))
            {
                EnterIntoConsole(input, command);
            }
            else
            {
                Interpret(input, command);
            }
        }

        private void Interpret(string input, string script)
        {
            string returnValue;

            try
            {
                returnValue = Convert.ToString(interpreter.Evaluate(script));
            }
            catch (Exception e)
            {
                returnValue = e.Message;
            }

            EnterIntoConsole(input, returnValue);
        }

        public void EnterIntoConsole(string input, string value)
        {
            history.Add(input);

            //value = BuiltinColors.ToNames(value);

            bool valueHasNewLine = value.Contains(NewLine); // Try to forsee large value that may be ugly on the same line as the input reflection
            bool hasReturnValue = value.Length > 0;
            int lines = TextUtils.FindAll(input, NewLine) + TextUtils.FindAll(value, NewLine) + 2; // Plus 2 for doubleNewLine at end;

            if (valueHasNewLine)
            {
                lines += 1;
            }

            Vec2f additionalLabelSize = new Vec2f(0f, lines * ((label.Font.LineHeight + label.LineSpacing) * label.FontScale));

            label.Size += additionalLabelSize;

            int additionalLabelCapacity = input.Length + value.Length + 10; // A little over most likely

            label.Open(additionalLabelCapacity);
            //label.Append(inputTerminator);
            label.Append(input);

            if (hasReturnValue)
            {
                label.Append(": ");
            }

            if (valueHasNewLine)
            {
                label.Append(NewLine);
            }

            label.Append(value);
            label.Append(DoubleNewLine);
            label.Append(inputTerminator);
            label.Close();

            inputField.Text = Utils.EmptyString;

            float currentLabelTextWidth = label.Measure(label.Text).X;

            if (currentLabelTextWidth > label.Size.X)
            {
                label.Size = new Vec2f(currentLabelTextWidth + scrollPane.ComputeScrollbarTrackSize() * 19f, label.Size.Y);
            }

            scrollPane.Layout();
        }

        // Returns true if it can continue on with the ScriptInterpreter.
        // This happens if the input is empty, is not a command, or is a command and was successfully fully parsed.
        // Will return false if it found a command, but there was a parsing error somewhere along the line.
        private static bool TryCommand(string text, ImmutableArray<Command> commands, HelpItemDictionary items, out string transformedCommand)
        {
            CommandParser.CommandResult result = CommandParser.Parse(text, commands, items, out transformedCommand);

            if (result == CommandParser.CommandResult.IsCommandButError)
            {
                return false;
            }

            return true;
        }

        private void SetUITints()
        {
            // output

            scrollPane.Style.RenderData.SetColor(OutputTint);
            label.Style.TextNormal.FontColor = OutputTextTint;

            // input

            inputField.Style.Active.SetColor(InputTint);
            inputField.Style.TextNormal.FontColor = InputTextTint;
            inputField.Style.CaretTint = InputTextTint;
        }

        private void CheckAndSetBounds()
        {
            Vec2f size = GetConsoleSize();
            Vec2f position = GetConsolePosition(size);

            if (mainGroup.Position != position || mainGroup.Size != size)
            {
                mainGroup.Position = position;
                mainGroup.Size = size;
            }

            Vec2f fieldSize = GetConsoleInputSize(size);
            Vec2f fieldPosition = GetConsoleInputPosition(position, size, fieldSize);

            if (inputField.Position != fieldPosition || inputField.Size != fieldSize)
            {
                inputField.Position = fieldPosition;
                inputField.Size = fieldSize;
            }
        }

        private Vec2f GetConsolePosition(Vec2f size)
        {
            return (Vec2f)game.Geo.Display.WindowSize - size;
        }

        private Vec2f GetConsoleSize()
        {
            return new Vec2f(game.Geo.Display.WindowWidth, game.Geo.Display.WindowHeight * 0.25f);
        }

        private Vec2f GetConsoleInputSize(Vec2f consoleSize)
        {
            return new Vec2f(consoleSize.X, consoleSize.Y * 0.125f);
        }

        private Vec2f GetConsoleInputPosition(Vec2f consolePosition, Vec2f consoleSize, Vec2f inputSize)
        {
            return consolePosition + consoleSize - inputSize;
        }

        private Vec2f GetConsoleScrollPaneSize(Vec2f consoleSize, Vec2f inputSize)
        {
            return new Vec2f(consoleSize.X, consoleSize.Y - inputSize.Y);
        }

        private Vec2f GetConsoleScrollPanePosition(Vec2f consolePosition)
        {
            return consolePosition;
        }

        private Vec2f GetConsoleLabelSize(UIManager uiManager, float targetFontScale)
        {
            float width = scrollPane.Size.X;
            float height;

            if (label is null)
            {
                height = uiManager.MainFont.LineHeight * targetFontScale;
            }
            else
            {
                height = label.Font.LineHeight * targetFontScale;
            }

            return new Vec2f(width, height);
        }
    }
}
