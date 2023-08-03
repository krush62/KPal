/*
This file is part of the KPal distribution (https://github.com/krush62/KPal).
Copyright(c) 2023 Andreas Kruschinski.

This program is free software: you can redistribute it and/or modify  
it under the terms of the GNU General Public License as published by  
the Free Software Foundation, version 3.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
General Public License for more details.
You should have received a copy of the GNU General Public License 
long with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static KPal.SaveData;

namespace KPal
{
    public partial class MainWindow : Window
    {
        private byte RampCounter;
        private readonly List<PaletteEditor> PaletteEditorList;
        private readonly List<ColorLink> ColorLinkList;
        private readonly DispatcherTimer PreviewUpdateTimer;
        private string? SaveFileName;
        private bool IsDataSaved;
        private (Visualizer, Visualizer) Visualizers;

        public const string KPAL_TITLE = "KPal";
        private const string KPAL_FILE_FILTER = KPAL_TITLE + " file (*.kpal)|*.kpal";
        private readonly string EXPORT_FILTER;
        private readonly List<FileExporter.ExportFilter> EXPORT_FILTER_LIST;        
        private const uint MAX_RAMPS = 255;

        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(ColorNames.COLOR_FILE_NAME))
            {                
                _ = MessageBox.Show(string.Format(Properties.Resources.MainWindow_Warning_ColorList_Not_Found, ColorNames.COLOR_FILE_NAME), Properties.Resources.MainWindow_Warning_General_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            PaletteEditorList = new List<PaletteEditor>();
            ColorLinkList = new List<ColorLink>();
            PreviewUpdateTimer = new DispatcherTimer();
            PreviewUpdateTimer.Tick += PreviewUpdateTimer_Tick;
            PreviewUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            EXPORT_FILTER_LIST = FileExporter.GetAvailableFormats();
            EXPORT_FILTER = CreateExportFilter(EXPORT_FILTER_LIST);
            InitNew();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            bool doClose = true;
            if (!IsDataSaved)
            {
                MessageBoxResult mResult = MessageBox.Show(Properties.Resources.MainWindow_Msg_Discard_Message, Properties.Resources.MainWindow_Msg_Discard_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                doClose = (mResult == MessageBoxResult.OK);
            }
            e.Cancel = !doClose;
        }

        private void InitNew()
        {
            RampCounter = 0;
            ClearEntries();
            SetSaveFileName(null);
            SetNewVisualizer(true, Visualizers.Item1, Visualizer.VisualizerType.ShadingCube);
            SetNewVisualizer(false, Visualizers.Item2, Visualizer.VisualizerType.PaletteMerge);
            UpdatePreviews();
            SetDataSaved(true);
        }

        private void Visualizer_VisualizerSelectionChanged(object? sender, EventArgs e)
        {
            if (e is SelectionChangedEventArgs evArgs && evArgs.AddedItems.Count > 0 && evArgs.AddedItems[0] is Visualizer.VisualizerType type &&
                sender is Visualizer vis && type != vis.Type)
            {
                bool isLeft = vis == Visualizers.Item1;
                SetNewVisualizer(isLeft, vis, type);
            }
        }

        private void SetNewVisualizer(bool isLeft, Visualizer vis, Visualizer.VisualizerType type)
        {
            VisualizerGrid.Children.Remove(vis);
            vis = Visualizer.GetVisualizerForType(type);
            vis.VisualizerSelectionChanged += Visualizer_VisualizerSelectionChanged;
            Grid.SetRow(vis, 0);

            if (isLeft)
            {
                Visualizers.Item1 = vis;
                Grid.SetColumn(vis, 0);
            }
            else
            {
                Visualizers.Item2 = vis;
                Grid.SetColumn(vis, 2);
            }
            _ = VisualizerGrid.Children.Add(vis);
            UpdatePreviews();
        }


        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (PaletteEditorList.Count < MAX_RAMPS && RampCounter < MAX_RAMPS)
            {
                AddNewPaletteEditor();
                UpdatePreviews();
                SetDataSaved(false);
            }
            else
            {
                _ = MessageBox.Show(Properties.Resources.MainWindow_Warning_Cannot_Add_Ramp, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void AddNewPaletteEditor(SaveData.SavePalette? paletteData = null)
        {

            RowDefinition r = new()
            {
                Height = new(1, GridUnitType.Auto)
            };
            EntryGrid.RowDefinitions.Insert(EntryGrid.Children.Count - 1, r);
            Grid.SetRow(EntryGrid.Children[^1], EntryGrid.Children.Count);
            PaletteEditor p = new();

            if (paletteData != null)
            {
                p.SetTitle(paletteData.Value.Name);
                if (paletteData.Value.Options.ContainsKey(OPTION_TYPE_PALETTE.SAT_CURVE_MODE))
                {
                    p.SatCurveMode = (PaletteEditor.SaturationCurveMode)paletteData.Value.Options.GetValueOrDefault(OPTION_TYPE_PALETTE.SAT_CURVE_MODE);
                }

                if (paletteData.Value.Options.ContainsKey(OPTION_TYPE_PALETTE.IS_MINIMIZED))
                {
                    p.IsMinimized = paletteData.Value.Options.GetValueOrDefault(OPTION_TYPE_PALETTE.IS_MINIMIZED) == 0;
                }
                p.ColorCountSlider.Value = paletteData.Value.ColorCount;
                p.BaseHueSlider.Value = paletteData.Value.BaseHue;
                p.BaseSaturationSlider.Value = paletteData.Value.BaseSaturation;
                p.HueShiftSlider.Value = paletteData.Value.HueShift;
                p.HueShiftExponentSlider.Value = paletteData.Value.HueShiftExponent;
                p.SaturationShiftSlider.Value = paletteData.Value.SaturationShift;
                p.SaturationShiftExponentSlider.Value = paletteData.Value.SaturationShiftExponent;
                p.ValueRangeSlider.LowerValue = paletteData.Value.ValueMin;
                p.ValueRangeSlider.HigherValue = paletteData.Value.ValueMax;

                for (int i = 0; i < paletteData.Value.HSVShifts.Count; i++)
                {
                    SaveHSVShift shift = paletteData.Value.HSVShifts[i];
                    p.PaletteColorList[i].SetShift(shift.HueShift, shift.SaturationShift, shift.ValueShift);
                }
            }

            Grid.SetRow(p, EntryGrid.Children.Count - 1);
            Grid.SetColumn(p, 0);
            EntryGrid.Children.Insert(EntryGrid.Children.Count - 1, p);
            PaletteEditorList.Add(p);
            SetPaletteEditorEvents(p);

            if (paletteData == null)
            {
                p.SetTitle(string.Format("{0} {1}", Properties.Resources.Editor_Ramp_Prefix, (++RampCounter).ToString()));
            }

        }


        private void PaletteEditor_ColorsUpdated(object? sender, EventArgs e)
        {
            UpdatePreviews();
            SetDataSaved(false);
        }

        private void PaletteEditor_ColorHoverIn(object? sender, EventArgs e)
        {
            if (sender != null && sender is PaletteColor paletteColor)
            {
                SetHighlightBorders(paletteColor, true);
            }
        }

        private void PaletteColor_ColorHoverOut(object? sender, EventArgs e)
        {
            if (sender != null && sender is PaletteColor paletteColor)
            {
                SetHighlightBorders(paletteColor, false);
            }
        }

        private void SetHighlightBorders(PaletteColor source, bool highlight, bool? goUp = null)
        {
            foreach (ColorLink link in ColorLinkList)
            {
                if (link.Source.Color == source && (!goUp.HasValue || !goUp.GetValueOrDefault()))
                {
                    link.Target.Color.SetHighlightBorder(highlight);
                    link.Source.Color.SetHighlightBorder(highlight);
                    SetHighlightBorders(link.Target.Color, highlight, false);
                }
                if (link.Target.Color == source && (!goUp.HasValue || goUp.GetValueOrDefault()))
                {
                    link.Target.Color.SetHighlightBorder(highlight);
                    link.Source.Color.SetHighlightBorder(highlight);
                    SetHighlightBorders(link.Source.Color, highlight, true);
                }
            }
        }

        private void PaletteEditor_ControllerColorChanged(object? sender, EventArgs e)
        {
            if (sender is PaletteEditor sourceEditor)
            {
                foreach (ColorLink link in ColorLinkList)
                {
                    if (link.Source.Editor == sourceEditor)
                    {
                        HSVColor hsvColor = link.Source.Color.HSVColor;
                        link.Target.Editor.UpdateDependentColor(link.Target.Color, hsvColor);
                    }
                }
            }
        }

        private void PaletteEditor_LinkDeleted(object? sender, EventArgs e)
        {
            if (e is PaletteColor.LinkDeletedEventArgs ldEventArgs)
            {
                foreach (ColorLink link in ColorLinkList)
                {
                    if (link.Target.Color == ldEventArgs.ColorLinkPartner.Color &&
                        link.Target.Editor == ldEventArgs.ColorLinkPartner.Editor)
                    {
                        _ = ColorLinkList.Remove(link);
                        //update affected editors
                        link.Target.Editor.UpdateLinks(ColorLinkList);
                        link.Source.Editor.UpdateLinks(ColorLinkList);
                        SetDataSaved(false);
                        break;
                    }
                }
            }
        }

        private void PaletteEditor_LinkCreated(object? sender, EventArgs e)
        {
            if (e is PaletteColor.LinkCreatedEventArgs lcEventArgs)
            {
                ColorLinkList.Add(lcEventArgs.ColorLink);
                lcEventArgs.ColorLink.Source.Editor.UpdateLinks(ColorLinkList);
                lcEventArgs.ColorLink.Target.Editor.UpdateLinks(ColorLinkList);
                SetDataSaved(false);
            }
        }

        private void PaletteEditor_CloseButtonPressed(object? sender, EventArgs e)
        {
            //remove affected links
            _ = ColorLinkList.RemoveAll(p => (p.Target.Editor == sender || p.Source.Editor == sender));
            foreach (PaletteEditor editor in PaletteEditorList)
            {
                editor.UpdateLinks(ColorLinkList);
            }

            //find index of current element
            int? index = null;
            for (int i = 0; i < EntryGrid.Children.Count - 1; i++)
            {
                if (sender == EntryGrid.Children[i])
                {
                    index = i;
                    break;
                }
            }

            //remove element and shift remaining ones
            if (index.HasValue && EntryGrid.Children[index.Value] is PaletteEditor pEditor)
            {
                _ = PaletteEditorList.Remove(pEditor);
                EntryGrid.Children.Remove(pEditor);
                EntryGrid.RowDefinitions.RemoveAt(index.Value);
                for (int i = index.Value; i < EntryGrid.Children.Count; i++)
                {
                    Grid.SetRow(EntryGrid.Children[i], i);
                }
            }
            UpdatePreviews();
            SetDataSaved(false);
        }

        private void UpdatePreviews()
        {
            if (!PreviewUpdateTimer.IsEnabled)
            {
                PreviewUpdateTimer.Start();
            }
        }

        private void PreviewUpdateTimer_Tick(object? sender, EventArgs e)
        {
            PreviewUpdateTimer.Stop();
            Visualizers.Item1.Update(PaletteEditorList, ColorLinkList);
            Visualizers.Item2.Update(PaletteEditorList, ColorLinkList);
        }

        private void SaveButton_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SavingTriggered(true);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SavingTriggered(false);
        }

        private void SavingTriggered(bool dialogMandatory)
        {
            if (PaletteEditorList.Count == 0)
            {
                _ = MessageBox.Show(Properties.Resources.MainWindow_Msg_Cannot_Save_Empty_List, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SaveFileName == null || dialogMandatory)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = KPAL_FILE_FILTER
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    SetSaveFileName(saveFileDialog.FileName);
                }
                else
                {
                    return;
                }
            }

            SaveData.SaveConversionData scData = new(PaletteEditorList, ColorLinkList, CreateSaveOptionList());
            SaveData sData = new(scData);
            if (SaveFileName != null)
            {
                if (!sData.SaveToFile(SaveFileName))
                {
                    _ = MessageBox.Show(Properties.Resources.MainWindow_Error_Saving_Failed, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    SetDataSaved(true);
                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            bool doLoad = true;
            if (!IsDataSaved)
            {
                MessageBoxResult mResult = MessageBox.Show(Properties.Resources.MainWindow_Msg_Discard_Message, Properties.Resources.MainWindow_Warning_General_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                doLoad = (mResult == MessageBoxResult.OK);
            }

            if (doLoad)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = KPAL_FILE_FILTER
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    Cursor oldCursor = Mouse.OverrideCursor;
                    Mouse.OverrideCursor = Cursors.Wait;
                    SaveData sData = new(openFileDialog.FileName, out bool success);
                    if (!success)
                    {
                        _ = MessageBox.Show(Properties.Resources.MainWindow_Error_Loading_Failed, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        InitFromSaveData(sData);
                        SetSaveFileName(openFileDialog.FileName);
                        SetDataSaved(true);
                    }
                    Mouse.OverrideCursor = oldCursor;
                }
            }
        }

        private void SetDataSaved(bool isDataSaved)
        {
            if (isDataSaved != IsDataSaved)
            {
                IsDataSaved = isDataSaved;
                SetSaveFileName(SaveFileName);
            }
        }

        private void SetSaveFileName(string? saveFileName)
        {
            SaveFileName = saveFileName;
            string titleString = KPAL_TITLE;
            if (saveFileName != null)
            {
                titleString += " - " + Path.GetFileName(saveFileName);
                if (!IsDataSaved)
                {
                    titleString += "*";
                }
            }
            Title = titleString;
        }

        private void InitFromSaveData(SaveData sData)
        {
            ClearEntries();

            if (sData.Options.ContainsKey(OPTION_TYPE_GENERAL.LEFT_VISUALIZER))
            {
                SetNewVisualizer(true, Visualizers.Item1, (Visualizer.VisualizerType)sData.Options.GetValueOrDefault(OPTION_TYPE_GENERAL.LEFT_VISUALIZER));
            }
            if (sData.Options.ContainsKey(OPTION_TYPE_GENERAL.RIGHT_VISUALIZER))
            {
                SetNewVisualizer(false, Visualizers.Item2, (Visualizer.VisualizerType)sData.Options.GetValueOrDefault(OPTION_TYPE_GENERAL.RIGHT_VISUALIZER));
            }
            if (sData.Options.ContainsKey(OPTION_TYPE_GENERAL.RAMP_COUNTER))
            {
                RampCounter = sData.Options.GetValueOrDefault(OPTION_TYPE_GENERAL.RAMP_COUNTER);
            }



            for (int i = 0; i < sData.Palettes.Count; i++)
            {
                AddNewPaletteEditor(sData.Palettes[i]);
            }

            for (int i = 0; i < sData.ColorLinks.Count; i++)
            {
                PaletteEditor sourceEditor = PaletteEditorList[sData.ColorLinks[i].SourcePaletteIndex];
                PaletteColor sourceColor = sourceEditor.PaletteColorList[sData.ColorLinks[i].SourceColorIndex];
                PaletteEditor targetEditor = PaletteEditorList[sData.ColorLinks[i].TargetPaletteIndex];
                PaletteColor targetColor = targetEditor.PaletteColorList[sData.ColorLinks[i].TargetColorIndex];
                ColorLink cLink = new(sourceEditor, sourceColor, targetEditor, targetColor, true);
                PaletteColor.LinkCreatedEventArgs args = new(cLink);
                targetEditor.PaletteColor_LinkCreated(null, args);
            }

            UpdatePreviews();
        }

        private void SetPaletteEditorEvents(PaletteEditor p)
        {
            p.CloseButtonPressed += PaletteEditor_CloseButtonPressed;
            p.ControllerColorChanged += PaletteEditor_ControllerColorChanged;
            p.LinkCreated += PaletteEditor_LinkCreated;
            p.LinkDeleted += PaletteEditor_LinkDeleted;
            p.ColorHoverIn += PaletteEditor_ColorHoverIn;
            p.ColorHoverOut += PaletteColor_ColorHoverOut;
            p.ColorsUpdated += PaletteEditor_ColorsUpdated;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            bool doNew = true;
            if (!IsDataSaved)
            {
                MessageBoxResult mResult = MessageBox.Show(Properties.Resources.MainWindow_Msg_Discard_Message, Properties.Resources.MainWindow_Warning_General_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                doNew = (mResult == MessageBoxResult.OK);
            }
            if (doNew)
            {
                InitNew();
            }
        }

        private void ClearEntries()
        {
            while (EntryGrid.Children.Count > 1)
            {
                EntryGrid.Children.RemoveAt(0);
            }

            while (EntryGrid.RowDefinitions.Count > 1)
            {
                EntryGrid.RowDefinitions.RemoveAt(0);
            }

            PaletteEditorList.Clear();
            ColorLinkList.Clear();
        }

        private static string CreateExportFilter(List<FileExporter.ExportFilter> filterList)
        {
            StringBuilder filterBuilder = new();
            for (int i = 0; i < filterList.Count; i++)
            {
                if (i != 0)
                {
                    _ = filterBuilder.Append('|');
                }
                _ = filterBuilder.Append(filterList[i].Name);
                _ = filterBuilder.Append("|*.");
                _ = filterBuilder.Append(filterList[i].Extension);
            }
            return filterBuilder.ToString();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (PaletteEditorList.Count == 0)
            {
                _ = MessageBox.Show(Properties.Resources.MainWendow_Msg_Cannot_Export_Empty_List, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                Filter = EXPORT_FILTER
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                EXPORT_FILTER_LIST[saveFileDialog.FilterIndex - 1].Efunction(saveFileDialog.FileName, new SaveData.SaveConversionData(PaletteEditorList, ColorLinkList, CreateSaveOptionList()), out bool success);
                if (!success)
                {
                    _ = MessageBox.Show(Properties.Resources.MainWindow_Error_Exporting_Failed, Properties.Resources.MainWindow_Error_General_Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Dictionary<OPTION_TYPE_GENERAL, byte> CreateSaveOptionList()
        {
            Dictionary<OPTION_TYPE_GENERAL, byte> options = new()
            {
                { OPTION_TYPE_GENERAL.LEFT_VISUALIZER, Convert.ToByte(Visualizers.Item1.Type) },
                { OPTION_TYPE_GENERAL.RIGHT_VISUALIZER, Convert.ToByte(Visualizers.Item2.Type) },
                { OPTION_TYPE_GENERAL.RAMP_COUNTER, RampCounter }
            };
            return options;
        }
    }
}
