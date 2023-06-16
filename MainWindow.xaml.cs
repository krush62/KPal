using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static KPal.SaveData;

//TODO GENERAL
//display color distance for each color in palette (starting from center left and right)?
//Perform a check if all necessary files (colors.csv etc.) exist
//Max ramps allowed?
//review every access modifier (public -> internal, etc.)


namespace KPal
{
    public partial class MainWindow : Window
    {
        private uint rampCounter;
        private readonly List<PaletteEditor> PaletteEditorList;
        private readonly List<ColorLink> ColorLinkList;
        private readonly DispatcherTimer PreviewUpdateTimer;
        private string? SaveFileName;
        private bool IsDataSaved;
        private (Visualizer, Visualizer) Visualizers;

        private const string KPAL_FILE_FILTER = "kPal file (*.kpal)|*.kpal";
        private const string KPAL_TITLE = "kPal";

        public MainWindow()
        {
            InitializeComponent();

            if (!File.Exists(ColorNames.COLOR_FILE_NAME))
            {
                _ = MessageBox.Show("Color name list file (" + ColorNames.COLOR_FILE_NAME + ") was not found.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            SetSaveFileName(null);
            SetDataSaved(true);
            PaletteEditorList = new List<PaletteEditor>();
            ColorLinkList = new List<ColorLink>();
            rampCounter = 0;
            PreviewUpdateTimer = new DispatcherTimer();
            PreviewUpdateTimer.Tick += PreviewUpdateTimer_Tick;
            PreviewUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            SetNewVisualizer(true, Visualizers.Item1, Visualizer.VisualizerType.ShadingCube);
            SetNewVisualizer(false, Visualizers.Item2, Visualizer.VisualizerType.PaletteMerge);
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
            RowDefinition r = new()
            {
                Height = new(1, GridUnitType.Auto)
            };
            EntryGrid.RowDefinitions.Insert(EntryGrid.Children.Count - 1, r);
            Grid.SetRow(EntryGrid.Children[^1], EntryGrid.Children.Count);
            PaletteEditor p = new();
            p.SetTitle("Ramp " + (++rampCounter).ToString());

            SetPaletteEditorEvents(p);
            Grid.SetRow(p, EntryGrid.Children.Count - 1);
            Grid.SetColumn(p, 0);
            EntryGrid.Children.Insert(EntryGrid.Children.Count - 1, p);
            PaletteEditorList.Add(p);
            UpdatePreviews();
            SetDataSaved(false);
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
                        HSVColor hsvColor = link.Source.Color.ColorHSV;
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
                    _ = MessageBox.Show("Saving file failed", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBoxResult mResult = MessageBox.Show("Discard unsaved changes?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
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
                    SaveData sData = new(openFileDialog.FileName, out bool success);
                    if (!success)
                    {
                        _ = MessageBox.Show("Loading file failed", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        InitFromSaveData(sData);
                        SetSaveFileName(openFileDialog.FileName);
                        SetDataSaved(true);
                    }
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



            for (int i = 0; i < sData.Palettes.Count; i++)
            {
                SavePalette pal = sData.Palettes[i];
                RowDefinition r = new()
                {
                    Height = new(1, GridUnitType.Auto)
                };
                EntryGrid.RowDefinitions.Insert(EntryGrid.Children.Count - 1, r);
                Grid.SetRow(EntryGrid.Children[^1], EntryGrid.Children.Count);
                PaletteEditor p = new();
                p.SetTitle(pal.Name);
                if (pal.Options.ContainsKey(OPTION_TYPE_PALETTE.SAT_CURVE_MODE))
                {
                    p.SatCurveMode = (PaletteEditor.SaturationCurveMode)pal.Options.GetValueOrDefault(OPTION_TYPE_PALETTE.SAT_CURVE_MODE);
                }

                if (pal.Options.ContainsKey(OPTION_TYPE_PALETTE.IS_MINIMIZED))
                {
                    p.IsMinimized = pal.Options.GetValueOrDefault(OPTION_TYPE_PALETTE.IS_MINIMIZED) == 0;
                }

                Grid.SetRow(p, EntryGrid.Children.Count - 1);
                Grid.SetColumn(p, 0);
                EntryGrid.Children.Insert(EntryGrid.Children.Count - 1, p);
                PaletteEditorList.Add(p);
                p.ColorCountSlider.Value = pal.ColorCount;
                p.BaseHueSlider.Value = pal.BaseHue;
                p.BaseSaturationSlider.Value = pal.BaseSaturation;
                p.HueShiftSlider.Value = pal.HueShift;
                p.HueShiftExponentSlider.Value = pal.HueShiftExponent;
                p.SaturationShiftSlider.Value = pal.SaturationShift;
                p.SaturationShiftExponentSlider.Value = pal.SaturationShiftExponent;
                p.ValueRangeSlider.LowerValue = pal.ValueMin;
                p.ValueRangeSlider.HigherValue = pal.ValueMax;
                SetPaletteEditorEvents(p);
            }


            for (int i = 0; i < sData.ColorLinks.Count; i++)
            {
                PaletteEditor sourceEditor = PaletteEditorList[sData.ColorLinks[i].SourcePaletteIndex];
                PaletteColor sourceColor = sourceEditor.GetColorForIndex(sData.ColorLinks[i].SourceColorIndex);
                PaletteEditor targetEditor = PaletteEditorList[sData.ColorLinks[i].TargetPaletteIndex];
                PaletteColor targetColor = targetEditor.GetColorForIndex(sData.ColorLinks[i].TargetColorIndex);
                ColorLink cLink = new(sourceEditor, sourceColor, targetEditor, targetColor);
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
                MessageBoxResult mResult = MessageBox.Show("Discard unsaved changes?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                doNew = (mResult == MessageBoxResult.OK);
            }
            if (doNew)
            {
                ClearEntries();
                SetSaveFileName(null);
                UpdatePreviews();
                SetDataSaved(true);
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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            List<FileExporter.ExportFilter> filterList = FileExporter.GetAvailableFormats();
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

            SaveFileDialog saveFileDialog = new()
            {
                Filter = filterBuilder.ToString()
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                filterList[saveFileDialog.FilterIndex - 1].Efunction(saveFileDialog.FileName, new SaveData.SaveConversionData(PaletteEditorList, ColorLinkList, CreateSaveOptionList()), out bool success);
                if (success)
                {
                    _ = MessageBox.Show("Exporting file failed", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Dictionary<OPTION_TYPE_GENERAL, byte> CreateSaveOptionList()
        {
            Dictionary<OPTION_TYPE_GENERAL, byte> options = new()
            {
                { OPTION_TYPE_GENERAL.LEFT_VISUALIZER, Convert.ToByte(Visualizers.Item1.Type) },
                { OPTION_TYPE_GENERAL.RIGHT_VISUALIZER, Convert.ToByte(Visualizers.Item2.Type) }
            };
            return options;
        }
    }
}
