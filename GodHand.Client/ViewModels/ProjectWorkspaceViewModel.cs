﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using GodHand.Shared.IO;
using GodHand.Shared.Models;
using MessageBox = System.Windows.MessageBox;
using Screen = Caliburn.Micro.Screen;
using TreeViewItem = GodHand.Shared.Models.TreeViewItem;

namespace GodHand.Client.ViewModels
{
    public class ProjectWorkspaceViewModel : Screen, ITabItem
    {
        protected ProjectWorkspaceViewModel(string header, bool isSelected, ProjectSettings settings)
        {
            Header = header;
            IsSelected = isSelected;
            _settings = settings;
        }

        public static ProjectWorkspaceViewModel Create(string header, bool isSelected, ProjectSettings settings) => new ProjectWorkspaceViewModel(header, isSelected, settings);

        #region Interface
        public string Header { get; set; }
        public bool IsSelected { get; set; }
        public Screen Content => this;
        #endregion

        #region Properties

        private ProjectSettings _settings;

        private int _controlWidth = 300;
        public int ControlWidth
        {
            get => _controlWidth;
            set
            {
                _controlWidth = value;
                NotifyOfPropertyChange(() => ControlWidth);
            }
        }

        private List<TreeViewItem> _treeViewItems;

        public List<TreeViewItem> TreeViewItems
        {
            get => _treeViewItems;
            set
            {
                _treeViewItems = value;
                NotifyOfPropertyChange(() => TreeViewItems);
            }
        }

        private List<TreeViewItem> _files;

        public List<TreeViewItem> Files
        {
            get => _files;
            set
            {
                _files = value;
                NotifyOfPropertyChange(() => Files);
            }
        }

        public List<string> CmbEncoderTable { get; } = new List<string>() { "Default" };
        public string SelectedCmbEncoderTable { get; set; } = "Default";

        private long _tbxStartOffset = -1;
        public long TbxStartOffset
        {
            get => _tbxStartOffset;
            set
            {
                _tbxStartOffset = value;
                NotifyOfPropertyChange(() => TbxStartOffset);
            }
        }

        private long _tbxOffsetLength = -1;
        public long TbxOffsetLength
        {
            get => _tbxOffsetLength;
            set
            {
                _tbxOffsetLength = value;
                NotifyOfPropertyChange(() => TbxOffsetLength);
            }
        }

        private ObservableCollection<ByteInformation> _collection = new ObservableCollection<ByteInformation>();

        public ObservableCollection<Shared.Models.ByteInformation> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
                NotifyOfPropertyChange(() => Collection);
            }
        }

        private ByteInformation _selectedCollection;
        public ByteInformation SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                _selectedCollection = value;
                if (value != null)
                {
                    if (value?.RomajiTranslation == null && Sources.Settings.EnableRomajiTranslation)
                        value.RomajiTranslation = Shared.IO.Convert.ToRomaji(value.CurrentValue, Sources.Settings);
                    if (value?.EnglishTranslation == null && Sources.Settings.EnableGoogleTranslation)
                        value.EnglishTranslation = Shared.IO.Convert.ToEnglish(value.CurrentValue);
                }
                NotifyOfPropertyChange(() => SelectedCollection);
            }
        }

        private bool _isOpening;
        private bool IsOpening
        {
            get => _isOpening;
            set
            {
                _isOpening = value;
                NotifyOfPropertyChange(() => IsOpening);
                NotifyOfPropertyChange(() => CanBtnSaveFile);
            }
        }

        public TreeViewItem SelectedListBoxItem { get; set; }

        #endregion

        #region Methods

        private async void OpenFile()
        {
            IsOpening = true;
            Collection.Clear();
            await Task.Factory.StartNew(() =>
            {
                Collection = Read.File(SelectedListBoxItem.Fullpath, TbxStartOffset, TbxOffsetLength, SelectedCmbEncoderTable);
            });
            IsOpening = false;
        }

        public void BtnSaveFile()
        {
            ObservableCollection<ByteInformation> collection = new ObservableCollection<ByteInformation>(
                Collection.Where(x => x.HasChange));

            if (!File.Exists(SelectedListBoxItem.Fullpath + ".bak"))
            {
                var dlgResult = System.Windows.Forms.MessageBox.Show("Do you want to make a backup file of the original file?",
                    "Generating Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dlgResult == DialogResult.Yes) File.Copy(SelectedListBoxItem.Fullpath, SelectedListBoxItem.Fullpath + ".bak");
            }

            Write.ValueToFile(SelectedListBoxItem.Fullpath, collection);
        }

        public bool CanBtnSaveFile => new ObservableCollection<ByteInformation>(
                                          Collection.Where(x => x.HasChange)).Any() && !_isOpening;

        public void BtnShowHide()
        {
            ControlWidth = (ControlWidth == 300) ? ControlWidth = 0 : ControlWidth = 300;
        }
        #endregion

        #region Events
        public void View_Loaded()
        {
            TreeViewItems = Shared.IO.Read.DirectoriesOfPath(_settings.RootPath);
        }

        public void Dg_CellEditEnding(DataGridCellEditEndingEventArgs e)
        {
            var element = e.EditingElement as System.Windows.Controls.TextBox;
            e.Cancel = CellValueChanged(e.Column, element?.Text);
        }

        private bool CellValueChanged(DataGridColumn column, string value)
        {
            bool isCellValueValid = true;

            if (column.Header.ToString() == "New Value")
            {
                isCellValueValid = value?.Length > SelectedCollection.ByteValueLength;
                if (!value.Equals(SelectedCollection.CurrentValue)) SelectedCollection.HasChange = true;
            }

            return isCellValueValid;
        }

        public void Cmb_DropDownOpened()
        {
            CmbEncoderTable.RemoveRange(1, CmbEncoderTable.Count - 1);
            var files = Directory.GetFiles(Environment.CurrentDirectory + @"\encoding\", "*.txt",
                SearchOption.TopDirectoryOnly);

            CmbEncoderTable.AddRange(files.Select(x => x.Split('\\')[x.Split('\\').Length - 1]));
        }

        public void TreeView_SelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as TreeViewItem;
            Files = Shared.IO.Read.FilesOfDirectory(item?.Fullpath);
        }

        public void ListBox_MouseDoubleClick()
        {            
            OpenFile();
        }
        #endregion
    }
}
