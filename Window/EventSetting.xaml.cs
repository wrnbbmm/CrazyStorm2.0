﻿/*
 * The MIT License (MIT)
 * Copyright (c) StarX 2015 
 */
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CrazyStorm.Core;
using CrazyStorm.Expression;

namespace CrazyStorm
{
    /// <summary>
    /// Interaction logic for EventSetting.xaml
    /// </summary>
    public partial class EventSetting : Window
    {
        #region Private Members
        EventGroup eventGroup;
        Expression.Environment environment;
        IList<FileResource> sounds;
        IList<ParticleType> types;
        bool isPlaySound;
        bool isEditing;
        bool isExpressionResult;
        DockPanel editingPanel;
        IOrderedEnumerable<VariableComboBoxItem> sortedVaraibles;
        #endregion

        #region Constructor
        public EventSetting(EventGroup eventGroup, Expression.Environment environment,
            IList<FileResource> sounds, IList<ParticleType> types, bool emitter, bool aboutParticle)
        {
            this.eventGroup = eventGroup;
            this.environment = environment;
            this.sounds = sounds;
            this.types = types;
            InitializeComponent();
            InitializeSetting(emitter, aboutParticle);
            LoadContent();
            TranslateEvents();
        }
        #endregion

        #region Private Methods
        void InitializeSetting(bool emitter, bool aboutParticle)
        {
            GroupBox.DataContext = eventGroup;
            EventList.ItemsSource = eventGroup.TranslatedEvents;
            EmitParticle.Visibility = emitter ? Visibility.Visible : Visibility.Collapsed;
            ChangeType.Visibility = aboutParticle ? Visibility.Visible : Visibility.Collapsed;
        }
        void LoadContent()
        {
            //Load properties.
            foreach (var property in environment.Properties)
            {
                var item = new VariableComboBoxItem();
                item.Name = property.Key;
                string[] split = property.Key.Split('.');
                var displayName = (string)TryFindResource(split[0] + "Str");
                if (displayName != null && split.Length > 1)
                    displayName += "." + split[1];

                item.DisplayName = displayName != null ? displayName : property.Key;
                LeftConditionComboBox.Items.Add(item);
                RightConditionComboBox.Items.Add(item);
                PropertyComboBox.Items.Add(item);
            }
            //Load locals.
            foreach (var local in environment.Locals)
            {
                var item = new VariableComboBoxItem();
                item.Name = local.Key;
                item.DisplayName = item.Name;
                LeftConditionComboBox.Items.Add(item);
                RightConditionComboBox.Items.Add(item);
                PropertyComboBox.Items.Add(item);
            }
            //Load globals.
            foreach (var global in environment.Globals)
            {
                var item = new VariableComboBoxItem();
                item.Name = global.Key;
                item.DisplayName = item.Name;
                LeftConditionComboBox.Items.Add(item);
                RightConditionComboBox.Items.Add(item);
                PropertyComboBox.Items.Add(item);
            }
            //Load sounds.
            foreach (FileResource sound in sounds)
            {
                if (sound.IsValid)
                    SoundCombo.Items.Add(sound);
            }
            //Load particle types.
            //First needs to merge repeated type name.
            var typesNorepeat = new List<ParticleType>();
            foreach (var item in types)
            {
                bool exist = false;
                for (int i = 0; i < typesNorepeat.Count; ++i)
                    if (item.Name == typesNorepeat[i].Name)
                    {
                        exist = true;
                        break;
                    }

                if (!exist)
                    typesNorepeat.Add(item);
            }
            TypeCombo.ItemsSource = typesNorepeat;
            //Create sorted variable list for translating event
            var varaibleList = new List<VariableComboBoxItem>();
            foreach (VariableComboBoxItem item in PropertyComboBox.Items)
                varaibleList.Add(item);
            //Longer name first
            sortedVaraibles = varaibleList.OrderByDescending(s => s.Name.Length);
        }
        string TranslateEvent(string originalEvent)
        {
            return originalEvent;
            //TODO
            //string translatedEvent = "";
            ////Translate variable name
            //if (string.IsNullOrWhiteSpace(translatedEvent))
            //    translatedEvent = originalEvent;

            //foreach (VariableComboBoxItem item in sortedVaraibles)
            //{
            //    if (originalEvent.Contains(item.Name))
            //        translatedEvent = translatedEvent.Replace(item.Name, item.DisplayName);
            //}
            ////Translate keyword
            //if (string.IsNullOrWhiteSpace(translatedEvent))
            //    translatedEvent = originalEvent;

            //string[] keywords = {"Linear", "Accelerated", "Decelerated", "Fixed", "ChangeTo", "Increase", "Decrease",
            //                    "EmitParticle", "PlaySound", "Loop", "ChangeType"};
            //foreach (string item in keywords)
            //{
            //    if (originalEvent.Contains(item))
            //        translatedEvent = translatedEvent.Replace(item, (string)FindResource(item + "Str"));
            //}
            //return translatedEvent;
        }
        void TranslateEvents()
        {
            if (eventGroup.TranslatedEvents.Count != 0)
                return;

            foreach (string originalEvent in eventGroup.OriginalEvents)
                eventGroup.TranslatedEvents.Add(TranslateEvent(originalEvent));
        }
        void ChangeTextBoxState(TextBox source, bool hasError)
        {
            if (hasError)
            {
                var tip = new ToolTip();
                var tipText = new TextBlock();
                tipText.Text = (string)FindResource("ValueInvalidStr");
                tip.Content = tipText;
                source.ToolTip = tip;
                source.Background = new SolidColorBrush(Color.FromRgb(255, 190, 190));
            }
            else
            {
                source.ToolTip = null;
                source.Background = new SolidColorBrush(Colors.White);
            }
        }
        bool SetConditionInfo(EventInfo eventInfo)
        {
            //Check if there have errors
            if (LeftValue.ToolTip != null || RightValue.ToolTip != null)
                return false;

            if (LeftLessThan.IsChecked == true)
                eventInfo.leftOperator = "<";
            else if (LeftEqual.IsChecked == true)
                eventInfo.leftOperator = "=";
            else if (LeftMoreThan.IsChecked == true)
                eventInfo.leftOperator = ">";

            if (LeftConditionComboBox.SelectedItem != null && !String.IsNullOrEmpty(LeftValue.Text) &&
                !String.IsNullOrEmpty(eventInfo.leftOperator))
            {
                var selectedItem = LeftConditionComboBox.SelectedItem as VariableComboBoxItem;
                eventInfo.leftCondition = selectedItem.Name;
                eventInfo.leftType = GetValueType(selectedItem.Name);
                eventInfo.leftValue = LeftValue.Text;
            }
            if (RightLessThan.IsChecked == true)
                eventInfo.rightOperator = "<";
            else if (RightEqual.IsChecked == true)
                eventInfo.rightOperator = "=";
            else if (RightMoreThan.IsChecked == true)
                eventInfo.rightOperator = ">";

            if (RightConditionComboBox.SelectedItem != null && !String.IsNullOrEmpty(RightValue.Text) &&
                !String.IsNullOrEmpty(eventInfo.rightOperator))
            {
                var selectedItem = RightConditionComboBox.SelectedItem as VariableComboBoxItem;
                eventInfo.rightCondition = selectedItem.Name;
                eventInfo.rightType = GetValueType(selectedItem.Name);
                eventInfo.rightValue = RightValue.Text;
            }
            //Allow empty condition
            if (String.IsNullOrEmpty(eventInfo.leftCondition) && String.IsNullOrEmpty(eventInfo.rightCondition))
                return true;

            if (String.IsNullOrEmpty(eventInfo.leftCondition) && !String.IsNullOrEmpty(eventInfo.rightCondition))
            {
                eventInfo.leftCondition = eventInfo.rightCondition;
                eventInfo.rightCondition = null;
                eventInfo.leftOperator = eventInfo.rightOperator;
                eventInfo.rightOperator = null;
                eventInfo.leftType = eventInfo.rightType;
                eventInfo.rightType = PropertyType.IllegalType;
                eventInfo.leftValue = eventInfo.rightValue;
                eventInfo.rightValue = null;

            }
            if (!String.IsNullOrEmpty(eventInfo.leftCondition) && !String.IsNullOrEmpty(eventInfo.rightCondition))
            {
                if (And.IsChecked == true)
                    eventInfo.midOperator = "&";
                else if (Or.IsChecked == true)
                    eventInfo.midOperator = "|";
            }
            eventInfo.hasCondition = true;
            return true;
        }
        bool BuildEvent(out string text)
        {
            text = string.Empty;
            var eventInfo = new EventInfo();
            //Check if there have errors
            if (ResultValue.ToolTip != null || ChangeTime.ToolTip != null || ExecuteTime.ToolTip != null)
                return false;

            if (!SetConditionInfo(eventInfo))
                return false;

            if (ChangeTo.IsChecked == true)
                eventInfo.changeType = "ChangeTo";
            else if (Increase.IsChecked == true)
                eventInfo.changeType = "Increase";
            else if (Decrease.IsChecked == true)
                eventInfo.changeType = "Decrease";

            if (Linear.IsChecked == true)
                eventInfo.changeMode = "Linear";
            else if (Accelerated.IsChecked == true)
                eventInfo.changeMode = "Accelerated";
            else if (Decelerated.IsChecked == true)
                eventInfo.changeMode = "Decelerated";
            else if (Fixed.IsChecked == true)
                eventInfo.changeMode = "Fixed";

            if (PropertyComboBox.SelectedItem != null && !String.IsNullOrEmpty(ResultValue.Text) &&
                !String.IsNullOrEmpty(eventInfo.changeType) && !String.IsNullOrEmpty(eventInfo.changeMode) && 
                !String.IsNullOrEmpty(ChangeTime.Text))
            {
                var selectedItem = PropertyComboBox.SelectedItem as VariableComboBoxItem;
                eventInfo.property = selectedItem.Name;
                eventInfo.isExpressionResult = isExpressionResult;
                eventInfo.resultType = GetValueType(selectedItem.Name);
                eventInfo.resultValue = ResultValue.Text;
                eventInfo.changeTime = ChangeTime.Text;
                if (!String.IsNullOrEmpty(ExecuteTime.Text))
                    eventInfo.executeTime = ExecuteTime.Text;
            }
            else
                return false;

            text = EventHelper.BuildEvent(eventInfo);
            return true;
        }
        PropertyType GetValueType(string name)
        {
            object value = environment.GetProperty(name);
            if (value == null)
                value = environment.GetLocal(name);

            if (value == null)
                value = environment.GetGlobal(name);

            return PropertyTypeRule.GetValueType(value);
        }
        bool BuildSpecialEvent(out string text)
        {
            text = string.Empty;
            var eventInfo = new EventInfo();
            if (!SetConditionInfo(eventInfo))
                return false;

            if (EmitParticle.IsChecked == true)
            {
                eventInfo.specialEvent = "EmitParticle";
                eventInfo.arguments = string.Empty;
            }
            else if (PlaySound.IsChecked == true)
            {
                if (SoundCombo.SelectedItem == null)
                    return false;

                eventInfo.specialEvent = "PlaySound";
                eventInfo.arguments = SoundCombo.SelectedItem + ", " + VolumeSlider.Value;
            }
            else if (Loop.IsChecked == true)
            {
                //Check if there have errors
                if (LoopTime.ToolTip != null || StopCondition.ToolTip != null)
                    return false;

                if (String.IsNullOrEmpty(LoopTime.Text) && String.IsNullOrEmpty(StopCondition.Text))
                    return false;

                string arguments = string.Empty;
                if (!String.IsNullOrEmpty(LoopTime.Text) && !String.IsNullOrEmpty(StopCondition.Text))
                    arguments = LoopTime.Text + ", " + StopCondition.Text;
                else if (String.IsNullOrEmpty(LoopTime.Text) && !String.IsNullOrEmpty(StopCondition.Text))
                    arguments = "0, " + StopCondition.Text;
                else
                    arguments = LoopTime.Text;

                eventInfo.specialEvent = "Loop";
                eventInfo.arguments = arguments;
            }
            else if (ChangeType.IsChecked == true)
            {
                if (TypeCombo.SelectedItem == null || ColorCombo.SelectedItem == null)
                    return false;

                eventInfo.specialEvent = "ChangeType";
                eventInfo.arguments = TypeCombo.SelectedItem + ", " + ((ComboBoxItem)ColorCombo.SelectedItem).Content;
            }
            else
                return false;

            eventInfo.isSpecialEvent = true;
            text = EventHelper.BuildEvent(eventInfo);
            return true;
        }
        void ResetAll()
        {
            LeftConditionComboBox.SelectedIndex = -1;
            LeftMoreThan.IsChecked = false;
            LeftEqual.IsChecked = false;
            LeftLessThan.IsChecked = false;
            LeftValue.Text = string.Empty;
            And.IsChecked = false;
            Or.IsChecked = false;
            RightConditionComboBox.SelectedIndex = -1;
            RightMoreThan.IsChecked = false;
            RightEqual.IsChecked = false;
            RightLessThan.IsChecked = false;
            RightValue.Text = string.Empty;
            PropertyComboBox.SelectedIndex = -1;
            ChangeTo.IsChecked = false;
            Increase.IsChecked = false;
            Decrease.IsChecked = false;
            ResultValue.Text = string.Empty;
            Linear.IsChecked = false;
            Accelerated.IsChecked = false;
            Decelerated.IsChecked = false;
            Fixed.IsChecked = false;
            ChangeTime.Text = string.Empty;
            ExecuteTime.Text = string.Empty;
            EmitParticle.IsChecked = false;
            PlaySoundPanel.Visibility = Visibility.Collapsed;
            PlaySound.IsChecked = false;
            SoundCombo.SelectedIndex = -1;
            isPlaySound = false;
            SoundTestButton.Content = (string)FindResource("TestStr");
            VolumeSlider.Value = 50;
            LoopPanel.Visibility = Visibility.Collapsed;
            Loop.IsChecked = false;
            LoopTime.Text = string.Empty;
            StopCondition.Text = string.Empty;
            ChangeTypePanel.Visibility = Visibility.Collapsed;
            ChangeType.IsChecked = false;
            TypeCombo.SelectedIndex = -1;
            ColorCombo.SelectedIndex = -1;
        }
        void MapEventText(string text)
        {
            ResetAll();
            Dictionary<string, RadioButton> buttonMap = new Dictionary<string, RadioButton>();
            buttonMap[">"] = LeftMoreThan;
            buttonMap["="] = LeftEqual;
            buttonMap["<"] = LeftLessThan;
            buttonMap["&"] = And;
            buttonMap["|"] = Or;
            buttonMap["ChangeTo"] = ChangeTo;
            buttonMap["Increase"] = Increase;
            buttonMap["Decrease"] = Decrease;
            buttonMap["Linear"] = Linear;
            buttonMap["Accelerated"] = Accelerated;
            buttonMap["Decelerated"] = Decelerated;
            buttonMap["Fixed"] = Fixed;
            buttonMap["EmitParticle"] = EmitParticle;
            buttonMap["PlaySound"] = PlaySound;
            buttonMap["Loop"] = Loop;
            buttonMap["ChangeType"] = ChangeType;
            EventInfo eventInfo = EventHelper.SplitEvent(text);
            //Backfill condition
            if (eventInfo.hasCondition)
            {
                if (eventInfo.rightCondition != null)
                {
                    for (int i = 0; i < LeftConditionComboBox.Items.Count; ++i)
                    {
                        var item = LeftConditionComboBox.Items[i] as VariableComboBoxItem;
                        if (item.Name == eventInfo.leftCondition)
                            LeftConditionComboBox.SelectedIndex = i;
                    }
                    buttonMap[eventInfo.leftOperator].IsChecked = true;
                    LeftValue.Text = eventInfo.leftValue;
                    buttonMap[eventInfo.midOperator].IsChecked = true;
                    for (int i = 0; i < RightConditionComboBox.Items.Count; ++i)
                    {
                        var item = RightConditionComboBox.Items[i] as VariableComboBoxItem;
                        if (item.Name == eventInfo.rightCondition)
                            RightConditionComboBox.SelectedIndex = i;
                    }
                    buttonMap[">"] = RightMoreThan;
                    buttonMap["="] = RightEqual;
                    buttonMap["<"] = RightLessThan;
                    buttonMap[eventInfo.rightOperator].IsChecked = true;
                    RightValue.Text = eventInfo.rightValue;
                }
                else
                {
                    for (int i = 0; i < LeftConditionComboBox.Items.Count; ++i)
                    {
                        var item = LeftConditionComboBox.Items[i] as VariableComboBoxItem;
                        if (item.Name == eventInfo.leftCondition)
                            LeftConditionComboBox.SelectedIndex = i;
                    }
                    buttonMap[eventInfo.leftOperator].IsChecked = true;
                    LeftValue.Text = eventInfo.leftValue;
                }
            }
            //Backfill event
            if (!eventInfo.isSpecialEvent)
            {
                for (int i = 0; i < PropertyComboBox.Items.Count; ++i)
                {
                    var item = PropertyComboBox.Items[i] as VariableComboBoxItem;
                    if (item.Name == eventInfo.property)
                        PropertyComboBox.SelectedIndex = i;
                }
                isExpressionResult = eventInfo.isExpressionResult;
                buttonMap[eventInfo.changeType].IsChecked = true;
                ResultValue.Text = eventInfo.resultValue;
                buttonMap[eventInfo.changeMode].IsChecked = true;
                ChangeTime.Text = eventInfo.changeTime;
                if (eventInfo.executeTime != null)
                    ExecuteTime.Text = eventInfo.executeTime;
                //Prevent from setting special event
                SpecialEventPanel.IsEnabled = false;
            }
            else
            {
                buttonMap[eventInfo.specialEvent].IsChecked = true;
                string[] split = eventInfo.arguments.Split(',');
                if (eventInfo.specialEvent == "PlaySound")
                {
                    for (int i = 0; i < SoundCombo.Items.Count; ++i)
                    {
                        if ((SoundCombo.Items[i] as FileResource).Label == split[0])
                        {
                            SoundCombo.SelectedIndex = i;
                            break;
                        }
                    }
                    VolumeSlider.Value = int.Parse(split[1]);
                }
                else if (eventInfo.specialEvent == "Loop")
                {
                    LoopTime.Text = split[0] == "0" ? string.Empty : split[0];
                    if (split.Length > 1)
                    {
                        StopCondition.Text = split[1].Trim();
                    }
                }
                else if (eventInfo.specialEvent == "ChangeType")
                {
                    for (int i = 0; i < TypeCombo.Items.Count; ++i)
                    {
                        if ((TypeCombo.Items[i] as ParticleType).Name == split[0])
                        {
                            TypeCombo.SelectedIndex = i;
                            break;
                        }
                    }
                    if (TypeCombo.SelectedItem != null)
                    {
                        for (int i = 0; i < ColorCombo.Items.Count; ++i)
                        {
                            if ((string)(ColorCombo.Items[i] as ComboBoxItem).Content == split[1].Trim())
                            {
                                ColorCombo.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                //Prevent from setting property event
                PropertyEventPanel.IsEnabled = false;
            }
        }
        #endregion

        #region Window EventHandlers
        private void EventList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = VisualHelper.VisualUpwardSearch<ListViewItem>(e.OriginalSource as DependencyObject);
            EventList.SelectedItem = item;
        }
        private void Linear_Checked(object sender, RoutedEventArgs e)
        {
            Accelerated.IsChecked = false;
            Decelerated.IsChecked = false;
            Fixed.IsChecked = false;
        }
        private void Accelerated_Checked(object sender, RoutedEventArgs e)
        {
            Linear.IsChecked = false;
            Decelerated.IsChecked = false;
            Fixed.IsChecked = false;
        }
        private void Decelerated_Checked(object sender, RoutedEventArgs e)
        {
            Accelerated.IsChecked = false;
            Linear.IsChecked = false;
            Fixed.IsChecked = false;
        }
        private void Fixed_Checked(object sender, RoutedEventArgs e)
        {
            Accelerated.IsChecked = false;
            Decelerated.IsChecked = false;
            Linear.IsChecked = false;
        }
        private void EmitParticleButton_Checked(object sender, RoutedEventArgs e)
        {
            PlaySoundPanel.Visibility = Visibility.Collapsed;
            LoopPanel.Visibility = Visibility.Collapsed;
            ChangeTypePanel.Visibility = Visibility.Collapsed;
        }
        private void PlaySoundButton_Checked(object sender, RoutedEventArgs e)
        {
            PlaySoundPanel.Visibility = Visibility.Visible;
            LoopPanel.Visibility = Visibility.Collapsed;
            ChangeTypePanel.Visibility = Visibility.Collapsed;
        }
        private void LoopButton_Checked(object sender, RoutedEventArgs e)
        {
            PlaySoundPanel.Visibility = Visibility.Collapsed;
            LoopPanel.Visibility = Visibility.Visible;
            ChangeTypePanel.Visibility = Visibility.Collapsed;
        }
        private void ChangeTypeButton_Checked(object sender, RoutedEventArgs e)
        {
            PlaySoundPanel.Visibility = Visibility.Collapsed;
            LoopPanel.Visibility = Visibility.Collapsed;
            ChangeTypePanel.Visibility = Visibility.Visible;
        }
        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            string text;
            if (isEditing)
            {
                if (BuildEvent(out text))
                {
                    eventGroup.OriginalEvents[EventList.SelectedIndex] = text;
                    eventGroup.TranslatedEvents[EventList.SelectedIndex] = TranslateEvent(text);
                    editingPanel.Background = null;
                    EventList.IsEnabled = true;
                    AddEvent.Content = (string)FindResource("AddStr");
                    PropertyEventPanel.IsEnabled = true;
                    AddSpecialEvent.Content = AddEvent.Content;
                    SpecialEventPanel.IsEnabled = true;
                    isEditing = false;
                }
            }
            else if (BuildEvent(out text))
            {
                eventGroup.OriginalEvents.Add(text);
                eventGroup.TranslatedEvents.Add(TranslateEvent(text));
            }
        }
        private void AddSpecialEvent_Click(object sender, RoutedEventArgs e)
        {
            string text;
            if (isEditing)
            {
                if (BuildSpecialEvent(out text))
                {
                    eventGroup.OriginalEvents[EventList.SelectedIndex] = text;
                    eventGroup.TranslatedEvents[EventList.SelectedIndex] = TranslateEvent(text);
                    editingPanel.Background = null;
                    EventList.IsEnabled = true;
                    AddEvent.Content = (string)FindResource("AddStr");
                    PropertyEventPanel.IsEnabled = true;
                    AddSpecialEvent.Content = AddEvent.Content;
                    SpecialEventPanel.IsEnabled = true;
                    isEditing = false;
                }
            }
            else if (BuildSpecialEvent(out text))
            {
                eventGroup.OriginalEvents.Add(text);
                eventGroup.TranslatedEvents.Add(TranslateEvent(text));
            }
        }
        private void EditEvent_Click(object sender, RoutedEventArgs e)
        {
            editingPanel = (((e.OriginalSource as FrameworkElement).Parent as ContextMenu).PlacementTarget) as DockPanel;
            editingPanel.Background = SystemColors.HighlightBrush;
            MapEventText(eventGroup.OriginalEvents[EventList.SelectedIndex]);
            EventList.IsEnabled = false;
            AddEvent.Content = (string)FindResource("ModifyStr");
            AddSpecialEvent.Content = AddEvent.Content;
            isEditing = true;
        }
        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            var item = EventList.SelectedItem;
            if (item != null)
            {
                int index = eventGroup.OriginalEvents.IndexOf((string)item);
                eventGroup.OriginalEvents.RemoveAt(index);
                eventGroup.TranslatedEvents.RemoveAt(index);
                EventList.ItemsSource = eventGroup.TranslatedEvents;
            }
        }
        private void LeftConditionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = LeftConditionComboBox.SelectedItem as VariableComboBoxItem;
            if (selectedItem != null)
                LeftConditionComboBox.ToolTip = selectedItem.Name;
            else
                LeftConditionComboBox.ToolTip = null;

            LeftMoreThan.IsChecked = false;
            LeftEqual.IsChecked = false;
            LeftLessThan.IsChecked = false;
            LeftValue.Text = string.Empty;
            ChangeTextBoxState(LeftValue, false);
        }
        private void RightConditionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = RightConditionComboBox.SelectedItem as VariableComboBoxItem;
            if (selectedItem != null)
                RightConditionComboBox.ToolTip = selectedItem.Name;
            else
                RightConditionComboBox.ToolTip = null;

