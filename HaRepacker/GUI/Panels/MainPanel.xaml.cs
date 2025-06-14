﻿using HaRepacker.GUI.Input;
using HaSharedLibrary.GUI;
using MapleLib.WzLib;
using MapleLib.WzLib.Spine;
using MapleLib.WzLib.WzProperties;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static MapleLib.Configuration.UserSettings;
using System.IO;
using HaRepacker.GUI.Panels.SubPanels;
using HaRepacker.GUI.Controls;
using MapleLib.WzLib.WzStructure.Data;
using System.ComponentModel.DataAnnotations;

namespace HaRepacker.GUI.Panels
{
    /// <summary>
    /// Interaction logic for MainPanelXAML.xaml
    /// </summary>
    public partial class MainPanel : UserControl
    {
        // Constants
        private const string FIELD_LIMIT_OBJ_NAME = "fieldLimit";
        private const string FIELD_TYPE_OBJ_NAME = "fieldType";
        private const string PORTAL_NAME_OBJ_NAME = "pn";

        private readonly MainForm _mainForm;
        public MainForm MainForm
        {
            get { return _mainForm; }
            private set { }
        }

        // Data binding
        private MainPanelPropertyItems _bindingPropertyItem = new MainPanelPropertyItems();
        //private MainPanelPropertyItemInterface _bindingPropertyItemReadOnly = new MainPanelPropertyItems_ReadOnly();


        // Etc
        private readonly static List<WzObject> clipboard = new List<WzObject>();
        private readonly UndoRedoManager undoRedoMan;

        private bool isSelectingWzMapFieldLimit = false;
        private bool isLoading = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPanel(MainForm mainForm)
        {
            InitializeComponent();

            isLoading = true;

            this._mainForm = mainForm;

            // Events
#if DEBUG
            toolStripStatusLabel_debugMode.Visibility = Visibility.Visible;
#else
            toolStripStatusLabel_debugMode.Visibility = Visibility.Collapsed;
#endif

            // undo redo
            undoRedoMan = new UndoRedoManager(this);

            // Set theme color
            if (Program.ConfigurationManager.UserSettings.ThemeColor == (int)UserSettingsThemeColor.Dark)
            {
                VisualStateManager.GoToState(this, "BlackTheme", false);
                DataTree.BackColor = System.Drawing.Color.Black;
                DataTree.ForeColor = System.Drawing.Color.White;
            }

            // data binding stuff
            propertyGrid.DataContext = _bindingPropertyItem;
            _bindingPropertyItem.PropertyChanged += propertyGrid_PropertyChanged_1;

            // Storyboard
            System.Windows.Media.Animation.Storyboard sbb = (System.Windows.Media.Animation.Storyboard)(this.FindResource("Storyboard_Find_FadeIn"));
            sbb.Completed += Storyboard_Find_FadeIn_Completed;


            // buttons
            menuItem_changeImage.Visibility = Visibility.Collapsed;
            menuItem_changeSound.Visibility = Visibility.Collapsed;
            menuItem_saveSound.Visibility = Visibility.Collapsed;
            menuItem_saveImage.Visibility = Visibility.Collapsed;

            textEditor.SaveButtonClicked += TextEditor_SaveButtonClicked;
            Loaded += MainPanelXAML_Loaded;


            isLoading = false;
        }

        private void MainPanelXAML_Loaded(object sender, RoutedEventArgs e)
        {
            this.fieldLimitPanel1.FieldLimitChanged += FieldLimitPanel1_FieldLimitChanged;
            //this.fieldTypePanel.SetTextboxOnFieldTypeChange(textPropBox);
        }

        #region Exported Fields
        public UndoRedoManager UndoRedoMan { get { return undoRedoMan; } }

        #endregion

        #region Data Tree
        private void DataTree_DoubleClick(object sender, EventArgs e)
        {
            if (DataTree.SelectedNode != null && DataTree.SelectedNode.Tag is WzImage && DataTree.SelectedNode.Nodes.Count == 0)
            {
                ParseOnDataTreeSelectedItem(((WzNode)DataTree.SelectedNode), true);
            }
        }

        private void DataTree_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            if (DataTree.SelectedNode == null)
            {
                return;
            }
            ShowObjectValue((WzObject)DataTree.SelectedNode.Tag);

            _bindingPropertyItem.WzFileType = ((WzNode)DataTree.SelectedNode).GetTypeName();
            //selectionLabel.Text = string.Format(Properties.Resources.SelectionType, ((WzNode)DataTree.SelectedNode).GetTypeName());
        }

        /// <summary>
        /// Parse the data tree selected item on double clicking, or copy pasting into it.
        /// </summary>
        /// <param name="selectedNode"></param>
        private static void ParseOnDataTreeSelectedItem(WzNode selectedNode, bool expandDataTree = true)
        {
            WzImage wzImage = (WzImage)selectedNode.Tag;

            if (!wzImage.Parsed)
                wzImage.ParseImage();
            selectedNode.Reparse();
            if (expandDataTree)
            {
                selectedNode.Expand();
            }
        }

