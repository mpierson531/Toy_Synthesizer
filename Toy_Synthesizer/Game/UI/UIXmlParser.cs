using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

using GeoLib;
using GeoLib.GeoGraphics;
using GeoLib.GeoGraphics.UI;
using GeoLib.GeoGraphics.UI.Widgets;
using GeoLib.GeoMaths;
using GeoLib.GeoShapes;
using GeoLib.GeoUtils;
using GeoLib.GeoUtils.Collections;

using Microsoft.Xna.Framework;

using static Toy_Synthesizer.Game.UI.UIXmlParser;

namespace Toy_Synthesizer.Game.UI
{
    public class UIXmlParser
    {
        public static ViewableList<Widget> Parse(Game game, string xmlContent, GroupWidget rootParent = null)
        {
            UIXmlParser parser = new UIXmlParser(game);

            return parser.Parse(xmlContent, rootParent);
        }

        private readonly Game game;
        private readonly UIManager uiManager;

        private readonly ViewableList<string> knownWidgetTypeNames;

        private readonly ViewableList<TypeFactory> additionalTypeFactories;

        private readonly Dictionary<string, Dictionary<string, object>> enumTypeCache;

        public UIXmlParser(Game game)
        {
            this.game = game;
            this.uiManager = game.UIManager;

            knownWidgetTypeNames = new ViewableList<string> 
            {
                "TextButton",
                "Label", 
                "PlainLabel", 
                "TextField", 
                "Input", 
                "Checkbox", 
                "Window", 
                "ScrollPane", 
                "GroupWidget",
                "Slider",
                "Drawer",
                "DropDown",
                "DropDownListView" // DropDownListView should only be used with enums.
            };

            additionalTypeFactories = new ViewableList<TypeFactory>(1000);

            enumTypeCache = new Dictionary<string, Dictionary<string, object>>(1000);
        }

        public void AddTypeFactory(TypeFactory typeFactory)
        {
            if (additionalTypeFactories.Find(factory => factory == typeFactory || factory.TypeName.Equals(typeFactory.TypeName)) is not null)
            {
                throw new InvalidOperationException($"Already contained a factory for type \"{typeFactory.TypeName}\".");
            }

            additionalTypeFactories.Add(typeFactory);

            knownWidgetTypeNames.Add(typeFactory.TypeName);
        }

        // Parsing enum lists will not work if this method is not used for the type first.
        // Also only caches values that Enum.GetValues<T> returns.
        public void CacheEnumType<T>() where T : unmanaged, Enum
        {
            string typeName = typeof(T).Name;

            if (enumTypeCache.ContainsKey(typeName))
            {
                throw new InvalidOperationException($"Enum type \"{typeName}\" already cached.");
            }

            T[] values = Enum.GetValues<T>();

            Dictionary<string, object> valuesDictionary = new Dictionary<string, object>();

            for (int index = 0; index < values.Length; index++)
            {
                T value = values[index];

                valuesDictionary.Add(value.ToString(), value);
            }

            enumTypeCache.Add(typeName, valuesDictionary);
        }

        // At the moment, this only supports using all values in an enum type, rather than explicitly specified values.
        // If the type name of the enum is cached, this will return true, even for empty enums.
        public bool TryParseEnumList(ViewableList<XAttribute> attributes, out ViewableList<object> values)
        {
            if (!TryGetString(attributes, "typename", out string typeName))
            {
                values = null;

                return false;
            }

            if (!enumTypeCache.TryGetValue(typeName, out Dictionary<string, object> enumValues))
            {
                values = null;

                return false;
            }

            values = new ViewableList<object>(enumValues.Count);

            foreach (object value in enumValues.Values)
            {
                values.Add(value);
            }

            return true;
        }

        // This will return false if the property (under the name of propertyName) does not exist.
        // If propertyName exists and the list is not empty, this will return true.
        // See the other overload.
        public static bool TryParseStringList(ViewableList<XAttribute> attributes, string propertyName, out ViewableList<string> items)
        {
            if (!TryGetString(attributes, propertyName, out string list))
            {
                items = null;

                return false;
            }

            return TryParseStringList(list, out items);
        }