            RightMoreThan.IsChecked = false;
            RightEqual.IsChecked = false;
            RightLessThan.IsChecked = false;
            RightValue.Text = string.Empty;
            ChangeTextBoxState(RightValue, false);
        }
        private void PropertyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = PropertyComboBox.SelectedItem as VariableComboBoxItem;
            if (selectedItem != null)
                PropertyComboBox.ToolTip = selectedItem.Name;
            else
                PropertyComboBox.ToolTip = null;

            ChangeTo.IsChecked = false;
            Increase.IsChecked = false;
            Decrease.IsChecked = false;
            ResultValue.Text = string.Empty;
            ChangeTextBoxState(ResultValue, false);
        }
        private void LeftValue_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(LeftValue, false);
            LeftValue.Text = LeftValue.Text.Trim();
            string input = LeftValue.Text;
            if (String.IsNullOrEmpty(input))
                return;

            if (LeftConditionComboBox.SelectedItem != null)
            {
                var item = LeftConditionComboBox.SelectedItem as VariableComboBoxItem;
                object value = environment.GetProperty(item.Name);
                if (value != null)
                {
                    if (!PropertyTypeRule.TryParse(value, input, out value))
                    {
                        ChangeTextBoxState(LeftValue, true);
                        return;
                    }
                    LeftValue.Text = value.ToString();
                    return;
                }
                if (value == null)
                {
                    value = environment.GetLocal(item.Name);
                }
                if (value == null)
                {
                    value = environment.GetGlobal(item.Name);
                }
                if (value != null)
                {
                    float testValue;
                    if (!float.TryParse(input, out testValue))
                    {
                        ChangeTextBoxState(LeftValue, true);
                        return;
                    }
                }
            }
        }
        private void RightValue_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(RightValue, false);
            RightValue.Text = RightValue.Text.Trim();
            string input = RightValue.Text;
            if (String.IsNullOrEmpty(input))
                return;

            if (RightConditionComboBox.SelectedItem != null)
            {
                var item = RightConditionComboBox.SelectedItem as VariableComboBoxItem;
                object value = environment.GetProperty(item.Name);
                if (value != null)
                {
                    if (!PropertyTypeRule.TryParse(value, input, out value))
                    {
                        ChangeTextBoxState(RightValue, true);
                        return;
                    }
                    RightValue.Text = value.ToString();
                    return;
                }
                if (value == null)
                {
                    value = environment.GetLocal(item.Name);
                }
                if (value == null)
                {
                    value = environment.GetGlobal(item.Name);
                }
                if (value != null)
                {
                    float testValue;
                    if (!float.TryParse(input, out testValue))
                    {
                        ChangeTextBoxState(RightValue, true);
                        return;
                    }
                }
            }
        }
        private void ResultValue_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(ResultValue, false);
            ResultValue.Text = ResultValue.Text.Trim();
            string input = ResultValue.Text;
            if (String.IsNullOrEmpty(input))
                return;

            if (PropertyComboBox.SelectedItem != null)
            {
                try
                {
                    var item = PropertyComboBox.SelectedItem as VariableComboBoxItem;
                    object value = environment.GetProperty(item.Name);
                    if (value != null)
                    {
                        object output = null;
                        if (PropertyTypeRule.TryParse(value, input, out output))
                        {
                            ResultValue.Text = output.ToString();
                            isExpressionResult = false;
                            return;
                        }
                        var lexer = new Lexer();
                        lexer.Load(input);
                        var syntaxTree = new Parser(lexer).Expression();
                        if (syntaxTree is Number)
                            throw new ExpressionException();

                        var result = syntaxTree.Eval(environment);
                        if (!(PropertyTypeRule.IsMatchWith(value.GetType(), result.GetType())))
                            throw new ExpressionException();

                        isExpressionResult = true;
                        return;
                    }
                    if (value == null)
                    {
                        value = environment.GetLocal(item.Name);
                    }
                    if (value == null)
                    {
                        value = environment.GetGlobal(item.Name);
                    }
                    if (value != null)
                    {
                        //Fields of support struct must be float type.
                        var lexer = new Lexer();
                        lexer.Load(input);
                        var syntaxTree = new Parser(lexer).Expression();
                        var result = syntaxTree.Eval(environment);
                        if (!(result is float))
                            throw new ExpressionException();

                        isExpressionResult = true;
                    }
                }
                catch
                {
                    ChangeTextBoxState(ResultValue, true);
                }
            }
        }
        private void ChangeTime_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(ChangeTime, false);
            ChangeTime.Text = ChangeTime.Text.Trim();
            string input = ChangeTime.Text;
            if (String.IsNullOrEmpty(input))
                return;

            int value;
            if (!int.TryParse(input, out value))
                ChangeTextBoxState(ChangeTime, true);
            else if (value <= 0)
                ChangeTime.Text = "1";
        }
        private void ExecuteTime_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(ExecuteTime, false);
            ExecuteTime.Text = ExecuteTime.Text.Trim();
            string input = ExecuteTime.Text;
            if (String.IsNullOrEmpty(input))
                return;

            int value;
            if (!int.TryParse(input, out value))
                ChangeTextBoxState(ExecuteTime, true);
            else if (value < 0)
                ExecuteTime.Text = "0";
        }
        private void LoopTime_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(LoopTime, false);
            LoopTime.Text = LoopTime.Text.Trim();
            string input = LoopTime.Text;
            if (String.IsNullOrEmpty(input))
                return;

            int value;
            if (!int.TryParse(input, out value))
                ChangeTextBoxState(LoopTime, true);
            else if (value <= 0)
                LoopTime.Text = "1";
        }
        private void Condition_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(Condition, false);
            Condition.Text = Condition.Text.Trim();
            string input = Condition.Text;
            if (String.IsNullOrEmpty(input))
            {
                eventGroup.Condition = input;
                return;
            }
            try
            {
                var lexer = new Lexer();
                lexer.Load(input);
                var syntaxTree = new Parser(lexer).Expression();
                var result = syntaxTree.Eval(environment);
                if (!(result is bool))
                    throw new ExpressionException();
                eventGroup.Condition = input;
            }
            catch
            {
                ChangeTextBoxState(Condition, true);
            }
        }
        private void StopCondition_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ChangeTextBoxState(StopCondition, false);
            StopCondition.Text = StopCondition.Text.Trim();
            string input = StopCondition.Text;
            if (String.IsNullOrEmpty(input))
                return;

            try
            {
                var lexer = new Lexer();
                lexer.Load(input);
                var syntaxTree = new Parser(lexer).Expression();
                var result = syntaxTree.Eval(environment);
                if (!(result is bool))
                    throw new ExpressionException();
            }
            catch
            {
                ChangeTextBoxState(StopCondition, true);
            }
        }
        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeCombo.SelectedItem != null)
            {
                ColorCombo.Items.Clear();
                var selectedItem = TypeCombo.SelectedItem as ParticleType;
                foreach (var item in types)
                {
                    if (item.Name == selectedItem.Name)
                    {
                        var color = new ComboBoxItem();
                        color.Content = item.Color.ToString();
                        ColorCombo.Items.Add(color);
                    }
                }
            }
        }
        private void SoundTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (SoundCombo.SelectedItem != null)
            {
                isPlaySound = !isPlaySound;
                if (isPlaySound)
                {
                    SoundTestButton.Content = (string)FindResource("PauseStr");
                    MediaPlayer.Source = new Uri(((FileResource)SoundCombo.SelectedItem).AbsolutePath, UriKind.Absolute);
                    MediaPlayer.Volume = VolumeSlider.Value / 100;
                    MediaPlayer.LoadedBehavior = MediaState.Manual;
                    MediaPlayer.Play();
                }
                else
                {
                    SoundTestButton.Content = (string)FindResource("TestStr");
                    MediaPlayer.Stop();
                }
            }
        }
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            isPlaySound = false;
            SoundTestButton.Content = (string)FindResource("TestStr");
            MediaPlayer.Stop();
        }
        #endregion
    }
}
