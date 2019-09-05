﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Mapperdom.Helpers;
using Mapperdom.Views;
using Mapperdom.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Mapperdom.Services;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;

namespace Mapperdom.ViewModels
{
    public class MainViewModel : Observable
    {

        public bool CanLoad
        {
            get
            {
                return SaveService.CanLoad("LastSave");
            }
        }


        private MapperGame _referencedGame;
        public MapperGame ReferencedGame
        {
            get
            {
                return _referencedGame;
            }
            set
            {
                Set(ref _referencedGame, value);
                OnPropertyChanged("IsActiveGame");
                SetMapEntries();
                SetNationEntries();
            }
        }



        private bool _nationIsTalking;
        public bool NationIsTalking
        {
            get
            {
                return _nationIsTalking;
            }
            set
            {
                Set(ref _nationIsTalking, value);

                if(value == false)
                {
                    TalkingNation = null;
                }
            }
        }

        private ObservableCollection<Nation> _nations;
        public ObservableCollection<Nation> Nations
        {
            get
            {
                return _nations;
            }
            set
            {
                Set(ref _nations, value);
            }
        }

        public Nation TalkingNation
        {
            get
            {
                if (ReferencedGame != null)
                    return ReferencedGame.TalkingNation;
                else
                    return null;
            }
            set
            {
                if(ReferencedGame != null)
                {
                    Set(ref ReferencedGame.TalkingNation, value);
                    SourceImage = ReferencedGame.GetCurrentMap();

                }
            }
        }

        public bool IsActiveGame
        {
            get
            {
                return _referencedGame != null;
            }
        }

        public bool SelectedNationIsAtWar
        {
            get
            {
                if (SelectedDisplayEntry == null) return false;
                return SelectedDisplayEntry.Nation.WarSide.HasValue;
            }
        }

        public bool NavyForcesEnabled
        {
            get
            {
                if (SelectedDisplayEntry == null) return false;

                return SelectedDisplayEntry.Nation.plan.navalActivity;
            }
            set
            {
                Set(ref SelectedDisplayEntry.Nation.plan.navalActivity, value);
            }
        }

        public int ForceStrength
        {
            get
            {
                if (SelectedDisplayEntry == null) return 0;
                return SelectedDisplayEntry.Nation.plan.range;
            }
            set
            {
                if(SelectedDisplayEntry != null)
                Set(ref SelectedDisplayEntry.Nation.plan.range, (ushort)value);
            }
        }


        private WriteableBitmap _sourceImage;
        public WriteableBitmap SourceImage
        {
            get
            {
                return _sourceImage;
            }
            set
            {
                Set(ref _sourceImage, value);
                SetMapEntries();
                SetNationEntries();
            }
        }

        private MapDisplayEntry _selectedDisplayEntry;

        public MapDisplayEntry SelectedDisplayEntry
        {
            get
            {
                return _selectedDisplayEntry;
            }
            set
            {
                Set(ref _selectedDisplayEntry, value);
                OnPropertyChanged("SelectedNationIsAtWar");
                OnPropertyChanged("NavyForcesEnabled");
                OnPropertyChanged("ForceStrength");
            }
        }

        private ObservableCollection<MapDisplayEntry> _mapEntries;
        public ObservableCollection<MapDisplayEntry> MapEntries
        {
            get
            {
                return _mapEntries;
            }
            set
            {
                Set(ref _mapEntries, value);
            }
        }

        private ICommand _executePlanCommand;
        public ICommand ExecutePlanCommand
        {
            get
            {
                if (_executePlanCommand == null)
                    _executePlanCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Advance();
                        SourceImage = ReferencedGame.GetCurrentMap();
                        ReferencedGame.Backup();
                    });