        // Lists of strings may begin with '[' or '{', and end with ']' or '}'. This is not required.
        // Items in the list should be separated by commas (',').
        // Instances of single or double quotes (''' or '"') will be included in each string item if it contains quotes.
        // If the returned list would be empty for any reason, this will return false and items will be set to null.
        public static bool TryParseStringList(string list, out ViewableList<string> items)
        {
            // TODO: I think something might be wrong here; come back.

            if (TextUtils.IsNullEmptyOrWhitespace(list))
            {
                items = null;

                return false;
            }

            list = list.Trim();

            if (!list.Contains(','))
            {
                if (list.Length > 0)
                {
                    if (list[0] == '[' || list[0] == '{')
                    {
                        list = list.Remove(0, 1);
                    }

                    if (list.Length > 0 && (list[list.Length - 1] == ']' || list[list.Length - 1] == '}'))
                    {
                        list = list.Remove(list.Length - 1);
                    }
                }

                if (TextUtils.IsNullEmptyOrWhitespace(list))
                {
                    items = null;

                    return false;
                }

                items = new ViewableList<string>(list);

                return true;
            }

            if (list.Length <= 2)
            {
                items = null;

                return false;
            }

            if (list[0] == '[' || list[0] == '{')
            {
                list = list.Remove(0, 1);
            }

            if (list[list.Length - 1] == ']' || list[list.Length - 1] == '}')
            {
                list = list.Remove(list.Length - 1);
            }

            if (TextUtils.IsNullEmptyOrWhitespace(list))
            {
                items = null;

                return false;
            }

            if (!list.Contains(','))
            {
                items = new ViewableList<string>(list);

                return true;
            }

            string[] itemsArray = list.Split(',');

            items = new ViewableList<string>(itemsArray);

            return true;
        }

        public ViewableList<Widget> Parse(string xmlContent, 
                                          GroupWidget rootParent = null,
                                          AABB? rootParentBaseBounds = null)
        {
            XDocument doc = XDocument.Parse(xmlContent);

            ViewableList<Widget> rootWidgets = new ViewableList<Widget>();

            if (doc.Root is null)
            {
                return rootWidgets;
            }

            if (!rootParentBaseBounds.HasValue && rootParent is not null)
            {
                rootParentBaseBounds = rootParent.GetBoundsAABB();
            }

            // Handle case where root is just a container tag <Layout> vs. a specific widget.

            if (IsWidgetName(doc.Root.Name.LocalName))
            {
                Widget w = ParseElement(doc.Root, baseBounds: rootParentBaseBounds);

                if (w is not null)
                {
                    rootWidgets.Add(w);
                }
            }
            else
            {
                foreach (XElement element in doc.Root.Elements())
                {
                    Widget w = ParseElement(element, baseBounds: rootParentBaseBounds);

                    if (w is not null)
                    {
                        rootWidgets.Add(w);
                    }
                }
            }

            if (rootParent is not null)
            {
                rootParent.AddChildRange(rootWidgets);
            }

            return rootWidgets;
        }

        private Widget ParseElement(XElement element, AABB? baseBounds = null)
        {
            Widget widget = CreateWidget(element, element.Name.LocalName, baseBounds);

            if (widget is null)
            {
                return null;
            }

            if (element.HasElements)
            {
                if (widget is GroupWidget group)
                {
                    foreach (var childElement in element.Elements())
                    {
                        Widget childWidget = ParseElement(childElement, widget.GetBoundsAABB());

                        if (childWidget is not null)
                        {
                            group.AddChild(childWidget);
                        }
                    }
                }
            }

            return widget;
        }

        private Widget CreateWidget(XElement element, string typeName, AABB? baseBounds)
        {
            ViewableList<XAttribute> attributes = new ViewableList<XAttribute>(element.Attributes());

            AABB baseBoundsNotNull = GetBounds(attributes, baseBounds);

            (Vec2f position, Vec2f size) = baseBoundsNotNull;

            TypeFactory additionalTypeFactory = additionalTypeFactories.Find(factory => factory.TypeName.Equals(typeName));

            Widget widget;

            if (additionalTypeFactory is not null)
            {
                widget = additionalTypeFactory.Create(game, uiManager, position, size, attributes);
            }
            else
            {
                widget = typeName switch
                {
                    "TextButton" => CreateTextButton(position, size, attributes),
                    "ImageButton" => CreateImageButton(position, size, attributes),
                    "Label" => CreateLabel(position, size, attributes),
                    "PlainLabel" => CreatePlainLabel(position, size, attributes),
                    "TextField" or "Input" => CreateTextField(position, size, attributes),
                    "Checkbox" => CreateCheckbox(position, size, attributes),
                    "Window" => CreateWindow(position, size, attributes),
                    "GroupWidget" => CreateGroupWidget(position, size, attributes),
                    "ScrollPane" => CreateScrollPane(position, size, attributes),
                    "Slider" => CreateNumberSlider(position, size, attributes),
                    "Drawer" => CreateDrawer(position, size, attributes),
                    "DropDown" => CreateDropDown(baseBoundsNotNull, attributes),
                    "DropDownListView" => CreateDropDownList(baseBoundsNotNull, attributes),

                    _ => throw new InvalidOperationException($"Unsupported UI widget type: \"{typeName}\".")
                };
            }

            ApplyAdditionalAttributes(uiManager, widget, attributes);

            return widget;
        }

        private TextButton CreateTextButton(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            TryGetString(attributes, "text", out string text);

            TryGetAlignment(attributes, out Alignment alignment);

            return uiManager.TextButton(position, size, text, alignment: alignment);
        }