        private void DataTree_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!DataTree.Focused) return;
            bool ctrl = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == System.Windows.Forms.Keys.Control;
            bool alt = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Alt) == System.Windows.Forms.Keys.Alt;
            bool shift = (System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Shift) == System.Windows.Forms.Keys.Shift;
            System.Windows.Forms.Keys filteredKeys = e.KeyData;
            if (ctrl) filteredKeys = filteredKeys ^ System.Windows.Forms.Keys.Control;
            if (alt) filteredKeys = filteredKeys ^ System.Windows.Forms.Keys.Alt;
            if (shift) filteredKeys = filteredKeys ^ System.Windows.Forms.Keys.Shift;

            switch (filteredKeys)
            {
                case System.Windows.Forms.Keys.F5:
                    StartAnimateSelectedCanvas();
                    break;
                case System.Windows.Forms.Keys.Escape:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case System.Windows.Forms.Keys.Delete:
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    PromptRemoveSelectedTreeNodes();
                    break;
            }
            if (ctrl)
            {
                switch (filteredKeys)
                {
                    case System.Windows.Forms.Keys.R: // Render map        
                        //HaRepackerMainPanel.

                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        break;
                    case System.Windows.Forms.Keys.C:
                        DoCopy();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        break;
                    case System.Windows.Forms.Keys.V:
                        DoPaste();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        break;
                    case System.Windows.Forms.Keys.F: // open search box
                        if (grid_FindPanel.Visibility == Visibility.Collapsed)
                        {
                            System.Windows.Media.Animation.Storyboard sbb = (System.Windows.Media.Animation.Storyboard)(this.FindResource("Storyboard_Find_FadeIn"));
                            sbb.Begin();

                            e.Handled = true;
                            e.SuppressKeyPress = true;
                        }
                        break;
                    case System.Windows.Forms.Keys.T:
                    case System.Windows.Forms.Keys.O:
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        break;
                }
            }
        }
        #endregion

        #region Wz Directory Context Menu
        /// <summary>
        /// WzDirectory
        /// </summary>
        /// <param name="target"></param>
        public void AddWzDirectoryToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            if (!(target.Tag is WzDirectory) && !(target.Tag is WzFile))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            string name;
            if (!NameInputBox.Show(Properties.Resources.MainAddDir, 0, out name))
                return;

            bool added = false;

            WzObject obj = (WzObject)target.Tag;
            while (obj is WzFile || ((obj = obj.Parent) is WzFile))
            {
                WzFile topMostWzFileParent = (WzFile)obj;

                ((WzNode)target).AddObject(new WzDirectory(name, topMostWzFileParent), UndoRedoMan);
                added = true;
                break;
            }
            if (!added)
            {
                MessageBox.Show(Properties.Resources.MainTreeAddDirError);
            }
        }

        /// <summary>
        /// WzDirectory
        /// </summary>
        /// <param name="target"></param>
        public void AddWzImageToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            if (!(target.Tag is WzDirectory) && !(target.Tag is WzFile))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameInputBox.Show(Properties.Resources.MainAddImg, 0, out name))
                return;
            ((WzNode)target).AddObject(new WzImage(name) { Changed = true }, UndoRedoMan);
        }

        /// <summary>
        /// WzByteProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzByteFloatToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            double? d;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!FloatingPointInputBox.Show(Properties.Resources.MainAddFloat, out name, out d))
                return;
            ((WzNode)target).AddObject(new WzFloatProperty(name, (float)d), UndoRedoMan);
        }

        /// <summary>
        /// WzCanvasProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzCanvasToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            List<System.Drawing.Bitmap> bitmaps = new List<Bitmap>();
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!BitmapInputBox.Show(Properties.Resources.MainAddCanvas, out name, out bitmaps))
                return;

            WzNode wzNode = ((WzNode)target);

            int i = 0;
            foreach (System.Drawing.Bitmap bmp in bitmaps)
            {
                string proposedName = bitmaps.Count == 1 ? name : (name + i);

                // Check if name already exists in parent
                if (WzNode.GetChildNode(wzNode, proposedName) != null)
                {
                    Warning.Error(Properties.Resources.MainNodeExists);
                    continue;
                }

                WzCanvasProperty canvas = new WzCanvasProperty(proposedName);
                WzPngProperty pngProperty = new WzPngProperty();
                pngProperty.PNG = bmp;
                canvas.PngProperty = pngProperty;


                WzNode newInsertedNode = wzNode.AddObject(canvas, UndoRedoMan);
                // Add an additional WzVectorProperty with X Y of 0,0
                newInsertedNode.AddObject(new WzVectorProperty(WzCanvasProperty.OriginPropertyName, new WzIntProperty("X", 0), new WzIntProperty("Y", 0)), UndoRedoMan);

                i++;
            }
        }

        /// <summary>
        /// WzCompressedInt
        /// </summary>
        /// <param name="target"></param>
        public void AddWzCompressedIntToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            int? value;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!IntInputBox.Show(
                Properties.Resources.MainAddInt,
                "", 0,
                out name, out value))
                return;
            ((WzNode)target).AddObject(new WzIntProperty(name, (int)value), UndoRedoMan);
        }

        /// <summary>
        /// WzLongProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzLongToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            long? value;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!LongInputBox.Show(Properties.Resources.MainAddInt, out name, out value))
                return;
            ((WzNode)target).AddObject(new WzLongProperty(name, (long)value), UndoRedoMan);
        }

        /// <summary>
        /// WzConvexProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzConvexPropertyToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameInputBox.Show(Properties.Resources.MainAddConvex, 0, out name))
                return;
            ((WzNode)target).AddObject(new WzConvexProperty(name), UndoRedoMan);
        }

        /// <summary>
        /// WzNullProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzDoublePropertyToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            double? d;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!FloatingPointInputBox.Show(Properties.Resources.MainAddDouble, out name, out d))
                return;
            ((WzNode)target).AddObject(new WzDoubleProperty(name, (double)d), UndoRedoMan);
        }

        /// <summary>
        /// WzNullProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzNullPropertyToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameInputBox.Show(Properties.Resources.MainAddNull, 0, out name))
                return;
            ((WzNode)target).AddObject(new WzNullProperty(name), UndoRedoMan);
        }

        /// <summary>
        /// WzSoundProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzSoundPropertyToSelectedNode(System.Windows.Forms.TreeNode target)
        {
            string name;
            string path;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!SoundInputBox.Show(Properties.Resources.MainAddSound, out name, out path))
                return;
            ((WzNode)target).AddObject(new WzBinaryProperty(name, path), UndoRedoMan);
        }

        /// <summary>
        /// WzStringProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzStringPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
            string name;
            string value;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameValueInputBox.Show(Properties.Resources.MainAddString, out name, out value))
                return;
            ((WzNode)target).AddObject(new WzStringProperty(name, value), UndoRedoMan);
        }

        /// <summary>
        /// WzSubProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzSubPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
            string name;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameInputBox.Show(Properties.Resources.MainAddSub, 0, out name))
                return;
            ((WzNode)target).AddObject(new WzSubProperty(name), UndoRedoMan);
        }

        /// <summary>
        /// WzUnsignedShortProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzUnsignedShortPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
            string name;
            int? value;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!IntInputBox.Show(Properties.Resources.MainAddShort,
                "", 0,
                out name, out value))
                return;
            ((WzNode)target).AddObject(new WzShortProperty(name, (short)value), UndoRedoMan);
        }

        /// <summary>
        /// WzUOLProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzUOLPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
            string name;
            string value;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameValueInputBox.Show(Properties.Resources.MainAddLink, out name, out value))
                return;
            ((WzNode)target).AddObject(new WzUOLProperty(name, value), UndoRedoMan);
        }

        /// <summary>
        /// WzVectorProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzVectorPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
            string name;
            System.Drawing.Point? pt;
            if (!(target.Tag is IPropertyContainer))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!VectorInputBox.Show(Properties.Resources.MainAddVec, out name, out pt))
                return;
            ((WzNode)target).AddObject(new WzVectorProperty(name, new WzIntProperty("X", ((System.Drawing.Point)pt).X), new WzIntProperty("Y", ((System.Drawing.Point)pt).Y)), UndoRedoMan);
        }

        /// <summary>
        /// WzLuaProperty
        /// </summary>
        /// <param name="target"></param>
        public void AddWzLuaPropertyToSelectedIndex(System.Windows.Forms.TreeNode target)
        {
 /*           string name;
            string value;
            if (!(target.Tag is WzDirectory) && !(target.Tag is WzFile))
            {
                Warning.Error(Properties.Resources.MainCannotInsertToNode);
                return;
            }
            else if (!NameValueInputBox.Show(Properties.Resources.MainAddString, out name, out value))
                return;

            string propertyName = name;
            if (!propertyName.EndsWith(".lua"))
            {
                propertyName += ".lua"; // it must end with .lua regardless
            }
            ((WzNode)target).AddObject(new WzImage(propertyName), UndoRedoMan);*/
        }

        /// <summary>
        /// Remove selected nodes
        /// </summary>
        public void PromptRemoveSelectedTreeNodes()
        {
            if (!Warning.Warn(Properties.Resources.MainConfirmRemove))
            {
                return;
            }

            List<UndoRedoAction> actions = new List<UndoRedoAction>();

            System.Windows.Forms.TreeNode[] nodeArr = new System.Windows.Forms.TreeNode[DataTree.SelectedNodes.Count];
            DataTree.SelectedNodes.CopyTo(nodeArr, 0);

            foreach (WzNode node in nodeArr)
                if (!(node.Tag is WzFile) && node.Parent != null)
                {
                    actions.Add(UndoRedoManager.ObjectRemoved((WzNode)node.Parent, node));
                    node.DeleteWzNode();
                }
            UndoRedoMan.AddUndoBatch(actions);
        }

        /// <summary>
        /// Rename an individual node
        /// </summary>
        public void PromptRenameWzTreeNode(WzNode node)
        {
            if (node == null)
                return;

            string newName = "";
            WzNode wzNode = node;
            if (RenameInputBox.Show(Properties.Resources.MainConfirmRename, wzNode.Text, out newName))
            {
                wzNode.ChangeName(newName);
            }
        }
        #endregion

        #region Panel Loading Events
        /// <summary>
        /// Set panel loading splash screen from MainForm.cs
        /// <paramref name="currentDispatcher"/>
        /// </summary>
        public void OnSetPanelLoading(Dispatcher currentDispatcher = null)
        {
            Action action = () =>
            {
                loadingPanel.OnStartAnimate();
                grid_LoadingPanel.Visibility = Visibility.Visible;
                treeView_WinFormsHost.Visibility = Visibility.Collapsed;
            };
            if (currentDispatcher != null)
                currentDispatcher.BeginInvoke(action);
            else
                grid_LoadingPanel.Dispatcher.BeginInvoke(action);
        }

        /// <summary>
        /// Remove panel loading splash screen from MainForm.cs
        /// <paramref name="currentDispatcher"/>
        /// </summary>
        public void OnSetPanelLoadingCompleted(Dispatcher currentDispatcher = null)
        {
            Action action = () =>
            {
                loadingPanel.OnPauseAnimate();
                grid_LoadingPanel.Visibility = Visibility.Collapsed;
                treeView_WinFormsHost.Visibility = Visibility.Visible;
            };
            if (currentDispatcher != null)
                currentDispatcher.BeginInvoke(action);
            else
                grid_LoadingPanel.Dispatcher.BeginInvoke(action);
        }

        /// <summary>
        /// Save the image animation into a JPG file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SaveImageAnimation_Click()
        {
            WzObject seletedWzObject = (WzObject)DataTree.SelectedNode.Tag;

            if (!AnimationBuilder.IsValidAnimationWzObject(seletedWzObject))
                return;

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog()
            {
                Title = HaRepacker.Properties.Resources.SelectOutApng,
                Filter = string.Format("{0}|*.png", HaRepacker.Properties.Resources.ApngFilter)
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            AnimationBuilder.ExtractAnimation((WzSubProperty)seletedWzObject, dialog.FileName, Program.ConfigurationManager.UserSettings.UseApngIncompatibilityFrame);
        }
        #endregion

        #region Animate
        /// <summary>
        /// Animate the list of selected canvases
        /// </summary>
        public void StartAnimateSelectedCanvas()
        {
            if (DataTree.SelectedNodes.Count == 0)
            {
                MessageBox.Show("Please select at least one or more canvas node.");
                return;
            }

            List<WzNode> selectedNodes = new List<WzNode>();
            foreach (WzNode node in DataTree.SelectedNodes)
            {
                selectedNodes.Add(node);
            }

            string path_title = ((WzNode)DataTree.SelectedNodes[0]).Parent?.FullPath ?? "Animate";

            Thread thread = new Thread(() =>
            {
                try
                {
                    ImageAnimationPreviewWindow previewWnd = new ImageAnimationPreviewWindow(selectedNodes, path_title);
                    previewWnd.Run();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error previewing animation. " + ex.ToString());
                }
            });
            thread.Start();
            // thread.Join();
        }

        private void nextLoopTime_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* if (nextLoopTime_comboBox == null)
                  return;

              switch (nextLoopTime_comboBox.SelectedIndex)
              {
                  case 1:
                      Program.ConfigurationManager.UserSettings.DelayNextLoop = 1000;
                      break;
                  case 2:
                      Program.ConfigurationManager.UserSettings.DelayNextLoop = 2000;
                      break;
                  case 3:
                      Program.ConfigurationManager.UserSettings.DelayNextLoop = 5000;
                      break;
                  case 4:
                      Program.ConfigurationManager.UserSettings.DelayNextLoop = 10000;
                      break;
                  default:
                      Program.ConfigurationManager.UserSettings.DelayNextLoop = Program.TimeStartAnimateDefault;
                      break;
              }*/
        }
        #endregion

        #region Buttons
        /// <summary>
        /// On texteditor save button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextEditor_SaveButtonClicked(object sender, EventArgs e)
        {
            if (DataTree.SelectedNode == null)
                return;

            WzNode node = (WzNode)DataTree.SelectedNode;
            WzObject obj = (WzObject)DataTree.SelectedNode.Tag;
            if (obj is WzLuaProperty luaProp)
            {
                string setText = textEditor.textEditor.Text;
                byte[] encBytes = luaProp.EncodeDecode(Encoding.ASCII.GetBytes(setText));
                luaProp.Value = encBytes;

                // highlight node to the user
                node.ChangedNodeProperty();
            } 
            else if (obj is WzStringProperty stringProp)
            {
                //if (stringProp.IsSpineAtlasResources)
               // {
                    string setText = textEditor.textEditor.Text;

                    stringProp.Value = setText;

                    // highlight node to the user
                    node.ChangedNodeProperty();
                /*  } 
                  else
                  {
                      throw new NotSupportedException("Usage of TextEditor for non-spine WzStringProperty.");
                  }*/
            }
        }
        #endregion 

        #region Batch Edit
        /// <summary>
        /// Check for image updates to the ImageRenderViewer that the user is currently selecting, after batch operation
        /// </summary>
        /// <param name="selectedTreeNode"></param>
        /// <param name="canvasPropBox"></param>
        private void RefreshSelectedImageToImageRenderviewer(object selectedTreeNode, ImageRenderViewer canvasPropBox) {
            // Check for updates to the changed canvas image that the user is currently selecting
            if (selectedTreeNode is WzCanvasProperty) // only allow button click if its an image property
            {
                WzCanvasProperty canvas = (WzCanvasProperty)selectedTreeNode;
                System.Drawing.Image img = canvas?.GetLinkedWzCanvasBitmap();
                if (img != null && canvas != null) {
                    canvasPropBox.BindingPropertyItem.SurfaceFormat = WzPngFormatExtensions.GetXNASurfaceFormat(canvas.PngProperty.Format);
                    canvasPropBox.BindingPropertyItem.Bitmap = (Bitmap)img;
                    canvasPropBox.BindingPropertyItem.BitmapBackup = (Bitmap)img;
                }
            }
        }

        /// <summary>
        /// Fix the '_inlink' and '_outlink' image property for compatibility to old MapleStory ver.
        /// </summary>
        public void FixLinkForOldMapleStory_OnClick()
        {
            // handle multiple nodes...
            int nodeCount = DataTree.SelectedNodes.Count;
            DateTime t0 = DateTime.Now;
            foreach (WzNode node in DataTree.SelectedNodes)
            {
                CheckImageNodeRecursively_linkRepair(node);
            }

            RefreshSelectedImageToImageRenderviewer(DataTree.SelectedNode.Tag, canvasPropBox);

            double ms = (DateTime.Now - t0).TotalMilliseconds;
            MessageBox.Show("Completed.\r\nElapsed time: " + ms + " ms (avg: " + (ms / nodeCount) + ")");
        }

        /// <summary>
        /// Check image node recursively, if it needs repairs for '_inlink' or '_outlink'
        /// </summary>
        /// <param name="node"></param>
        private void CheckImageNodeRecursively_linkRepair(WzNode node) {
            if (node.Tag is WzImage img) {
                if (!img.Parsed) {
                    img.ParseImage();
                }
                node.Reparse();
            }

            if (node.Tag is WzCanvasProperty property) {
                WzImageProperty linkedTarget = property.GetLinkedWzImageProperty();
                if (property.ContainsInlinkProperty() || property.ContainsOutlinkProperty()) // if its an inlink property, remove that before updating base image.
                {
                    if (property.ContainsInlinkProperty()) {
                        property.RemoveProperty(property[WzCanvasProperty.InlinkPropertyName]);
                        WzNode childInlinkNode = WzNode.GetChildNode(node, WzCanvasProperty.InlinkPropertyName);

                        childInlinkNode.DeleteWzNode(); // Delete '_inlink' node
                    }

                    if (property.ContainsOutlinkProperty()) // if its an outlink property, remove that before updating base image.
{
                        property.RemoveProperty(property[WzCanvasProperty.OutlinkPropertyName]);
                        WzNode childOutlinkNode = WzNode.GetChildNode(node, WzCanvasProperty.OutlinkPropertyName);

                        childOutlinkNode.DeleteWzNode(); // Delete '_outlink' node
                    }

                    property.PngProperty.PNG = linkedTarget.GetBitmap();

                    // Updates
                    node.ChangedNodeProperty();
                }
            }
            else {
                foreach (WzNode child in node.Nodes) {
                    CheckImageNodeRecursively_linkRepair(child);
                }
            }
            WzNode hash = WzNode.GetChildNode(node, "_hash");
            if (hash != null) { 
                hash.DeleteWzNode(); 
            }
        }

        /// <summary>
        /// AI Upscale all image currently in the selected node 
        /// by 4x with AI, then down-scale it by 50%.
        /// 
        /// if there is an 'origin' x & y coordinate in the WzNode, update that by x2
        /// <param name="downscaleFactor">The factor to downscale the image after upscaling.  0.5 = 50%, 0.375 = 37.5%</param>
        /// </summary>
        public async void AiBatchImageUpscaleEdit(float downscaleFactorAfter) {
            const float SCALE_UP_FACTOR = 4; // faactor to scale up to with neural networks

            // Reset progress bar
            mainProgressBar.Value = 20; // 20% at the start
            secondaryProgressBar.Value = 0;

            // disable inputs in the main UI from the user.
            gridMain.IsEnabled = false;

            Dispatcher currentDispatcher = this.Dispatcher;

            await Task.Run(async () => {
                // Image key = <image path>.GetHashCode().ToString()
                Dictionary<string, Tuple<Bitmap, WzCanvasProperty, WzNode>> toUpscaleImageDictionary = new Dictionary<string, Tuple<Bitmap, WzCanvasProperty, WzNode>>();

                // handle multiple nodes...
                int nodeCount = DataTree.SelectedNodes.Count;
                DateTime t0 = DateTime.Now;
                foreach (WzNode node in DataTree.SelectedNodes) {
                    UpscaleImageNodesRecursively(node, toUpscaleImageDictionary, currentDispatcher);
                }

                // Save all of the bitmap to a folder
                const string FILE_IN = "HaRepacker_ImageUpscaleInput";
                const string FILE_OUT = "HaRepacker_ImageUpscaleOutput";

                string pathIn = System.IO.Path.Combine(System.IO.Path.GetTempPath(), FILE_IN + "_" + new Random().Next().ToString()); // random folder in case multiple instances are running.
                string pathOut = System.IO.Path.Combine(System.IO.Path.GetTempPath(), FILE_OUT + "_" + new Random().Next().ToString()); // random folder in case multiple instances are running.

                try {
                    if (Directory.Exists(pathIn)) { // clear existing first
                        Directory.Delete(pathIn, true);
                    }
                    if (Directory.Exists(pathOut)) { // clear existing first
                        Directory.Delete(pathOut, true);
                    }
                    Directory.CreateDirectory(pathIn);
                    Directory.CreateDirectory(pathOut);

                    foreach (var kvp in toUpscaleImageDictionary) {
                        string fileName = System.IO.Path.GetFileName(kvp.Key) + ".png";
                        string filePath = System.IO.Path.Combine(pathIn, fileName);

                        kvp.Value.Item1.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    // Upscale all image saved in the folder
                    await RealESRGAN_AI_Upscale.EsrganNcnn.Run(pathIn, pathOut, (int) SCALE_UP_FACTOR);

                    // Update main progress bar to 50% once AI upscaling is done
                    mainProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                        mainProgressBar.Value = 50;
                    }));


                    foreach (KeyValuePair<string, Tuple<Bitmap, WzCanvasProperty, WzNode>> img in toUpscaleImageDictionary) {
                        string fileName = System.IO.Path.GetFileName(img.Key) + ".png";
                        string filePath = System.IO.Path.Combine(pathOut, fileName);

                        // Update secondary progress bar
                        // at the beginning of this loop, it should be 30%
                        // 60% once image is loaded
                        // then 90% once it is done downscaling image
                        secondaryProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                            secondaryProgressBar.Value = 30;
                        }));

                        // Get the bitmap from the output folder
                        using (System.Drawing.Bitmap originalBitmap = new System.Drawing.Bitmap(filePath)) {

                            byte[] bitmapBytes;
                            if (downscaleFactorAfter != 1) { // re-sizing is not necessary if its the same

                                // Calculate new dimensions (50% of original)
                                int newWidth = (int)(originalBitmap.Width * downscaleFactorAfter);
                                int newHeight = (int)(originalBitmap.Height * downscaleFactorAfter);

                                // Create a new bitmap with the reduced size
                                using (System.Drawing.Bitmap downscaledBitmap = new System.Drawing.Bitmap(newWidth, newHeight)) {
                                    // Update secondary progress bar
                                    // at the beginning of this loop, it should be 30%
                                    // 60% once image is loaded
                                    // then 90% once it is done downscaling image
                                    secondaryProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                                        secondaryProgressBar.Value = 60;
                                    }));

                                    // Use high quality downscaling
                                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(downscaledBitmap)) {
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                        g.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
                                    }

                                    // Update secondary progress bar
                                    // at the beginning of this loop, it should be 30%
                                    // 60% once image is loaded
                                    // then 90% once it is done downscaling image
                                    secondaryProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                                        secondaryProgressBar.Value = 90;
                                    }));

                                    // Convert downscaled Bitmap to byte array
                                    using (MemoryStream ms = new MemoryStream()) {
                                        downscaledBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                        bitmapBytes = ms.ToArray();
                                    }
                                }
                            }
                            else {
                                // Update secondary progress bar
                                // at the beginning of this loop, it should be 30%
                                // 60% once image is loaded
                                // then 90% once it is done downscaling image
                                secondaryProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                                    secondaryProgressBar.Value = 90;
                                }));

                                // Convert downscaled Bitmap to byte array
                                using (MemoryStream ms = new MemoryStream()) {
                                    originalBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    bitmapBytes = ms.ToArray();
                                }
                            }

                            // Create a new Bitmap from the byte array
                            using (MemoryStream ms = new MemoryStream(bitmapBytes)) {
                                System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(ms);

                                img.Value.Item2.PngProperty.PNG = newBitmap;

                                // Update 'origin' x/y if it exist
                                PointF pointXY = img.Value.Item2.GetCanvasOriginPosition();
                                if (pointXY != null && pointXY.X != 0 && pointXY.Y != 0) {
                                    // 4 * 0.25 = 1, 4 * 0.5 = 2
                                    PointF pointF = new PointF(
                                        pointXY.X * (SCALE_UP_FACTOR * downscaleFactorAfter),
                                        pointXY.Y * (SCALE_UP_FACTOR * downscaleFactorAfter));

                                    img.Value.Item2.SetCanvasOriginPosition(pointF);
                                }

                                // Update 'changed'
                                img.Value.Item3.ChangedNodeProperty();
                            }
                        }
                        // for each image completed thereafter, add main progress bar
                        mainProgressBar.Dispatcher.BeginInvoke(new Action(() => {
                            mainProgressBar.Value += (50d / (double) toUpscaleImageDictionary.Count);
                        }));
                    }

                    double ms_runtime = (DateTime.Now - t0).TotalSeconds;

                    MessageBox.Show("Completed.\r\nElapsed time: " + ms_runtime.ToString("N2") + " sec(s) (avg: " + (ms_runtime / nodeCount).ToString("N2") + ")");
                }
                catch (Exception exp) {
                    MessageBox.Show("Error", "Unable to upscale image, error:\r\n" + exp.ToString());
                }
                finally {
                    await canvasPropBox.Dispatcher.BeginInvoke(new Action(() => {
                        RefreshSelectedImageToImageRenderviewer(DataTree.SelectedNode.Tag, canvasPropBox);
                    }));
                    await gridMain.Dispatcher.BeginInvoke(new Action(() => {
                        // Reset progress bar
                        mainProgressBar.Value = 0;
                        secondaryProgressBar.Value = 0;

                        gridMain.IsEnabled = true; // disable inputs in the main UI from the user.
                    }));

                    // Clean-up
                    if (Directory.Exists(pathIn)) { // clear existing first
                        Directory.Delete(pathIn, true);
                    }
                    if (Directory.Exists(pathOut)) { // clear existing first
                        Directory.Delete(pathOut, true);
                    }

                    toUpscaleImageDictionary.Clear();
                    GC.Collect();
                }
            });
        }


        /// <summary>
        /// AI Upscale all image currently in the selected node (internal)
        /// </summary>
        /// <param name="node"></param>
        /// <param name="toUpscaleImageDictionary"></param>
        /// <param name="currentDispatcher">Main thread dispatcher</param>
        private void UpscaleImageNodesRecursively(WzNode node, Dictionary<string, Tuple<Bitmap, WzCanvasProperty, WzNode>> toUpscaleImageDictionary,
            Dispatcher currentDispatcher) {
            if (node == null || node.Tag == null) {
                return;
            }
            if (node.Tag is WzImage img) {
                if (!img.Parsed) {
                    currentDispatcher.BeginInvoke(new Action(() => {
                        img.ParseImage();
                    }));
                }
                currentDispatcher.BeginInvoke(new Action(() => {
                    node.Reparse();
                }));
            }

            if (node.Tag is WzCanvasProperty property) {
                WzImageProperty linkedTarget = property.GetLinkedWzImageProperty();
                if (!property.ContainsInlinkProperty() && !property.ContainsOutlinkProperty()) // skip link properties
                {
                    string key = property.FullPath.GetHashCode().ToString(); // happens when multiple nodes are selected while expanded
                    if (!toUpscaleImageDictionary.ContainsKey(key)) {
                        Bitmap bitmap = linkedTarget.GetBitmap();

                        toUpscaleImageDictionary.Add(property.FullPath.GetHashCode().ToString(), new Tuple<Bitmap, WzCanvasProperty, WzNode>(bitmap, property, node));
                    }
                }
            }
            else {
                foreach (WzNode child in node.Nodes) {
                    UpscaleImageNodesRecursively(child, toUpscaleImageDictionary, currentDispatcher);
                }
            }
        }
        #endregion

        #region Menu Item
        /// <summary>
        /// More option -- Shows ContextMenuStrip 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_MoreOption_Click(object sender, RoutedEventArgs e) {
            Button clickSrc = (Button)sender;

            clickSrc.ContextMenu.IsOpen = true;
            //  System.Windows.Forms.ContextMenuStrip contextMenu = new System.Windows.Forms.ContextMenuStrip();
            //  contextMenu.Show(clickSrc, 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_changeSound_Click(object sender, RoutedEventArgs e)
        {
            if (DataTree.SelectedNode.Tag is WzBinaryProperty)
            {
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = "Select the sound",
                    Filter = "Moving Pictures Experts Group Format 1 Audio Layer 3(*.mp3)|*.mp3"
                };
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                WzBinaryProperty prop;
                try
                {
                    prop = new WzBinaryProperty(((WzBinaryProperty)DataTree.SelectedNode.Tag).Name, dialog.FileName);
                }
                catch
                {
                    Warning.Error(Properties.Resources.MainImageLoadError);
                    return;
                }
                IPropertyContainer parent = (IPropertyContainer)((WzBinaryProperty)DataTree.SelectedNode.Tag).Parent;
                ((WzBinaryProperty)DataTree.SelectedNode.Tag).ParentImage.Changed = true;
                ((WzBinaryProperty)DataTree.SelectedNode.Tag).Remove();
                DataTree.SelectedNode.Tag = prop;
                parent.AddProperty(prop);
                mp3Player.SoundProperty = prop;
            }
        }

        /// <summary>
        /// Saving the sound from WzSoundProperty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_saveSound_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataTree.SelectedNode.Tag is WzBinaryProperty))
                return;
            WzBinaryProperty mp3 = (WzBinaryProperty)DataTree.SelectedNode.Tag;

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog()
            {
                FileName = mp3.Name,
                Title = "Select where to save the .mp3 file.",
                Filter = "Moving Pictures Experts Group Format 1 Audio Layer 3 (*.mp3)|*.mp3"
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            mp3.SaveToFile(dialog.FileName);
        }

        /// <summary>
        /// Saving the image from WzCanvasProperty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_saveImage_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataTree.SelectedNode.Tag is WzCanvasProperty) && !(DataTree.SelectedNode.Tag is WzUOLProperty))
            {
                return;
            }

            System.Drawing.Bitmap wzCanvasPropertyObjLocation = null;
            string fileName = string.Empty;

            if (DataTree.SelectedNode.Tag is WzCanvasProperty)
            {
                WzCanvasProperty canvas = (WzCanvasProperty)DataTree.SelectedNode.Tag;

                wzCanvasPropertyObjLocation = canvas.GetLinkedWzCanvasBitmap();
                fileName = canvas.Name;
            }
            else
            {
                WzObject linkValue = ((WzUOLProperty)DataTree.SelectedNode.Tag).LinkValue;
                if (linkValue is WzCanvasProperty)
                {
                    WzCanvasProperty canvas = (WzCanvasProperty)linkValue;

                    wzCanvasPropertyObjLocation = canvas.GetLinkedWzCanvasBitmap();
                    fileName = canvas.Name;
                }
                else
                    return;
            }
            if (wzCanvasPropertyObjLocation == null)
                return; // oops, we're fucked lulz

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog()
            {
                FileName = fileName,
                Title = "Select where to save the image...",
                Filter = "Portable Network Graphics (*.png)|*.png|CompuServe Graphics Interchange Format (*.gif)|*.gif|Bitmap (*.bmp)|*.bmp|Joint Photographic Experts Group Format (*.jpg)|*.jpg|Tagged Image File Format (*.tif)|*.tif"
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) 
                return;
            switch (dialog.FilterIndex)
            {
                case 1: //png
                    wzCanvasPropertyObjLocation.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    break;
                case 2: //gif
                    wzCanvasPropertyObjLocation.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Gif);
                    break;
                case 3: //bmp
                    wzCanvasPropertyObjLocation.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    break;
                case 4: //jpg
                    wzCanvasPropertyObjLocation.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case 5: //tiff
                    wzCanvasPropertyObjLocation.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Tiff);
                    break;
            }
        }

        /// <summary>
        /// Export .json, .atlas, as file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_ExportFile_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataTree.SelectedNode.Tag is WzStringProperty))
            {
                return;
            }
            WzStringProperty stProperty = DataTree.SelectedNode.Tag as WzStringProperty;

            string fileName = stProperty.Name;
            string value = stProperty.Value;

            string[] fileNameSplit = fileName.Split('.');
            string fileType = fileNameSplit.Length > 1 ? fileNameSplit[fileNameSplit.Length - 1] : "txt";

            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
            {
                FileName = fileName,
                Title = "Select where to save the file...",
                Filter = fileType + " files (*."+ fileType + ")|*."+ fileType + "|All files (*.*)|*.*" 
            }
            ;
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) 
                return;

            using (System.IO.FileStream fs = (System.IO.FileStream)saveFileDialog1.OpenFile())
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_changeImage_Click(object sender, RoutedEventArgs e) {
            if (DataTree.SelectedNode.Tag is WzCanvasProperty) // only allow button click if its an image property
            {
                System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog() {
                    Title = "Select an image",
                    Filter = "Supported Image Formats (*.png;*.bmp;*.jpg;*.gif;*.jpeg;*.tif;*.tiff)|*.png;*.bmp;*.jpg;*.gif;*.jpeg;*.tif;*.tiff"
                };
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                byte[] bitmapBytes = null;
                try {
                    using (System.Drawing.Bitmap originalBitmap = new System.Drawing.Bitmap(dialog.FileName)) {
                        using (MemoryStream ms = new MemoryStream()) {
                            originalBitmap.Save(ms, originalBitmap.RawFormat);
                            bitmapBytes = ms.ToArray();
                        }
                    }
                }
                catch {
                    Warning.Error(Properties.Resources.MainImageLoadError);
                    return;
                }
                //List<UndoRedoAction> actions = new List<UndoRedoAction>(); // Undo action

                if (bitmapBytes != null) {
                    MemoryStream ms = new MemoryStream(bitmapBytes); // dont close this
                    System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(ms);

                    ChangeCanvasPropBoxImage(newBitmap);
                }
            }
        }

        /// <summary>
        /// Changes the displayed image in 'canvasPropBox' with a user defined input.
        /// </summary>
        /// <param name="image"></param>
        /// <param name=""></param>
        public void ChangeCanvasPropBoxImage(Bitmap bmp) {
            if (DataTree.SelectedNode.Tag is WzCanvasProperty property) {
                WzNode parentCanvasNode = (WzNode)DataTree.SelectedNode;

                WzCanvasProperty selectedWzCanvas = property;

                if (selectedWzCanvas.ContainsInlinkProperty()) // if its an inlink property, remove that before updating base image.
                {
                    selectedWzCanvas.RemoveProperty(selectedWzCanvas[WzCanvasProperty.InlinkPropertyName]);

                    WzNode childInlinkNode = WzNode.GetChildNode(parentCanvasNode, WzCanvasProperty.InlinkPropertyName);

                    // Add undo actions
                    //actions.Add(UndoRedoManager.ObjectRemoved((WzNode)parentCanvasNode, childInlinkNode));
                    childInlinkNode.DeleteWzNode(); // Delete '_inlink' node

                    // TODO: changing _Inlink image crashes
                    // Mob2.wz/9400121/hit/0
                }
                else if (selectedWzCanvas.ContainsOutlinkProperty()) // if its an inlink property, remove that before updating base image.
                {
                    selectedWzCanvas.RemoveProperty(selectedWzCanvas[WzCanvasProperty.OutlinkPropertyName]);

                    WzNode childInlinkNode = WzNode.GetChildNode(parentCanvasNode, WzCanvasProperty.OutlinkPropertyName);

                    // Add undo actions
                    //actions.Add(UndoRedoManager.ObjectRemoved((WzNode)parentCanvasNode, childInlinkNode));
                    childInlinkNode.DeleteWzNode(); // Delete '_inlink' node
                }

                selectedWzCanvas.PngProperty.PNG = bmp;

                canvasPropBox.SetIsLoading(true);
                try {
                    canvasPropBox.BindingPropertyItem.SurfaceFormat = WzPngFormatExtensions.GetXNASurfaceFormat(selectedWzCanvas.PngProperty.Format);
                    canvasPropBox.BindingPropertyItem.Bitmap = bmp;
                    canvasPropBox.BindingPropertyItem.BitmapBackup = bmp;
                }
                finally {
                    canvasPropBox.SetIsLoading(false);
                }

                // flag changed for saving updates
                // and also node foreground color
                parentCanvasNode.ChangedNodeProperty();

                // Add undo actions
                //UndoRedoMan.AddUndoBatch(actions);
            }
        }
        #endregion

        #region Drag and Drop Image
        private bool bDragEnterActive = false;
        /// <summary>
        /// Scroll viewer drag enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvasPropBox_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Drag Enter");
            if (!bDragEnterActive)
            {
                bDragEnterActive = true;
            }
        }

        /// <summary>
        ///  Scroll viewer drag leave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvasPropBox_DragLeave(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Drag Leave");

            bDragEnterActive = false;
        }
        /// <summary>
        /// Scroll viewer drag drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void canvasPropBox_Drop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Drag Drop");
            if (bDragEnterActive && DataTree.SelectedNode.Tag is WzCanvasProperty) // only allow button click if its an image property
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length == 0)
                        return;

                    System.Drawing.Bitmap bmp;
                    try
                    {
                        bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(files[0]);
                    }
                    catch (Exception exp)
                    {
                        return;
                    }
                    if (bmp != null)
                        ChangeCanvasPropBoxImage(bmp);

                    //List<UndoRedoAction> actions = new List<UndoRedoAction>(); // Undo action
                }
            }
        }
        #endregion

        #region Copy & Paste
        /// <summary>
        /// Clones a WZ object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private WzObject CloneWzObject(WzObject obj)
        {
            if (obj is WzDirectory)
            {
                Warning.Error(Properties.Resources.MainCopyDirError);
                return null;
            }
            else if (obj is WzImage)
            {
                return ((WzImage)obj).DeepClone();
            }
            else if (obj is WzImageProperty)
            {
                return ((WzImageProperty)obj).DeepClone();
            }
            else
            {
                MapleLib.Helpers.ErrorLogger.Log(MapleLib.Helpers.ErrorLevel.MissingFeature, "The current WZ object type cannot be cloned " + obj.ToString() + " " + obj.FullPath);
                return null;
            }
        }

        /// <summary>
        /// Flag to determine if a copy task is currently active.
        /// </summary>
        private bool
            bPasteTaskActive = false;

        /// <summary>
        /// Copies from the selected Wz object
        /// </summary>
        public void DoCopy()
        {
            if (!Warning.Warn(Properties.Resources.MainConfirmCopy) || bPasteTaskActive)
                return;

            foreach (WzObject obj in clipboard)
            {
                //this causes minor weirdness with png's in copied nodes but otherwise memory is not free'd 
                obj.Dispose();
            }

            clipboard.Clear();

            foreach (WzNode node in DataTree.SelectedNodes)
            {
                WzObject clone = CloneWzObject((WzObject)((WzNode)node).Tag);
                if (clone != null)
                    clipboard.Add(clone);
            }
        }

        private ReplaceResult replaceBoxResult = ReplaceResult.NoneSelectedYet;

        /// <summary>
        /// Paste to the selected WzObject
        /// </summary>
        public void DoPaste()
        {
            if (!Warning.Warn(Properties.Resources.MainConfirmPaste))
                return;

            bPasteTaskActive = true;
            try
            {
                // Reset replace option
                replaceBoxResult = ReplaceResult.NoneSelectedYet;

                WzNode parent = (WzNode)DataTree.SelectedNode;
                WzObject parentObj = (WzObject)parent.Tag;

                if (parent != null && parent.Tag is WzImage && parent.Nodes.Count == 0)
                {
                    ParseOnDataTreeSelectedItem(parent); // only parse the main node.
                }

                if (parentObj is WzFile)
                    parentObj = ((WzFile)parentObj).WzDirectory;

                bool bNoToAllComplete = false;
                foreach (WzObject obj in clipboard)
                {
                    if (((obj is WzDirectory || obj is WzImage) && parentObj is WzDirectory) || (obj is WzImageProperty && parentObj is IPropertyContainer))
                    {
                        WzObject clone = CloneWzObject(obj);
                        if (clone == null)
                            continue;

                        WzNode node = new WzNode(clone, true);
                        WzNode child = WzNode.GetChildNode(parent, node.Text);
                        if (child != null) // A Child already exist
                        {
                            if (replaceBoxResult == ReplaceResult.NoneSelectedYet)
                            {
                                ReplaceBox.Show(node.Text, out replaceBoxResult);
                            }

                            switch (replaceBoxResult)
                            {
                                case ReplaceResult.No: // Skip just this
                                    replaceBoxResult = ReplaceResult.NoneSelectedYet; // reset after use
                                    break;

                                case ReplaceResult.Yes: // Replace just this
                                    child.DeleteWzNode();
                                    parent.AddNode(node, false);
                                    replaceBoxResult = ReplaceResult.NoneSelectedYet; // reset after use
                                    break;

                                case ReplaceResult.NoToAll:
                                    bNoToAllComplete = true;
                                    break;

                                case ReplaceResult.YesToAll:
                                    child.DeleteWzNode();
                                    parent.AddNode(node, false);
                                    break;
                            }

                            if (bNoToAllComplete)
                                break;
                        }
                        else // not not in this 
                        {
                            parent.AddNode(node, false);
                        }
                    }
                }
            }
            finally
            {
                bPasteTaskActive = false;
            }
        }
        #endregion

        #region UI layout
        /// <summary>
        /// Shows the selected data treeview object to UI
        /// </summary>
        /// <param name="obj"></param>
        private void ShowObjectValue(WzObject obj)
        {
            if (obj.WzFileParent != null && obj.WzFileParent.IsUnloaded) // this WZ is already unloaded from memory, dont attempt to display it (when the user clicks "reload" button while selection is on that)
                return;

            isLoading = true;

            try {
                mp3Player.SoundProperty = null;

                // Set file name binding
                _bindingPropertyItem.WzFileName = obj.Name;

                toolStripStatusLabel_additionalInfo.Text = "-"; // Reset additional info to default
                if (isSelectingWzMapFieldLimit) // previously already selected. update again
                {
                    isSelectingWzMapFieldLimit = false;
                }

                // Canvas animation
                if (DataTree.SelectedNodes.Count <= 1)
                {
                }
                else
                {
                    bool bIsAllCanvas = true;
                    // check if everything selected is WzUOLProperty and WzCanvasProperty
                    foreach (WzNode tree in DataTree.SelectedNodes)
                    {
                        WzObject wzobj = (WzObject)tree.Tag;
                        if (!(wzobj is WzUOLProperty) && !(wzobj is WzCanvasProperty))
                        {
                            bIsAllCanvas = false;
                            break;
                        }
                    }
                }

                // Set default layout collapsed state
                mp3Player.Visibility = Visibility.Collapsed;

                // Button collapsed state
                menuItem_changeImage.Visibility = Visibility.Collapsed;
                menuItem_saveImage.Visibility = Visibility.Collapsed;
                menuItem_changeSound.Visibility = Visibility.Collapsed;
                menuItem_saveSound.Visibility = Visibility.Collapsed;
                menuItem_exportFile.Visibility = Visibility.Collapsed;

                // Canvas collapsed state
                canvasPropBox.Visibility = Visibility.Collapsed;

                // Value`
                _bindingPropertyItem.WzFileValue = string.Empty;
                _bindingPropertyItem.ChangeReadOnlyAttribute(true, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue);

                // Field limit panel Map.wz/../fieldLimit
                fieldLimitPanelHost.Visibility = Visibility.Collapsed;
                // fieldType panel Map.wz/../fieldType
                fieldTypePanel.Visibility = Visibility.Collapsed;

                // Vector panel
                //_bindingPropertyItem.XYVector = new NotifyPointF(0, 0);
                _bindingPropertyItem.ChangeReadOnlyAttribute(true, _bindingPropertyItem, o => o.IsXYPanelReadOnly, o => o.XYVector);

                // Avalon Text editor
                textEditor.Visibility = Visibility.Collapsed;

                // vars
                bool bIsWzFile = obj is WzFile file;
                bool bIsWzDirectory = obj is WzDirectory;
                bool bIsWzImage = obj is WzImage;
                bool bIsWzLuaProperty = obj is WzLuaProperty;
                bool bIsWzSoundProperty = obj is WzBinaryProperty;
                bool bIsWzStringProperty = obj is WzStringProperty;
                bool bIsWzIntProperty = obj is WzIntProperty;
                bool bIsWzLongProperty = obj is WzLongProperty;
                bool bIsWzDoubleProperty = obj is WzDoubleProperty;
                bool bIsWzFloatProperty = obj is WzFloatProperty;
                bool bIsWzShortProperty = obj is WzShortProperty;
                bool bIsWzNullProperty = obj is WzNullProperty;
                bool bIsWzSubProperty = obj is WzSubProperty;
                bool bIsWzConvexProperty = obj is WzConvexProperty;

                bool bAnimateMoreButton = false; // The button to animate when there is more option under button_MoreOption

                // Set layout visibility
                if (bIsWzFile || bIsWzDirectory || bIsWzImage || bIsWzNullProperty || bIsWzSubProperty || bIsWzConvexProperty) {
                    /*if (obj is WzSubProperty) { // detect String.wz/Npc.img/ directory for AI related tools
                         if (obj.Parent.Name == "Npc.img") 
                         {
                             WzObject wzObj = obj.GetTopMostWzDirectory();
                             if (wzObj.Name == "String.wz" || (wzObj.Name.StartsWith("String") && wzObj.Name.EndsWith(".wz"))) 
                             {
                             }
                         }
                     }*/

                    if (bIsWzFile) {
                        _bindingPropertyItem.WzFileValue = (obj as WzFile).Header.Copyright;
                        _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // dont allow user to change fieldLimit manually
                    }
                }
                else if (obj is WzCanvasProperty canvasProp) {
                    bAnimateMoreButton = true; // flag

                    menuItem_changeImage.Visibility = Visibility.Visible;
                    menuItem_saveImage.Visibility = Visibility.Visible;

                    // Image
                    if (canvasProp.ContainsInlinkProperty() || canvasProp.ContainsOutlinkProperty()) {
                        System.Drawing.Image img = canvasProp.GetLinkedWzCanvasBitmap();
                        if (img != null) {
                            canvasPropBox.BindingPropertyItem.SurfaceFormat = WzPngFormatExtensions.GetXNASurfaceFormat(canvasProp.PngProperty.Format);
                            canvasPropBox.BindingPropertyItem.Bitmap = (System.Drawing.Bitmap)img;
                            canvasPropBox.BindingPropertyItem.BitmapBackup = (System.Drawing.Bitmap)img;
                        }
                    }
                    else {
                        Bitmap bmp = canvasProp.GetLinkedWzCanvasBitmap();

                        canvasPropBox.BindingPropertyItem.SurfaceFormat = WzPngFormatExtensions.GetXNASurfaceFormat(canvasProp.PngProperty.Format);
                        canvasPropBox.BindingPropertyItem.Bitmap = bmp;
                        canvasPropBox.BindingPropertyItem.BitmapBackup = bmp;
                    }
                    SetImageRenderView(canvasProp);
                }
                else if (obj is WzUOLProperty uolProperty) {
                    bAnimateMoreButton = true; // flag

                    // Image
                    WzObject linkValue = uolProperty.LinkValue;
                    if (linkValue is WzCanvasProperty canvasUOL) {
                        canvasPropBox.Visibility = Visibility.Visible;

                        Bitmap bmp = canvasUOL.GetLinkedWzCanvasBitmap();

                        canvasPropBox.BindingPropertyItem.SurfaceFormat = WzPngFormatExtensions.GetXNASurfaceFormat(canvasUOL.PngProperty.Format);
                        canvasPropBox.BindingPropertyItem.Bitmap = bmp; // in any event that the WzCanvasProperty is an '_inlink' or '_outlink'
                        canvasPropBox.BindingPropertyItem.BitmapBackup = bmp; // in any event that the WzCanvasProperty is an '_inlink' or '_outlink'

                        menuItem_saveImage.Visibility = Visibility.Visible; // dont show change image, as its a UOL

                        SetImageRenderView(canvasUOL);
                    }
                    else if (linkValue is WzBinaryProperty binProperty) // Sound, used rarely in wz. i.e Sound.wz/Rune/1/Destroy
                    {
                        mp3Player.Visibility = Visibility.Visible;
                        mp3Player.SoundProperty = binProperty;

                        menuItem_changeSound.Visibility = Visibility.Visible;
                        menuItem_saveSound.Visibility = Visibility.Visible;
                    }

                    // Value
                    // set wz file value binding
                    _bindingPropertyItem.WzFileValue = obj.ToString();
                    _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // can be changed
                }
                else if (bIsWzSoundProperty) {
                    bAnimateMoreButton = true; // flag

                    mp3Player.Visibility = Visibility.Visible;
                    mp3Player.SoundProperty = (WzBinaryProperty)obj;

                    menuItem_changeSound.Visibility = Visibility.Visible;
                    menuItem_saveSound.Visibility = Visibility.Visible;
                }
                else if (bIsWzLuaProperty) {
                    textEditor.Visibility = Visibility.Visible;
                    textEditor.SetHighlightingDefinitionIndex(2); // javascript

                    textEditor.textEditor.Text = obj.ToString();
                }
                else if (bIsWzStringProperty || bIsWzIntProperty || bIsWzLongProperty || bIsWzDoubleProperty || bIsWzFloatProperty || bIsWzShortProperty) {
                    // If text is a string property, expand the textbox
                    if (bIsWzStringProperty) {
                        WzStringProperty stringObj = (WzStringProperty)obj;

                        if (stringObj.IsSpineAtlasResources) // spine related resource
                        {
                            bAnimateMoreButton = true;
                            menuItem_exportFile.Visibility = Visibility.Visible;

                            textEditor.Visibility = Visibility.Visible;
                            textEditor.SetHighlightingDefinitionIndex(20); // json
                            textEditor.textEditor.Text = obj.ToString();


                            string path_title = stringObj.Parent?.FullPath ?? "Animate";

                            Thread thread = new Thread(() => {
                                try {
                                    WzSpineAnimationItem item = new WzSpineAnimationItem(stringObj);

                                    // Create xna window
                                    SpineAnimationWindow Window = new SpineAnimationWindow(item, path_title);
                                    Window.Run();
                                }
                                catch (Exception e) {
                                    Warning.Error("Error initialising/ rendering spine object. " + e.ToString());
                                }
                            });
                            thread.Start();
                            thread.Join();
                        }
                        else if (stringObj.Name.EndsWith(".json")) // Map001.wz/Back/BM3_3.img/spine/skeleton.json
                        {
                            bAnimateMoreButton = true;
                            menuItem_exportFile.Visibility = Visibility.Visible;

                            textEditor.Visibility = Visibility.Visible;
                            textEditor.SetHighlightingDefinitionIndex(20); // json
                            textEditor.textEditor.Text = obj.ToString();
                        }
                        else {
                            // Value
                            _bindingPropertyItem.WzFileValue = obj.ToString();
                            _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // can be changed

                            if (stringObj.Name == PORTAL_NAME_OBJ_NAME) // Portal type name display - "pn" = portal name 
                            {
                                PortalType portalType = PortalTypeExtensions.FromCode(obj.ToString());
                                
                                toolStripStatusLabel_additionalInfo.Text =
                                    string.Format(Properties.Resources.MainAdditionalInfo_PortalType, portalType.GetFriendlyName());
                            }
                            else {
                                //textPropBox.AcceptsReturn = true;
                                // TODO
                            }
                        }
                    }
                    else if (bIsWzLongProperty || bIsWzIntProperty || bIsWzShortProperty) {
                        // field limit UI
                        if (obj.Name == FIELD_LIMIT_OBJ_NAME) // fieldLimit
                        {
                            isSelectingWzMapFieldLimit = true;

                            ulong value_ = 0;
                            if (bIsWzLongProperty) // use uLong for field limit
                            {
                                value_ = (ulong)((WzLongProperty)obj).GetLong();
                            }
                            else if (bIsWzIntProperty) {
                                value_ = (ulong)((WzIntProperty)obj).GetLong();
                            }
                            else if (bIsWzShortProperty) {
                                value_ = (ulong)((WzShortProperty)obj).GetLong();
                            }

                            fieldLimitPanel1.UpdateFieldLimitCheckboxes(value_);

                            _bindingPropertyItem.WzFileValue = value_.ToString();
                            _bindingPropertyItem.ChangeReadOnlyAttribute(true, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // dont allow user to change fieldLimit manually

                            // Set visibility
                            fieldLimitPanelHost.Visibility = Visibility.Visible;
                        }
                        else {
                            long value_ = 0; // long for others, in the case of negative value
                            if (bIsWzLongProperty) {
                                value_ = ((WzLongProperty)obj).GetLong();
                            }
                            else if (bIsWzIntProperty) {
                                value_ = ((WzIntProperty)obj).GetLong();
                            }
                            else if (bIsWzShortProperty) {
                                value_ = ((WzShortProperty)obj).GetLong();
                            }
                            _bindingPropertyItem.WzFileValue = value_.ToString();
                            _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // can be changed
                        }
                    }
                    else if (bIsWzDoubleProperty || bIsWzFloatProperty) {
                        _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsWzValueReadOnly, o => o.WzFileValue); // can be changed

                        if (bIsWzFloatProperty) {
                            _bindingPropertyItem.WzFileValue = ((WzFloatProperty)obj).GetFloat().ToString();
                        }
                        else if (bIsWzDoubleProperty) {
                            _bindingPropertyItem.WzFileValue = ((WzDoubleProperty)obj).GetDouble().ToString();
                        }
                    }
                    else {
                        //textPropBox.AcceptsReturn = false;
                        // TODO
                    }
                }
                else if (obj is WzVectorProperty property) {
                    _bindingPropertyItem.XYVector.X = property.X.Value;
                    _bindingPropertyItem.XYVector.Y = property.Y.Value;

                    _bindingPropertyItem.ChangeReadOnlyAttribute(false, _bindingPropertyItem, o => o.IsXYPanelReadOnly, o => o.XYVector);
                }
                else {
                }

                // Animation button
                if (AnimationBuilder.IsValidAnimationWzObject(obj)) {
                    bAnimateMoreButton = true; // flag
                }
                else {
                }

                // Storyboard hint
                button_MoreOption.Visibility = bAnimateMoreButton ? Visibility.Visible : Visibility.Collapsed;
                if (bAnimateMoreButton) {
                    System.Windows.Media.Animation.Storyboard storyboard_moreAnimation = (System.Windows.Media.Animation.Storyboard)(this.FindResource("Storyboard_TreeviewItemSelectedAnimation"));
                    storyboard_moreAnimation.Begin();
                }
            } finally {
                isLoading = false;
            }
        }

        /// <summary>
        ///  Sets the ImageRender view on clicked, or via animation tick
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="animationFrame"></param>
        private void SetImageRenderView(WzCanvasProperty canvas)
        {
            // origin
            int? delay = canvas[WzCanvasProperty.AnimationDelayPropertyName]?.GetInt();
            PointF originVector = canvas.GetCanvasOriginPosition();
            PointF headVector = canvas.GetCanvasHeadPosition();
            PointF ltVector = canvas.GetCanvasLtPosition();

            canvasPropBox.SetIsLoading(true);
            try {
                canvasPropBox.SetParentMainPanel(this);

                // Set XY point to canvas xaml
                canvasPropBox.BindingPropertyItem.ParentWzCanvasProperty = canvas;
                canvasPropBox.BindingPropertyItem.Delay = delay ?? 0;
                canvasPropBox.BindingPropertyItem.CanvasVectorOrigin = new NotifyPointF(originVector);
                canvasPropBox.BindingPropertyItem.CanvasVectorHead = new NotifyPointF(headVector);
                canvasPropBox.BindingPropertyItem.CanvasVectorLt = new NotifyPointF(ltVector);

                if (canvasPropBox.Visibility != Visibility.Visible)
                    canvasPropBox.Visibility = Visibility.Visible;
            }
            finally {
                canvasPropBox.SetIsLoading(false);
            }
        }
        #endregion

        #region Property Item
        /// <summary>
        /// On property item selection changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void propertyGrid_PropertyChanged_1(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (isLoading) {
                return;
            }
            switch (e.PropertyName) {
                case "WzFileType": { // does nothing
                        break;
                    }
                case "WzFileName": {
                        if (DataTree.SelectedNode == null) 
                            return;

                        string setText = _bindingPropertyItem.WzFileName;

                        WzNode node = (WzNode)DataTree.SelectedNode;

                        if (node.Tag is WzFile) {

                        }
                        else if (WzNode.CanNodeBeInserted((WzNode)node.Parent, setText)) {
                            node.ChangeName(setText);
                        }
                        else
                            Warning.Error(Properties.Resources.MainNodeExists);
                        break;
                    }
                case "XYVector":
                case "WzFileValue": {
                        if (DataTree.SelectedNode == null)
                            return;

                        string setText = _bindingPropertyItem.WzFileValue;

                        bool bChangedNode = false;

                        WzNode node = (WzNode) DataTree.SelectedNode;
                        WzObject obj = (WzObject)DataTree.SelectedNode.Tag;

                        bool bIsWzFile = obj is WzFile file;
                        bool bIsWzDirectory = obj is WzDirectory;
                        bool bIsWzImage = obj is WzImage;
                        bool bIsWzLuaProperty = obj is WzLuaProperty;
                        bool bIsWzSoundProperty = obj is WzBinaryProperty;
                        bool bIsWzStringProperty = obj is WzStringProperty;
                        bool bIsWzIntProperty = obj is WzIntProperty;
                        bool bIsWzLongProperty = obj is WzLongProperty;
                        bool bIsWzDoubleProperty = obj is WzDoubleProperty;
                        bool bIsWzFloatProperty = obj is WzFloatProperty;
                        bool bIsWzShortProperty = obj is WzShortProperty;
                        bool bIsWzNullProperty = obj is WzNullProperty;
                        bool bIsWzSubProperty = obj is WzSubProperty;
                        bool bIsWzConvexProperty = obj is WzConvexProperty;


                        if (bIsWzFile) {
                            ((WzFile)node.Tag).Header.Copyright = setText;
                            ((WzFile)node.Tag).Header.RecalculateFileStart();

                            bChangedNode = true;
                        }
                        else if (obj is WzVectorProperty vectorProperty) {
                            vectorProperty.X.Value = (int) _bindingPropertyItem.XYVector.X;
                            vectorProperty.Y.Value = (int) _bindingPropertyItem.XYVector.Y;

                            bChangedNode = true;
                        }
                        else if (obj is WzStringProperty stringProperty) {
                            if (!stringProperty.IsSpineAtlasResources) {
                                stringProperty.Value = setText;

                                bChangedNode = true;
                            }
                            else {
                                throw new NotSupportedException("Usage of textBoxProp for spine WzStringProperty.");
                            }
                        }
                        else if (obj is WzFloatProperty floatProperty) {
                            float val;
                            if (!float.TryParse(setText, out val)) {
                                Warning.Error(string.Format(Properties.Resources.MainConversionError, setText));
                                return;
                            }
                            floatProperty.Value = val;

                            bChangedNode = true;
                        }
                        else if (obj is WzIntProperty intProperty) {
                            int val;
                            if (!int.TryParse(setText, out val)) {
                                Warning.Error(string.Format(Properties.Resources.MainConversionError, setText));
                                return;
                            }
                            intProperty.Value = val;

                            bChangedNode = true;
                        }
                        else if (obj is WzLongProperty longProperty) {
                            long val;
                            if (!long.TryParse(setText, out val)) {
                                Warning.Error(string.Format(Properties.Resources.MainConversionError, setText));
                                return;
                            }
                            longProperty.Value = val;

                            bChangedNode = true;
                        }
                        else if (obj is WzDoubleProperty doubleProperty) {
                            double val;
                            if (!double.TryParse(setText, out val)) {
                                Warning.Error(string.Format(Properties.Resources.MainConversionError, setText));
                                return;
                            }
                            doubleProperty.Value = val;

                            bChangedNode = true;
                        }
                        else if (obj is WzShortProperty shortProperty) {
                            short val;
                            if (!short.TryParse(setText, out val)) {
                                Warning.Error(string.Format(Properties.Resources.MainConversionError, setText));
                                return;
                            }
                            shortProperty.Value = val;

                            bChangedNode = true;
                        }
                        else if (obj is WzUOLProperty UOLProperty) {
                            UOLProperty.Value = setText;

                            bChangedNode = true;
                        }
                        else if (obj is WzLuaProperty) {
                            throw new NotSupportedException("Moved to TextEditor_SaveButtonClicked()");
                        }

                        if (bChangedNode) {
                            node.ChangedNodeProperty();
                        }
                        break;
                    }
                default: {
                        break;
                    }
            }
        }

        /// <summary>
        /// On field limit checkboxes changes, update the PropertyItem values accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FieldLimitPanel1_FieldLimitChanged(object sender, FieldLimitChangedEventArgs e) {
            _bindingPropertyItem.WzFileValue = e.FieldLimit.ToString();
        }
        #endregion

        #region Search

        /// <summary>
        /// On search box fade in completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Storyboard_Find_FadeIn_Completed(object sender, EventArgs e)
        {
            findBox.Focus();
        }

        private int searchidx = 0;
        private bool finished = false;
        private bool listSearchResults = false;
        private List<string> searchResultsList = new List<string>();
        private bool searchValues = true;
        private WzNode coloredNode = null;
        private int currentidx = 0;
        private string searchText = "";
        private bool extractImages = false;

        /// <summary>
        /// Close search box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_closeSearch_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Animation.Storyboard sbb = (System.Windows.Media.Animation.Storyboard)(this.FindResource("Storyboard_Find_FadeOut"));
            sbb.Begin();
        }

        private void SearchWzProperties(IPropertyContainer parent)
        {
            foreach (WzImageProperty prop in parent.WzProperties)
            {
                if ((0 <= prop.Name.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase)) || (searchValues && prop is WzStringProperty && (0 <= ((WzStringProperty)prop).Value.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase))))
                {
                    if (listSearchResults)
                        searchResultsList.Add(prop.FullPath.Replace(";", @"\"));
                    else if (currentidx == searchidx)
                    {
                        if (prop.HRTag == null)
                            ((WzNode)prop.ParentImage.HRTag).Reparse();
                        WzNode node = (WzNode)prop.HRTag;
                        //if (node.Style == null) node.Style = new ElementStyle();
                        node.BackColor = System.Drawing.Color.Yellow;
                        coloredNode = node;
                        node.EnsureVisible();
                        //DataTree.Focus();
                        finished = true;
                        searchidx++;
                        return;
                    }
                    else
                        currentidx++;
                }
                if (prop is IPropertyContainer && prop.WzProperties.Count != 0)
                {
                    SearchWzProperties((IPropertyContainer)prop);
                    if (finished)
                        return;
                }
            }
        }

        private void SearchTV(WzNode node)
        {
            foreach (WzNode subnode in node.Nodes)
            {
                if (0 <= subnode.Text.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (listSearchResults)
                        searchResultsList.Add(subnode.FullPath.Replace(";", @"\"));
                    else if (currentidx == searchidx)
                    {
                        //if (subnode.Style == null) subnode.Style = new ElementStyle();
                        subnode.BackColor = System.Drawing.Color.Yellow;
                        coloredNode = subnode;
                        subnode.EnsureVisible();
                        //DataTree.Focus();
                        finished = true;
                        searchidx++;
                        return;
                    }
                    else
                        currentidx++;
                }
                if (subnode.Tag is WzImage)
                {
                    WzImage img = (WzImage)subnode.Tag;
                    if (img.Parsed)
                        SearchWzProperties(img);
                    else if (extractImages)
                    {
                        img.ParseImage();
                        SearchWzProperties(img);
                    }
                    if (finished) return;
                }
                else SearchTV(subnode);
            }
        }

        /// <summary>
        /// Find all
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_allSearch_Click(object sender, RoutedEventArgs e)
        {
            if (coloredNode != null)
            {
                coloredNode.BackColor = System.Drawing.Color.White;
                coloredNode = null;
            }
            if (findBox.Text == "" || DataTree.Nodes.Count == 0)
                return;
            if (DataTree.SelectedNode == null)
                DataTree.SelectedNode = DataTree.Nodes[0];

            finished = false;
            listSearchResults = true;
            searchResultsList.Clear();
            //searchResultsBox.Items.Clear();
            searchValues = Program.ConfigurationManager.UserSettings.SearchStringValues;
            currentidx = 0;
            searchText = findBox.Text;
            extractImages = Program.ConfigurationManager.UserSettings.ParseImagesInSearch;
            foreach (WzNode node in DataTree.SelectedNodes)
            {
                if (node.Tag is WzImageProperty)
                    continue;
                else if (node.Tag is IPropertyContainer)
                    SearchWzProperties((IPropertyContainer)node.Tag);
                else
                    SearchTV(node);
            }

            SearchSelectionForm form = SearchSelectionForm.Show(searchResultsList);
            form.OnSelectionChanged += Form_OnSelectionChanged;

            findBox.Focus();
        }

        /// <summary>
        /// On search selection from SearchSelectionForm list changed
        /// </summary>
        /// <param name="str"></param>
        private void Form_OnSelectionChanged(string str)
        {
            string[] splitPath = str.Split(@"\".ToCharArray());
            WzNode node = null;
            System.Windows.Forms.TreeNodeCollection collection = DataTree.Nodes;
            for (int i = 0; i < splitPath.Length; i++)
            {
                node = GetNodeByName(collection, splitPath[i]);
                if (node != null)
                {
                    if (node.Tag is WzImage && !((WzImage)node.Tag).Parsed && i != splitPath.Length - 1)
                    {
                        ParseOnDataTreeSelectedItem(node, false);
                    }
                    collection = node.Nodes;
                }
            }
            if (node != null)
            {
                DataTree.SelectedNode = node;
                node.EnsureVisible();
                DataTree.RefreshSelectedNodes();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private WzNode GetNodeByName(System.Windows.Forms.TreeNodeCollection collection, string name)
        {
            foreach (WzNode node in collection)
                if (node.Text == name)
                    return node;
            return null;
        }

        /// <summary>
        /// Find next
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_nextSearch_Click(object sender, RoutedEventArgs e)
        {
            if (coloredNode != null)
            {
                coloredNode.BackColor = System.Drawing.Color.White;
                coloredNode = null;
            }
            if (findBox.Text == "" || DataTree.Nodes.Count == 0) return;
            if (DataTree.SelectedNode == null) DataTree.SelectedNode = DataTree.Nodes[0];
            finished = false;
            listSearchResults = false;
            searchResultsList.Clear();
            searchValues = Program.ConfigurationManager.UserSettings.SearchStringValues;
            currentidx = 0;
            searchText = findBox.Text;
            extractImages = Program.ConfigurationManager.UserSettings.ParseImagesInSearch;
            foreach (WzNode node in DataTree.SelectedNodes)
            {
                if (node.Tag is IPropertyContainer)
                    SearchWzProperties((IPropertyContainer)node.Tag);
                else if (node.Tag is WzImageProperty) continue;
                else SearchTV(node);
                if (finished) break;
            }
            if (!finished) { MessageBox.Show(Properties.Resources.MainTreeEnd); searchidx = 0; DataTree.SelectedNode.EnsureVisible(); }
            findBox.Focus();
        }

        private void findBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                button_nextSearch_Click(null, null);
                e.Handled = true;
            }
        }

        private void findBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchidx = 0;
        }
        #endregion
    }
}