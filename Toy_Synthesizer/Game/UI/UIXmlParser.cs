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

namespace Toy_Synthesizer.Game.UI
{
    public class UIXmlParser
    {
        public static ViewableList<Widget> Parse(UIManager uiManager, string xmlContent, GroupWidget rootParent = null)
        {
            UIXmlParser parser = new UIXmlParser(uiManager);

            return parser.Parse(xmlContent, rootParent);
        }

        private readonly Geo geo;
        private readonly UIManager uiManager;

        private readonly ViewableList<string> knownWidgetTypeNames;

        private readonly ViewableList<TypeFactory> additionalTypeFactories;

        public UIXmlParser(UIManager uiManager)
        {
            this.uiManager = uiManager;

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
                "Drawer"
            };

            additionalTypeFactories = new ViewableList<TypeFactory>(1000);
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

        public ViewableList<Widget> Parse(string xmlContent, 
                                          GroupWidget rootParent = null)
        {
            XDocument doc = XDocument.Parse(xmlContent);

            ViewableList<Widget> rootWidgets = new ViewableList<Widget>();

            if (doc.Root is null)
            {
                return rootWidgets;
            }

            // Handle case where root is just a container tag <Layout> vs. a specific widget.

            if (IsWidgetName(doc.Root.Name.LocalName))
            {
                Widget w = ParseElement(doc.Root, parent: rootParent);

                if (w is not null)
                {
                    rootWidgets.Add(w);
                }
            }
            else
            {
                foreach (XElement element in doc.Root.Elements())
                {
                    Widget w = ParseElement(element, parent: rootParent);

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

        private Widget ParseElement(XElement element, Widget parent)
        {
            Widget widget = CreateWidget(element, element.Name.LocalName, parent);

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
                        Widget childWidget = ParseElement(childElement, widget);

                        if (childWidget is not null)
                        {
                            group.AddChild(childWidget);
                        }
                    }
                }
            }

            return widget;
        }

        private Widget CreateWidget(XElement element, string typeName, Widget parent)
        {
            ViewableList<XAttribute> attributes = new ViewableList<XAttribute>(element.Attributes());

            (Vec2f position, Vec2f size) = GetBounds(attributes, parent);

            TypeFactory additionalTypeFactory = additionalTypeFactories.Find(factory => factory.TypeName.Equals(typeName));

            Widget widget;

            if (additionalTypeFactory is not null)
            {
                widget = additionalTypeFactory.Create(uiManager, position, size, attributes);
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

        private static AABB GetBounds(ViewableList<XAttribute> attributes, Widget parent)
        {
            TryGetPosition(attributes, out Vec2f? positionNullable, out PositionMode positionMode, out bool xIsPercent, out bool yIsPercent);
            TryGetSize(attributes, out Vec2f? sizeNullable, out SizeMode sizeMode, out bool widthIsPercent, out bool heightIsPercent);

            Vec2f position = positionNullable.GetValueOrDefault(Vec2f.Zero);
            Vec2f size = sizeNullable.GetValueOrDefault(Vec2f.Zero);

            if (xIsPercent || yIsPercent || widthIsPercent || heightIsPercent)
            {
                AABB baseBounds = parent is null ? (AABB)Geo.Instance.Display.WindowBounds : parent.GetBoundsAABB();

                if (xIsPercent && heightIsPercent && positionMode != PositionMode.None)
                {
                    switch (positionMode)
                    {
                        case PositionMode.Min:
                            position = baseBounds.Position + (baseBounds.Size.Min() * GeoMath.PercentToScalar(position));
                            break;

                        case PositionMode.Max:
                            position = baseBounds.Position + (baseBounds.Size.Max() * GeoMath.PercentToScalar(position));
                            break;

                        default: throw new InvalidOperationException($"Invalid PositionMode: \"{positionMode}\".");
                    }
                }
                else
                {
                    if (xIsPercent)
                    {
                        position.X = baseBounds.Position.X + (baseBounds.Size.X * GeoMath.PercentToScalar(position.X));
                    }

                    if (yIsPercent)
                    {
                        position.Y = baseBounds.Position.Y + (baseBounds.Size.Y * GeoMath.PercentToScalar(position.Y));
                    }
                }

                if (widthIsPercent && heightIsPercent && sizeMode != SizeMode.None)
                {
                    switch (sizeMode)
                    {
                        case SizeMode.Min:
                            size = baseBounds.Size.Min() * GeoMath.PercentToScalar(size);
                            break;

                        case SizeMode.Max:
                            size = baseBounds.Size.Max() * GeoMath.PercentToScalar(size);
                            break;

                        default: throw new InvalidOperationException($"Invalid SizeMode: \"{sizeMode}\".");
                    }
                }
                else
                {
                    if (widthIsPercent)
                    {
                        size.X = baseBounds.Size.X * GeoMath.PercentToScalar(size.X);
                    }

                    if (heightIsPercent)
                    {
                        size.Y = baseBounds.Size.Y * GeoMath.PercentToScalar(size.Y);
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

            return TryGetVec2f(attributes, AllowedPositionAttrNames, AllowedPositionXAttrNames, AllowedPositionYAttrNames,
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

            return TryGetVec2f(attributes, AllowedSizeAttrNames, AllowedWidthAttrNames, AllowedHeightAttrNames, 
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

            /*for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    value = ParseFloat(attributeValue, out isPercent);

                    return true;
                }
            }

            value = 0f;
            isPercent = false;

            return false;*/
        }

        public static bool TryGetDouble(ViewableList<XAttribute> attributes, string name,
                                        out double value,
                                        out bool isPercent)
        {
            return TryGetNumber<double>(attributes, name, out value, out isPercent);

            /*for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    value = ParseDouble(attributeValue, out isPercent);

                    return true;
                }
            }

            value = 0.0;
            isPercent = false;

            return false;*/
        }

        public static bool TryGetInt(ViewableList<XAttribute> attributes, string name, out int value)
        {
            return TryGetNumber<int>(attributes, name, out value, out _);

            /*for (int index = 0; index < attributes.Count; index++)
            {
                XAttribute attribute = attributes[index];

                string attributeName = attribute.Name.LocalName.ToLower();
                string attributeValue = attribute.Value;

                if (attributeName == name)
                {
                    attributes.RemoveAt(index);

                    value = ParseInt(attributeValue);

                    return true;
                }
            }

            value = 0;

            return false;*/
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

        public static readonly ReadOnlyMemory<string> AllowedPositionAttrNames;
        public static readonly ReadOnlyMemory<string> AllowedPositionXAttrNames;
        public static readonly ReadOnlyMemory<string> AllowedPositionYAttrNames;

        public static readonly ReadOnlyMemory<string> AllowedSizeAttrNames;
        public static readonly ReadOnlyMemory<string> AllowedWidthAttrNames;
        public static readonly ReadOnlyMemory<string> AllowedHeightAttrNames;

        static UIXmlParser()
        {
            AllowedPositionAttrNames = new string[] { "position" };
            AllowedPositionXAttrNames = new string[] { "x" };
            AllowedPositionYAttrNames = new string[] { "y" };

            AllowedSizeAttrNames = new string[] { "size" };
            AllowedWidthAttrNames = new string[] { "width", "w" };
            AllowedHeightAttrNames = new string[] { "height", "h" };
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

            public abstract Widget Create(UIManager uiManager, Vec2f position, Vec2f size, ViewableList<XAttribute> attributes);
        }
    }
}