        private ImageButton CreateImageButton(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            ImageButton.ImageButtonStyle style;

            if (!TryGetString(attributes, "style", out string styleName))
            {
                style = null;
            }
            else
            {
                style = uiManager.GetStyle<ImageButton.ImageButtonStyle>(styleName);
            }
    
            return uiManager.ImageButton(position, size, style);
        }

        private Label CreateLabel(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            TryGetString(attributes, "text", out string text);

            TryGetAlignment(attributes, out Alignment alignment);

            return uiManager.BackgroundedLabel(position, size, text, alignment);
        }

        private PlainLabel CreatePlainLabel(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            TryGetString(attributes, "text", out string text);

            TryGetAlignment(attributes, out Alignment alignment);

            if (!TryGetBool(attributes, "fittext", out bool fitText))
            {
                fitText = true;
            }

            if (!TryGetBool(attributes, "wraptext", out bool wrapText))
            {
                wrapText = false;
            }

            if (!TryGetBool(attributes, "growwithtext", out bool growWithText))
            {
                growWithText = false;
            }

            return uiManager.PlainLabel(position, size, text, 
                                        alignment: alignment, 
                                        fitText: fitText, 
                                        wrapText: wrapText, 
                                        growWithText: growWithText);
        }

        private TextField CreateTextField(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            if (!TryGetInt(attributes, "maxcharacters", out int maxCharacters))
            {
                maxCharacters = int.MaxValue;
            }

            TryGetNumberRangeAndDefaultValue(attributes,
                                             out NumberRange<double>? numberModeRange,
                                             out double numberModeDefaultValue);

            if (!TryGetBool(attributes, "numberallowfractional", out bool numberModeAllowFractional))
            {
                numberModeAllowFractional = true;
            }

            if (!TryGetInt(attributes, "numberallowedsign", out int numberModeAllowedSign))
            {
                numberModeAllowedSign = 0;
            }

            return uiManager.GeneralTextField(position, size, 
                                              maxCharacters: maxCharacters,
                                              numberModeRange: numberModeRange,
                                              numberModeAllowFractional: numberModeAllowFractional,
                                              numberModeAllowedSign: (sbyte)numberModeAllowedSign,
                                              numberModeDefaultValue: numberModeDefaultValue);
        }

        private Button CreateCheckbox(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            return uiManager.Checkbox(position, size);
        }

        // TODO: Implement support for title bar widgets here.
        private Window CreateWindow(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            TryGetString(attributes, "title", out string title);

            return uiManager.Window(position, size, title);
        }

        private GroupWidget CreateGroupWidget(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            return uiManager.GroupWithDefaultStyle(position, size);
        }

        private ScrollPane CreateScrollPane(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            return uiManager.ScrollPane(position, size);
        }

        private Slider CreateNumberSlider(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            if (!TryGetFloatNumberRangeAndDefaultValue(attributes, out NumberRange<float>? range, out float defaultValue))
            {
                throw new InvalidOperationException("Cannot create a number slider without a range and default value.");
            }

            if (!TryGetFloat(attributes, "dragincrement", out float dragIncrement, out _))
            {
                throw new InvalidOperationException("Can not create a number slider without a drag increment.");
            }

            return uiManager.Slider(position, size, defaultValue, range.Value, dragIncrement);
        }

        private Drawer CreateDrawer(Vec2f position, Vec2f size, ViewableList<XAttribute> attributes)
        {
            TryGetLayoutOrientation(attributes, out LayoutOrientation orientation, defaultOrientation: LayoutOrientation.Vertical);

            if (!TryGetString(attributes, "covertext", out string coverButtonText))
            {
                return uiManager.Drawer(position, size, orientation);
            }

            return uiManager.Drawer(position, size, orientation, coverButtonText);
        }

        // TODO: Implement CreateDropDown
        // TODO: Implement more properties for UX data for drop downs

        private DropDownWidget CreateDropDown(AABB bounds, ViewableList<XAttribute> attributes)
        {
            TryParseStringList(attributes, "items", out ViewableList<string> itemNames);

            string coverButtonText = GetDropDownCoverButtonTextOrDefaultValue(attributes, itemNames, defaultIndex: null);

            UIManager.DropDownUXData uxData = GetDropDownUXData(uiManager, attributes);

            return uiManager.DropDown(bounds.Position, bounds.Size, coverButtonText, itemNames?.Items, itemNames?.Count ?? 0, uxData: uxData);
        }

        private DropDownListView CreateDropDownList(AABB bounds, ViewableList<XAttribute> attributes)
        {
            TryParseEnumList(attributes, out ViewableList<object> values);

            bool hasDefaultIndex = TryGetInt(attributes, "defaultindex", out int defaultIndex);

            string coverButtonText = GetDropDownCoverButtonTextOrDefaultValue(attributes, values, !hasDefaultIndex ? 0 : defaultIndex);

            UIManager.DropDownUXData uxData = GetDropDownUXData(uiManager, attributes);

            return uiManager.DropDownList(bounds.Position, bounds.Size, values, defaultIndex, coverButtonText, uxData: uxData);
        }