                return _executePlanCommand;
            }
        }



        private ICommand _saveProjectCommand;
        public ICommand SaveProjectCommand
        {
            get
            {
                if (_saveProjectCommand == null)
                    _saveProjectCommand = new RelayCommand(() =>
                    {
                        SaveService.SaveAsync(ReferencedGame, "LastSave");
                    });

                return _saveProjectCommand;
            }
        }

        private ICommand _loadProjectCommand;
        public ICommand LoadProjectCommand
        {
            get
            {
                if (_loadProjectCommand == null)
                    _loadProjectCommand = new RelayCommand(async () =>
                    {
                        ReferencedGame = await SaveService.LoadAsync("LastSave");
                        if(ReferencedGame != null)
                            SourceImage = ReferencedGame.GetCurrentMap();
                        else
                        {
                            MessageDialog errorDialog = new MessageDialog("There was an error loading your most recent project", "Error");
                            await errorDialog.ShowAsync();
                        }
                    });

                return _loadProjectCommand;
            }
        }

        private ICommand _newGameCommand;
        public ICommand NewGameCommand
        {
            get
            {
                if (_newGameCommand == null)
                    _newGameCommand = new RelayCommand(async () =>
                    {
                        FileOpenPicker openPicker = new FileOpenPicker();
                        openPicker.ViewMode = PickerViewMode.Thumbnail;
                        openPicker.FileTypeFilter.Add(".png");

                        StorageFile f = await openPicker.PickSingleFileAsync();

                        //Start new game if selected
                        if (f != null)
                        {
                            ImageProperties p = await f.Properties.GetImagePropertiesAsync();
                            WriteableBitmap bmp = new WriteableBitmap((int)p.Width, (int)p.Height);

                            bmp.SetSource((await f.OpenReadAsync()).AsStream().AsRandomAccessStream());

                            try
                            {
                                ReferencedGame = new MapperGame(bmp);
                            }
                            catch (Exception e)
                            {
                                MessageDialog errorDialog = new MessageDialog(e.Message, "Error");
                                await errorDialog.ShowAsync();

                                return;
                            }
                            SourceImage = ReferencedGame.GetCurrentMap();
                            ReferencedGame.Backup();
                        }
                    });

                return _newGameCommand;
            }
        }



        private ICommand _saveImageCommand;
        public ICommand SaveImageCommand
        {
            get
            {
                if (_saveImageCommand == null)
                    _saveImageCommand = new RelayCommand(async () =>
                    {
                        FileSavePicker fileSavePicker = new FileSavePicker
                        {
                            SuggestedStartLocation = PickerLocationId.PicturesLibrary
                        };
                        fileSavePicker.FileTypeChoices.Add("PNG File", new List<string>() { ".png" });
                        fileSavePicker.SuggestedFileName = "image";

                        StorageFile outputFile = await fileSavePicker.PickSaveFileAsync();

                        if (outputFile == null)
                        {
                            // The user cancelled the picking operation
                            return;
                        }

                        using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            // Create an encoder with the desired format
                            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                            // Set the software bitmap
                            WriteableBitmap wb = ReferencedGame.GetCurrentMap();


                            Stream pixelStream = wb.PixelBuffer.AsStream();
                            byte[] pixels = new byte[pixelStream.Length];
                            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                            // Set additional encoding parameters, if needed
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)wb.PixelHeight, (uint)wb.PixelHeight, 96.0, 96.0, pixels);

                            try
                            {
                                await encoder.FlushAsync();
                            }
                            catch (Exception err)
                            {
                                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                                switch (err.HResult)
                                {
                                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                                        // If the encoder does not support writing a thumbnail, then try again
                                        // but disable thumbnail generation.
                                        encoder.IsThumbnailGenerated = false;
                                        break;
                                    default:
                                        throw;
                                }
                            }
                        }
                    });

                return _saveImageCommand;
            }
        }

        private ICommand _undoCommand;
        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                    _undoCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Undo();
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _undoCommand;
            }
        }
        private ICommand _redoCommand;
        public ICommand RedoCommand
        {
            get
            {
                if (_redoCommand == null)
                    _redoCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Redo();
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _redoCommand;
            }
        }

        private ICommand _annexOccupationCommand;
        public ICommand AnnexOccupationCommand
        {
            get
            {
                if (_annexOccupationCommand == null)
                    _annexOccupationCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        Nation n = SelectedDisplayEntry.Nation;
                        ReferencedGame.AnnexTerritory(ReferencedGame.Nations.Single(pair => pair.Value == n).Key);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _annexOccupationCommand;
            }
        }


        private ICommand _generateNextFrame;

        public ICommand GenerateNextFrame
        {
            get
            {
                if (_generateNextFrame == null)
                    _generateNextFrame = new RelayCommand(async () =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Advance();
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _generateNextFrame;
            }
        }

        private ICommand _declareWarCommand;
        public ICommand DeclareWarCommand
        {
            get
            {
                if (_declareWarCommand == null)
                    _declareWarCommand = new RelayCommand(async () =>
                    {
                        ObservableCollection<Nation> options = new ObservableCollection<Nation>();
                        foreach (Nation n in ReferencedGame.Nations.Values.ToList())
                            options.Add(n);


                        if (SelectedNationIsAtWar)
                        {
                            foreach (Nation n in ReferencedGame.Nations.Values.Where(nat => nat.WarSide != null))
                                options.Remove(n);
                            options.Remove(SelectedDisplayEntry.Nation);
                        }

                        options.Remove(SelectedDisplayEntry.Nation);

                        PickNationDialog d1 = new PickNationDialog(this, options, SelectedDisplayEntry.Nation);
                        if ((await d1.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                        WarSide n1Side = d1.ViewModel.Nation1.WarSide.HasValue ? ReferencedGame.Sides[d1.ViewModel.Nation1.WarSide.Value] : null;
                        WarSide n2Side = d1.ViewModel.Nation2.WarSide.HasValue ? ReferencedGame.Sides[d1.ViewModel.Nation2.WarSide.Value] : null;


                        if (n1Side == null)
                        {
                            ObservableCollection<WarSide> sideOptions = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            if (n1Side != null) sideOptions.Remove(n1Side);

                            PickSideDialog d2 = new PickSideDialog(this, options.Count > 0, sideOptions, d1.ViewModel.Nation1);
                            if ((await d2.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            if(d2.ViewModel.IsNewWarSide)
                                n1Side = d2.ViewModel.NewWarSide;
                            else n1Side = d2.ViewModel.SelectedWarSide;
                        }

                        if (n2Side == null)
                        {
                            ObservableCollection<WarSide> sideOptions = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            if (n2Side != null) sideOptions.Remove(n2Side);

                            PickSideDialog d2 = new PickSideDialog(this, options.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values), d1.ViewModel.Nation2);
                            if ((await d2.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            if (d2.ViewModel.IsNewWarSide)
                                n2Side = d2.ViewModel.NewWarSide;
                            else n2Side = d2.ViewModel.SelectedWarSide;
                        }

                        ReferencedGame.Backup();
                        ReferencedGame.DeclareWar(ReferencedGame.Nations.FirstOrDefault(kvp => kvp.Value == d1.ViewModel.Nation1).Key, ReferencedGame.Nations.FirstOrDefault(kvp => kvp.Value == d1.ViewModel.Nation2).Key, n1Side, n2Side);

                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _declareWarCommand;
            }
        }



        private ICommand _startUprisingCommand;
        public ICommand StartUprisingCommand
        {
            get
            {
                if (_startUprisingCommand == null)
                    _startUprisingCommand = new RelayCommand(async () =>
                    {
                        if(SelectedNationIsAtWar)
                        {
                            ObservableCollection<WarSide> options = new ObservableCollection<WarSide>(ReferencedGame.Sides.Values);
                            options.Remove(ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value]);

                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, options, new Nation(SelectedDisplayEntry.Nation.Name + " Rebels", System.Drawing.Color.FromArgb(0x0000B33C)));
                            if ((await d1.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d1.ViewModel.SelectedNation, ReferencedGame.Sides[SelectedDisplayEntry.Nation.WarSide.Value], d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide);
                        }
                        else
                        {
                            PickSideDialog d1 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values), SelectedDisplayEntry.Nation);
                            if ((await d1.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            PickSideDialog d2 = new PickSideDialog(this, ReferencedGame.Sides.Count > 0, new ObservableCollection<WarSide>(ReferencedGame.Sides.Values.ToList()), new Nation(SelectedDisplayEntry.Nation.Name + " Rebels", System.Drawing.Color.FromArgb(0x0000B33C)));
                            if ((await d2.ShowAsync()) != Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
                                return;

                            ReferencedGame.Backup();
                            ReferencedGame.StartUprising(ReferencedGame.Nations.FirstOrDefault(n => n.Value == SelectedDisplayEntry.Nation).Key, d2.ViewModel.SelectedNation, d1.ViewModel.IsNewWarSide ? d1.ViewModel.NewWarSide : d1.ViewModel.SelectedWarSide, d2.ViewModel.IsNewWarSide ? d2.ViewModel.NewWarSide : d2.ViewModel.SelectedWarSide);

                        }

                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _startUprisingCommand;
            }
        }

        private ICommand _surrenderCommand;
        public ICommand SurrenderCommand
        {
            get
            {
                if (_surrenderCommand == null)
                    _surrenderCommand = new RelayCommand(() =>
                    {
                        ReferencedGame.Backup();
                        ReferencedGame.Surrender(ReferencedGame.Nations.Where(pair => pair.Value == SelectedDisplayEntry.Nation).Single().Key);
                        SourceImage = ReferencedGame.GetCurrentMap();
                    });

                return _surrenderCommand;
            }
        }

        private ICommand _attackNWCommand;
        public ICommand AttackNWCommand
        {
            get
            {
                if (_attackNWCommand == null)
                    _attackNWCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = -1;
                        SelectedDisplayEntry.Nation.plan.yFocus = -1;
                    });

                return _attackNWCommand;
            }
        }

        private ICommand _attackNCommand;
        public ICommand AttackNCommand
        {
            get
            {
                if (_attackNCommand == null)
                    _attackNCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 0;
                        SelectedDisplayEntry.Nation.plan.yFocus = -2;
                    });

                return _attackNCommand;
            }
        }

        private ICommand _attackNECommand;
        public ICommand AttackNECommand
        {
            get
            {
                if (_attackNECommand == null)
                    _attackNECommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 1;
                        SelectedDisplayEntry.Nation.plan.yFocus = -1;
                    });

                return _attackNECommand;
            }
        }


        private ICommand _attackWCommand;
        public ICommand AttackWCommand
        {
            get
            {
                if (_attackWCommand == null)
                    _attackWCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = -2;
                        SelectedDisplayEntry.Nation.plan.yFocus = 0;
                    });

                return _attackWCommand;
            }
        }

        private ICommand _attackCCommand;
        public ICommand AttackCCommand
        {
            get
            {
                if (_attackCCommand == null)
                    _attackCCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 0;
                        SelectedDisplayEntry.Nation.plan.yFocus = 0;
                    });

                return _attackCCommand;
            }
        }

        private ICommand _attackECommand;
        public ICommand AttackECommand
        {
            get
            {
                if (_attackECommand == null)
                    _attackECommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 2;
                        SelectedDisplayEntry.Nation.plan.yFocus = 0;
                    });

                return _attackECommand;
            }
        }


        private ICommand _attackSWCommand;
        public ICommand AttackSWCommand
        {
            get
            {
                if (_attackSWCommand == null)
                    _attackSWCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = -1;
                        SelectedDisplayEntry.Nation.plan.yFocus = 1;
                    });

                return _attackSWCommand;
            }
        }

        private ICommand _attackSCommand;
        public ICommand AttackSCommand
        {
            get
            {
                if (_attackSCommand == null)
                    _attackSCommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 0;
                        SelectedDisplayEntry.Nation.plan.yFocus = 2;
                    });

                return _attackSCommand;
            }
        }

        private ICommand _attackSECommand;
        public ICommand AttackSECommand
        {
            get
            {
                if (_attackSECommand == null)
                    _attackSECommand = new RelayCommand(() =>
                    {
                        SelectedDisplayEntry.Nation.plan.xFocus = 1;
                        SelectedDisplayEntry.Nation.plan.yFocus = 1;
                    });

                return _attackSECommand;
            }
        }
        public MainViewModel()
        {

        }

        private void SetNationEntries()
        {
            Nation n = TalkingNation;
            ObservableCollection<Nation> entries = new ObservableCollection<Nation>();
            
            if(ReferencedGame != null)
            {
                if(Nations == null)
                {
                    foreach (Nation nat in ReferencedGame.Nations.Values.ToList())
                    {
                        entries.Add(nat);
                    }

                    Nations = entries;
                }
                else
                {
                    List<Nation> currentNationList = ReferencedGame.Nations.Values.ToList();
                    ObservableCollection<Nation> currentEntries = new ObservableCollection<Nation>(Nations);

                    foreach (Nation entry in currentEntries)
                    {
                        if (currentNationList.FirstOrDefault(nat => nat == entry) == null)
                        {
                            Nations.Remove(entry);
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        if (Nations.FirstOrDefault(e => e == nat) == null)
                        {
                            Nations.Add(nat);
                        }
                    }
                }
            }
        }

        private void SetMapEntries()
        {
            Nation n = SelectedDisplayEntry != null ? SelectedDisplayEntry.Nation : null;
            ObservableCollection<MapDisplayEntry> entries = new ObservableCollection<MapDisplayEntry>();


            if (ReferencedGame != null)
            {
                if(MapEntries == null)
                {
                    foreach (Nation nat in ReferencedGame.Nations.Values.ToList())
                    {
                        entries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null));
                    }

                    MapEntries = entries;

                    if (n != null)
                        SelectedDisplayEntry = entries.FirstOrDefault(e => e.Nation == n);
                    else SelectedDisplayEntry = entries.FirstOrDefault();
                }
                else
                {
                    List<Nation> currentNationList = ReferencedGame.Nations.Values.ToList();
                    ObservableCollection<MapDisplayEntry> currentEntries = new ObservableCollection<MapDisplayEntry>(MapEntries);

                    foreach (MapDisplayEntry entry in currentEntries)
                    {
                        if(currentNationList.FirstOrDefault(nat => nat == entry.Nation) == null)
                        {
                            MapEntries.Remove(entry);
                        }
                    }

                    foreach(Nation nat in currentNationList)
                    {
                        if(MapEntries.FirstOrDefault(e => e.Nation == nat) == null)
                        {
                            MapEntries.Add(new MapDisplayEntry(nat, nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null));
                        }
                    }

                    foreach (Nation nat in currentNationList)
                    {
                        MapDisplayEntry entry = MapEntries.First(e => e.Nation == nat);
                        entry.Update(nat.WarSide.HasValue ? ReferencedGame.Sides[nat.WarSide.Value] : null);
                    }
                }
            }
            OnPropertyChanged("SelectedNationIsAtWar");
        }
    }
}