        private static string GetDropDownCoverButtonTextOrDefaultValue<T>(ViewableList<XAttribute> attributes, ViewableList<T> values, int? defaultIndex)
        {
            if (!TryGetString(attributes, "coverbuttontext", out string coverButtonText))
            {
                if (values is not null && !values.IsEmpty)
                {
                    coverButtonText = values[defaultIndex ?? 0].ToString();
                }
            }

            return coverButtonText;
        }

        private static UIManager.DropDownUXData GetDropDownUXData(UIManager uiManager, ViewableList<XAttribute> attributes)
        {
            UIManager.DropDownUXData uxData = uiManager.GetScrollableDropDownUXData();

            if (TryGetBool(attributes, "usescrollpane", out bool useScrollPane))
            {
                uxData.UseScrollPane = useScrollPane;
            }

            bool hasDropDownPosition = TryGetVec2fValue(attributes,
                                                        DropDownPositionNames,
                                                        DropDownXPositionNames,
                                                        DropDownYPositionNames,
                                                        out Vec2fValue dropDownPosition);

            bool hasDropDownWidth = TryGetFloatValue(attributes, "dropdownwidth", out FloatValue dropDownWidth);

            if (hasDropDownPosition || hasDropDownWidth)
            {
                if (hasDropDownPosition)
                {
                    uxData.DropDownPosition = dropDownPosition;
                }

                if (hasDropDownWidth)
                {
                    uxData.DropDownWidth = dropDownWidth;
                }
            }

            if (TryGetFloatValue(attributes, "dropdownheightpadding", out FloatValue dropDownHeightPadding))
            {
                uxData.DropDownHeightPadding = dropDownHeightPadding;
            }

            if (TryGetFloatValue(attributes, "dropdownmaxheight", out FloatValue dropDownMaxHeight))
            {
                uxData.DropDownMaxHeight = dropDownMaxHeight;
            }

            if (TryGetVec2fValue(attributes,
                                 DropDownButtonStartPositionNames,
                                 DropDownButtonStartXNames,
                                 DropDownButtonStartYNames,
                                 out Vec2fValue buttonStartPosition))
            {
                uxData.ButtonStartPosition = buttonStartPosition;
            }

            if (TryGetVec2fValue(attributes,
                                 DropDownButtonSizeNames,
                                 DropDownButtonWidthNames,
                                 DropDownButtonHeightNames,
                                 out Vec2fValue buttonSize))
            {
                uxData.ButtonSize = buttonSize;
            }

            if (TryGetVec2fValue(attributes,
                                 DropDownButtonSpacingNames,
                                 DropDownButtonSpacingXNames,
                                 DropDownButtonSpacingYNames,
                                 out Vec2fValue buttonSpacing))
            {
                uxData.ButtonSpacing = buttonSpacing;
            }

            return uxData;
        }

        private bool IsWidgetName(string name)
        {
            return knownWidgetTypeNames.Contains(name);
        }

        public static bool TryGetFloatNumberRange(ViewableList<XAttribute> attributes,
                                                  out NumberRange<float>? range)
        {
            return TryGetNumberRange<float>(attributes, out range);
        }

        public static bool TryGetDoubleNumberRange(ViewableList<XAttribute> attributes,
                                                  out NumberRange<double>? range)
        {
            return TryGetNumberRange<double>(attributes, out range);
        }

        public static bool TryGetFloatNumberRangeAndDefaultValue(ViewableList<XAttribute> attributes,
                                                                 out NumberRange<float>? range,
                                                                 out float defaultValue)
        {
            return TryGetNumberRangeAndDefaultValue<float>(attributes, out range, out defaultValue);
        }

        public static bool TryGetDoubleNumberRangeAndDefaultValue(ViewableList<XAttribute> attributes,
                                                                  out NumberRange<double>? range,
                                                                  out double defaultValue)
        {
            return TryGetNumberRangeAndDefaultValue<double>(attributes, out range, out defaultValue);
        }

        public static bool TryGetNumberRange<T>(ViewableList<XAttribute> attributes, 
                                                out NumberRange<T>? range) where T : INumber<T>, IMinMaxValue<T>
        {
            bool succeded = TryGetNumber<T>(attributes, "numberminvalue", out T minValue, out _);

            if (!TryGetNumber<T>(attributes, "numbermaxvalue", out T maxValue, out _))
            {
                succeded = false;
            }

            if (!succeded)
            {
                range = null;

                return false;
            }

            range = NumberRange<T>.From(minValue, maxValue);

            return true;
        }

        public static bool TryGetNumberRangeAndDefaultValue<T>(ViewableList<XAttribute> attributes, 
                                                               out NumberRange<T>? range,
                                                               out T defaultValue) where T : INumber<T>, IMinMaxValue<T>
        {
            bool succeded = TryGetNumberRange<T>(attributes, out range);

            if (!TryGetNumber<T>(attributes, "numberdefaultvalue", out defaultValue, out _))
            {
                succeded = false;
            }

            return succeded;
        }

        private static AABB GetBounds(ViewableList<XAttribute> attributes, AABB? baseBounds)
        {
            TryGetPosition(attributes, out Vec2f? positionNullable, out PositionMode positionMode, out bool xIsPercent, out bool yIsPercent);
            TryGetSize(attributes, out Vec2f? sizeNullable, out SizeMode sizeMode, out bool widthIsPercent, out bool heightIsPercent);

            Vec2f position = positionNullable.GetValueOrDefault(Vec2f.Zero);
            Vec2f size = sizeNullable.GetValueOrDefault(Vec2f.Zero);

            return GetBoundsRaw(baseBounds, position, size, positionMode, sizeMode, xIsPercent, yIsPercent, widthIsPercent, heightIsPercent);
        }

        private static AABB GetBoundsRaw(AABB? baseBounds,
                                         Vec2f position, Vec2f size,
                                         PositionMode positionMode,
                                         SizeMode sizeMode,
                                         bool xIsPercent, bool yIsPercent, 
                                         bool widthIsPercent, bool heightIsPercent)
        {
            if (xIsPercent || yIsPercent || widthIsPercent || heightIsPercent)
            {
                AABB realBaseBounds = !baseBounds.HasValue ? (AABB)Geo.Instance.Display.WindowBounds : baseBounds.Value;

                if (xIsPercent && yIsPercent && positionMode != PositionMode.None)
                {
                    switch (positionMode)
                    {
                        case PositionMode.Min:
                            position = realBaseBounds.Position + (realBaseBounds.Size.Min() * GeoMath.PercentToScalar(position));
                            break;

                        case PositionMode.Max:
                            position = realBaseBounds.Position + (realBaseBounds.Size.Max() * GeoMath.PercentToScalar(position));
                            break;

                        default: throw new InvalidOperationException($"Invalid PositionMode: \"{positionMode}\".");
                    }
                }
                else
                {
                    if (xIsPercent)
                    {
                        position.X = realBaseBounds.Position.X + (realBaseBounds.Size.X * GeoMath.PercentToScalar(position.X));
                    }

                    if (yIsPercent)
                    {
                        position.Y = realBaseBounds.Position.Y + (realBaseBounds.Size.Y * GeoMath.PercentToScalar(position.Y));
                    }
                }

                if (widthIsPercent && heightIsPercent && sizeMode != SizeMode.None)
                {
                    switch (sizeMode)
                    {
                        case SizeMode.Min:
                            size = realBaseBounds.Size.Min() * GeoMath.PercentToScalar(size);
                            break;

                        case SizeMode.Max:
                            size = realBaseBounds.Size.Max() * GeoMath.PercentToScalar(size);
                            break;

                        default: throw new InvalidOperationException($"Invalid SizeMode: \"{sizeMode}\".");
                    }
                }
                else
                {
                    if (widthIsPercent)
                    {
                        size.X = realBaseBounds.Size.X * GeoMath.PercentToScalar(size.X);
                    }

                    if (heightIsPercent)
                    {
                        size.Y = realBaseBounds.Size.Y * GeoMath.PercentToScalar(size.Y);
                    }
                }
            }

            return new AABB(position, size);
        }

        private static bool TryGetPosition(ViewableList<XAttribute> attributes, 
                                           out Vec2f? position, 
                                           out PositionMode positionMode,
                                           out bool xIsPercent,
                                           out bool yIsPercent)
        {
            TryGetEnum<PositionMode>(attributes, "positionmode", out positionMode);

            return TryGetVec2f(attributes, PositionAttrNames, PositionXAttrNames, PositionYAttrNames,
                               out position,
                               out xIsPercent,
                               out yIsPercent);
        }

        private static bool TryGetSize(ViewableList<XAttribute> attributes,
                                       out Vec2f? size, 
                                       out SizeMode sizeMode,
                                       out bool xIsPercent,
                                       out bool yIsPercent)
        {
            TryGetEnum<SizeMode>(attributes, "sizemode", out sizeMode);

            return TryGetVec2f(attributes, SizeAttrNames, WidthAttrNames, HeightAttrNames, 
                               out size,
                               out xIsPercent,
                               out yIsPercent);
        }

        private static bool TryGetAlignment(ViewableList<XAttribute> attributes,
                                            out Alignment alignment, 
                                            Alignment defaultAlignment = Alignment.Left)
        {
            return TryGetEnum<Alignment>(attributes, "alignment", out alignment, defaultAlignment);
        }

        private static bool TryGetLayoutOrientation(ViewableList<XAttribute> attributes, 
                                                    out LayoutOrientation layoutOrientation, 
                                                    LayoutOrientation defaultOrientation = LayoutOrientation.Vertical)
        {
            return TryGetEnum<LayoutOrientation>(attributes, "orientation", out layoutOrientation, defaultOrientation);
        }

        public static bool TryGetVec2fValue(ViewableList<XAttribute> attributes,
                                            ReadOnlyMemory<string> allowedNames,
                                            ReadOnlyMemory<string> allowedXNames,
                                            ReadOnlyMemory<string> allowedYNames,
                                            out Vec2fValue value)
        {
            // Due to how Vec2fValue works, if x or y is percent, both have to be treated as percentages.

            if (!TryGetVec2f(attributes, allowedNames, allowedXNames, allowedYNames,
                             out Vec2f? rawValue, 
                             out bool xIsPercent, out bool yIsPercent))
            {
                value = default;

                return false;
            }

            if (xIsPercent || yIsPercent)
            {
                rawValue = GeoMath.PercentToScalar(rawValue.Value);

                value = Vec2fValue.Normalized(rawValue.Value);
            }
            else
            {
                value = Vec2fValue.Absolute(rawValue.Value);
            }

            return true;
        }

        public static bool TryGetFloatValue(ViewableList<XAttribute> attributes, string name, out FloatValue value)
        {
            if (!TryGetFloat(attributes, name, out float rawValue, out bool isPercent))
            {
                value = default;

                return false;
            }

            value = isPercent ? FloatValue.Normalized(GeoMath.PercentToScalar(rawValue)) : FloatValue.Absolute(rawValue);

            return true;
        }

        public static bool TryGetVec2f(ViewableList<XAttribute> attributes, 
                                       ReadOnlyMemory<string> allowedNames, 
                                       ReadOnlyMemory<string> allowedXNames, 
                                       ReadOnlyMemory<string> allowedYNames,
                                       out Vec2f? value,
                                       out bool xIsPercent,
                                       out bool yIsPercent)
        {
            value = null;

            float foundX = 0f;
            float foundY = 0f;

            xIsPercent = false;
            yIsPercent = false;

            bool isXFound = false;
            bool isYFound = false;

            for (int index = 0; index < attributes.Count; index++)
            {
                if (isXFound && isYFound)
                {
                    value = new Vec2f(foundX, foundY);

                    return true;
                }

                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (ArrayUtils.ArrayContainsEquatable(allowedXNames.Span, attributeName))
                {
                    foundX = ParseFloat(attributeValue, out xIsPercent);

                    isXFound = true;

                    attributes.RemoveAt(index);

                    index--;
                }
                else if (ArrayUtils.ArrayContainsEquatable(allowedYNames.Span, attributeName))
                {
                    foundY = ParseFloat(attributeValue, out yIsPercent);

                    isYFound = true;

                    attributes.RemoveAt(index);

                    index--;
                }
                else if (ArrayUtils.ArrayContainsEquatable(allowedNames.Span, attributeName))
                {
                    attributes.RemoveAt(index);

                    value = ParseVec2f(attributeValue, out xIsPercent, out yIsPercent);

                    return true;
                }
            }

            if (isXFound && isYFound)
            {
                value = new Vec2f(foundX, foundY);

                return true;
            }

            return false;
        }

        public static bool TryGetFloat(ViewableList<XAttribute> attributes, string name, 
                                       out float value,
                                       out bool isPercent)
        {
            return TryGetNumber<float>(attributes, name, out value, out isPercent);
        }

        public static bool TryGetDouble(ViewableList<XAttribute> attributes, string name,
                                        out double value,
                                        out bool isPercent)
        {
            return TryGetNumber<double>(attributes, name, out value, out isPercent);
        }

        public static bool TryGetInt(ViewableList<XAttribute> attributes, string name, out int value)
        {
            return TryGetNumber<int>(attributes, name, out value, out _);
        }

        public static bool TryGetNumber<T>(ViewableList<XAttribute> attributes, 
                                           string name, 
                                           out T value,
                                           out bool isPercent) where T : INumber<T>, IMinMaxValue<T>
        {
            for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    value = ParseNumber<T>(attributeValue, out isPercent);

                    return true;
                }
            }

            value = default;
            isPercent = false;

            return false;
        }

        public static bool TryGetString(ViewableList<XAttribute> attributes, string name, out string value)
        {
            for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    value = attributeValue;

                    return true;
                }
            }

            value = null;

            return false;
        }

        public static bool TryGetBool(ViewableList<XAttribute> attributes, string name, out bool value)
        {
            for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    return bool.TryParse(attributeValue, out value);
                }
            }

            value = false;

            return false;
        }

        public static bool TryGetEnum<T>(ViewableList<XAttribute> attributes, string name, 
                                         out T value,
                                         T defaultValue = default) where T: struct, Enum
        {
            if (!TryGetString(attributes, name, out string enumString))
            {
                value = defaultValue;

                return false;
            }

            if (!Enum.TryParse<T>(enumString, out T parsedValue))
            {
                value = defaultValue;

                return false;
            }

            value = parsedValue;

            return true;
        }

        private static void ApplyAdditionalAttributes(UIManager uiManager, Widget widget, ViewableList<XAttribute> attributes)
        {
            foreach (var attr in attributes)
            {
                // Name becomes lowercase here, so switch cases must be lowercase.
                string name = attr.Name.LocalName.ToLower();
                string value = attr.Value;

                switch (name)
                {
                    case "style":
                        uiManager.TrySetStyle(widget, value);
                        break;

                    case "isvisible":
                        widget.IsVisible = bool.Parse(value);
                        break;

                    case "scalex":
                        widget.Scale = new Vec2f(ParseFloat(value), widget.Scale.Y);
                        break;

                    case "scaley":
                        widget.Scale = new Vec2f(widget.Scale.X, ParseFloat(value));
                        break;

                    case "scale":
                        if (value.Contains(','))
                        {
                            widget.Scale = ParseVec2f(value);
                        }
                        else
                        {
                            float scale = ParseFloat(value);
                            widget.Scale = new Vec2f(scale, scale);
                        }

                        break;

                    case "fontscale":
                        SetFontScale(widget, ParseFloat(value));
                        break;

                    case "fittext":
                        SetFitText(widget, ParseBool(value));
                        break;

                    case "growwithtext":
                        SetGrowWithText(widget, ParseBool(value));
                        break;

                    case "maintainvisualscale":
                        SetMaintainVisualScale(widget, ParseBool(value));
                        break;

                    case "text":
                        SetText(widget, value);
                        break;

                    case "title":
                        SetTitle(widget, value);
                        break;

                    case "name":
                        SetName(widget, value);
                        break;

                    case "titlebarheight":
                        SetTitleBarHeight(widget, ParseFloat(value));
                        break;

                    case "tint":
                        widget.Tint = ParseHexColor(value);
                        break;

                    case "imageposition":
                        Vec2f imagePosition = ParseVec2f(value);
                        SetImagePosition(widget, imagePosition);
                        break;

                    case "imagesize":
                        Vec2f imageSize = ParseVec2f(value);
                        SetImageSize(widget, imageSize);
                        break;

                    case "scaleimageonscale":
                        SetScaleImageOnScale(widget, ParseBool(value));
                        break;

                    case "alignment":
                        SetAlignment(widget, ParseEnum<Alignment>(value));
                        break;
                }
            }
        }

        private static void SetText(Widget widget, string text)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.Text = text;
            }
        }

        private static void SetFontScale(Widget widget, float value)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.FontScale = value;
            }
        }

        private static void SetFitText(Widget widget, bool value)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.FitText = value;
            }
        }

        private static void SetGrowWithText(Widget widget, bool value)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.GrowWithText = value;
            }
        }

        private static void SetMaintainVisualScale(Widget widget, bool value)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.MaintainVisualScale = value;
            }
        }

        private static void SetTitle(Widget widget, string title)
        {
            if (widget is Window window)
            {
                window.Title = title;
            }
        }

        private static void SetName(Widget widget, string name)
        {
            widget.Name = name;
        }

        private static void SetTitleBarHeight(Widget widget, float value)
        {

        }

        private static void SetImagePosition(Widget widget, Vec2f value)
        {
            if (widget is ImageButton imageButton)
            {
                imageButton.ImagePosition = value;
            }
        }

        private static void SetImageSize(Widget widget, Vec2f value)
        {
            if (widget is ImageButton imageButton)
            {
                imageButton.ImageSize = value;
            }
        }

        private static void SetScaleImageOnScale(Widget widget, bool value)
        {
            if (widget is ImageButton imageButton)
            {
                imageButton.ScaleImageOnScale = value;
            }
        }

        private static void SetAlignment(Widget widget, Alignment value)
        {
            if (widget is ITextWidget textWidget)
            {
                textWidget.Alignment = value;
            }
            else if (widget is ImageButton imageButton)
            {
                imageButton.Alignment = value;
            }
        }

        private static T ParseNumber<T>(string value, out bool isPercent) where T : INumber<T>, IMinMaxValue<T>
        {
            isPercent = false;

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            value = value.Trim();

            if (value.EndsWith('%'))
            {
                isPercent = true;

                value = value.TrimEnd('%');
            }

            return GeoMath.ParseOrDefault<T>(value);
        }

        private static float ParseFloat(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }

            return 0f;
        }

        private static int ParseInt(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            return 0;
        }

        private static bool ParseBool(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            return false;
        }

        private static Vec2f ParseVec2f(string value)
        {
            string[] parts = value.Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length >= 1)
            {
                RemoveAllOfChar(parts, '(');
                RemoveAllOfChar(parts, ')');

                float x;
                float y;

                if (parts.Length >= 2)
                {
                    x = ParseFloat(parts[0]);
                    y = ParseFloat(parts[1]);
                }
                else
                {
                    x = ParseFloat(parts[0]);
                    y = x;
                }

                return new Vec2f(x, y);
            }

            return Vec2f.Zero;
        }

        private static float ParseFloat(string value, out bool isPercent)
        {
            isPercent = false;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0f;
            }

            value = value.Trim();

            if (value.EndsWith('%'))
            {
                isPercent = true;
                
                value = value.TrimEnd('%');
            }

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }

            return 0f;
        }

        private static double ParseDouble(string value, out bool isPercent)
        {
            isPercent = false;

            if (string.IsNullOrWhiteSpace(value))
            {
                return 0.0;
            }

            value = value.Trim();

            if (value.EndsWith('%'))
            {
                isPercent = true;

                value = value.TrimEnd('%');
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return 0.0;
        }

        private static Vec2f ParseVec2f(string value, out bool xIsPercent, out bool yIsPercent)
        {
            xIsPercent = false;
            yIsPercent = false;

            string cleanValue = value.Replace("(", "").Replace(")", "");
            string[] parts = cleanValue.Split(',', StringSplitOptions.TrimEntries);

            if (parts.Length >= 1)
            {
                float x;
                float y;

                if (parts.Length >= 2)
                {
                    x = ParseFloat(parts[0], out xIsPercent);
                    y = ParseFloat(parts[1], out yIsPercent);
                }
                else
                {
                    x = ParseFloat(parts[0], out xIsPercent);
                    y = x;
                    yIsPercent = xIsPercent;
                }

                return new Vec2f(x, y);
            }

            return Vec2f.Zero;
        }

        private static T ParseEnum<T>(string value, T defaultValue = default) where T : struct, Enum
        {
            if (!Enum.TryParse<T>(value, out T result))
            {
                result = defaultValue;
            }

            return result;
        }

        private static Color ParseHexColor(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            return GeoLib.Colors.FromHexOrDefault(hex, Color.White);
        }

        private static void Trim(string[] strings)
        {
            for (int index = 0; index < strings.Length; index++)
            {
                strings[index] = strings[index].Trim();
            }
        }

        private static void RemoveAllOfChar(string[] strings, char c)
        {
            string charString = c.ToString();

            for (int arrayIndex = 0; arrayIndex < strings.Length; arrayIndex++)
            {
                string s = strings[arrayIndex];

                strings[arrayIndex] = s.Replace(charString, TextUtils.EmptyString);
            }
        }

        public static readonly ReadOnlyMemory<string> PositionAttrNames;
        public static readonly ReadOnlyMemory<string> PositionXAttrNames;
        public static readonly ReadOnlyMemory<string> PositionYAttrNames;

        public static readonly ReadOnlyMemory<string> SizeAttrNames;
        public static readonly ReadOnlyMemory<string> WidthAttrNames;
        public static readonly ReadOnlyMemory<string> HeightAttrNames;

        public static readonly ReadOnlyMemory<string> DropDownPositionNames;
        public static readonly ReadOnlyMemory<string> DropDownXPositionNames;
        public static readonly ReadOnlyMemory<string> DropDownYPositionNames;

        public static readonly ReadOnlyMemory<string> DropDownButtonStartPositionNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonStartXNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonStartYNames;

        public static readonly ReadOnlyMemory<string> DropDownButtonSizeNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonWidthNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonHeightNames;

        public static readonly ReadOnlyMemory<string> DropDownButtonSpacingNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonSpacingXNames;
        public static readonly ReadOnlyMemory<string> DropDownButtonSpacingYNames;

        static UIXmlParser()
        {
            PositionAttrNames = new string[] { "position" };
            PositionXAttrNames = new string[] { "x" };
            PositionYAttrNames = new string[] { "y" };

            SizeAttrNames = new string[] { "size" };
            WidthAttrNames = new string[] { "width", "w" };
            HeightAttrNames = new string[] { "height", "h" };

            DropDownPositionNames = new string[] { "dropdownposition" };
            DropDownXPositionNames = ReadOnlyMemory<string>.Empty;
            DropDownYPositionNames = ReadOnlyMemory<string>.Empty;

            DropDownButtonStartPositionNames = new string[] { "buttonstartposition" };
            DropDownButtonStartXNames = new string[] { "buttonstartx" };
            DropDownButtonStartYNames = new string[] { "buttonstarty" };

            DropDownButtonSizeNames = new string[] { "buttonsize" };
            DropDownButtonWidthNames = new string[] { "buttonwidth" };
            DropDownButtonHeightNames = new string[] { "buttonheight" };

            DropDownButtonSpacingNames = new string[] { "buttonspacing" };
            DropDownButtonSpacingXNames = new string[] { "buttonspacingx" };
            DropDownButtonSpacingYNames = new string[] { "buttonspacingy" };
        }

        public enum SizeMode
        {
            None,

            Min,
            Max
        }

        public enum PositionMode
        {
            None,

            Min,
            Max
        }

        public abstract class TypeFactory
        {
            public readonly string TypeName;

            public TypeFactory(string typeName)
            {
                this.TypeName = typeName;
            }

            public abstract Widget Create(Game game, UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes);
        }
    }
}