﻿using Editor;
using System;
using Microsoft.Xna.Framework;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Configuration;
using Microsoft.Xna.Framework.Audio;

namespace Editor
{
    public partial class FormEditor : Form
    {
        public GameEditor Game { get => m_game; set { m_game = value; HookEvents(); } }
        private GameEditor m_game = null;
        private Process m_MGCBProcess = null;
        private Object m_dropped = null;


        public FormEditor()
        {
            InitializeComponent();
            KeyPreview = true;
            toolStripStatusLabel1.Text = Directory.GetCurrentDirectory();
            listBoxAssets.MouseDown += ListBoxAssets_MouseDown;
            listBoxPrefabs.MouseDown += ListBoxPrefabs_MouseDown;
        }

        private void UpdateModelsList()
        {
            listBoxLevel.Items.Clear();
            List<Models> models = Game.Project.CurrentLevel?.GetModelsList();
            foreach (Models model in models)
            {
                listBoxLevel.Items.Add(new ListItemLevel() { Model = model });
            }
        }
        private void UpdatePrefabsList()
        {
            listBoxPrefabs.Items.Clear();
            string[] prefabs = Directory.GetFiles(Game.Project.Folder, "*.prefab");
            foreach (string prefab in prefabs)
            {
                string fileName = Path.GetFileName(prefab);
                ListItemPrefab item = new() { Name = fileName };
                listBoxPrefabs.Items.Add(item);
            }
        }

        private void ListBoxPrefabs_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBoxPrefabs.Items.Count == 0) return;

            int index = listBoxPrefabs.IndexFromPoint(e.X, e.Y);
            if (index < 0) return;
            var lip = listBoxPrefabs.Items[index] as ListItemPrefab;
            DoDragDrop(lip, DragDropEffects.Copy);
        }

        private void ListBoxAssets_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBoxAssets.Items.Count == 0) return;

            int index = listBoxAssets.IndexFromPoint(e.X, e.Y);
            if (index < 0) return;
            var lia = listBoxAssets.Items[index] as ListItemAsset;
            if ((lia.Type == AssetTypes.MODEL) || (lia.Type == AssetTypes.TEXTURE) || (lia.Type == AssetTypes.SFX) || (lia.Type == AssetTypes.EFFECT))
            {
                DoDragDrop(lia, DragDropEffects.Copy);
            }
        }

        private void HookEvents()
        {
            Form gameForm = Control.FromHandle(m_game.Window.Handle) as Form;
            gameForm.MouseDown += GameForm_MouseDown;
            gameForm.MouseUp += GameForm_MouseUp;
            gameForm.MouseWheel += GameForm_MouseWheel;
            gameForm.MouseMove += GameForm_MouseMove;
            KeyDown += GameForm_KeyDown;
            KeyUp += GameForm_KeyUp;

            gameForm.DragDrop += GameForm_DragDrop;
            gameForm.DragOver += GameForm_DragOver;
            gameForm.AllowDrop = true;
        }

        private void GameForm_DragOver(object sender, DragEventArgs e)
        {
            m_dropped = null;
            Form gameForm = Control.FromHandle(m_game.Window.Handle) as Form;
            var p = gameForm.PointToClient(new System.Drawing.Point(e.X, e.Y));
            InputController.Instance.MousePosition = new Vector2(p.X, p.Y);
            e.Effect = DragDropEffects.None;
            if (e.Data.GetDataPresent(typeof(ListItemAsset)))
            {
                var lia = e.Data.GetData(typeof(ListItemAsset)) as ListItemAsset;
                ISelectable obj = m_game.Project.CurrentLevel.HandlePick(false);
                if (lia.Type == AssetTypes.MODEL)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else if ((lia.Type == AssetTypes.TEXTURE) || (lia.Type == AssetTypes.EFFECT))
                {
                    if (obj is IMaterial) m_dropped = obj as IMaterial;
                }
                else if (lia.Type == AssetTypes.SFX)
                {
                    if (obj is ISoundEmitter) m_dropped = obj as ISoundEmitter;
                }
                if (m_dropped != null)
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
            else if (e.Data.GetDataPresent(typeof(ListItemPrefab)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void GameForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListItemAsset)))
            {
                var lia = e.Data.GetData(typeof(ListItemAsset)) as ListItemAsset;
                if (lia.Type == AssetTypes.MODEL)
                {
                    Models model = new(m_game, lia.Name, "DefaultTexture", "DefaultEffect", Vector3.Zero, 1.0f);
                    m_game.Project.CurrentLevel.AddModel(model);
                    listBoxLevel.Items.Add(new ListItemLevel() { Model = model });
                }
                else if (lia.Type == AssetTypes.TEXTURE)
                {
                    IMaterial material = m_dropped as IMaterial;
                    material?.SetTexture(m_game, lia.Name);
                }
                else if (lia.Type == AssetTypes.EFFECT)
                {
                    IMaterial material = m_dropped as IMaterial;
                    material?.SetShader(m_game, lia.Name);
                }
                else if (lia.Type == AssetTypes.SFX)
                {
                    ISoundEmitter emitter = (ISoundEmitter)m_dropped;
                    ContextMenuStrip menuStrip = new();
                    var items = Enum.GetNames(typeof(SoundEffectTypes));
                    int index = 0;
                    foreach (var i in items)
                    {
                        ToolStripMenuItem menuItem = new(i);
                        menuItem.Click += MenuItem_Click;
                        menuItem.Name = index.ToString();
                        menuItem.Tag = lia;
                        menuStrip.Items.Add(menuItem);
                        index++;
                    }
                    menuStrip.Show(new System.Drawing.Point(e.X, e.Y));
                }
                else if(e.Data.GetDataPresent(typeof(ListItemPrefab)))
                {
                    var lip = e.Data.GetData(typeof(ListItemPrefab)) as ListItemPrefab;
                    string fileName = Path.Combine(Game.Project.Folder, lip.Name);
                    using var stream = File.Open(fileName, FileMode.Open);
                    using var reader = new BinaryReader(stream, Encoding.UTF8, false);
                    Models m = new Models();
                    m.Deserialize(reader, m_game);
                    m_game.Project.CurrentLevel?.AddModel(m);
                    listBoxLevel.Items.Add(new ListItemLevel() {  Model = m });
                }
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            ISoundEmitter emitter = (ISoundEmitter)m_dropped;
            var tmi = sender as ToolStripMenuItem;
            int index = Int32.Parse(tmi.Name);
            var lia = tmi.Tag as ListItemAsset;
            emitter.SoundEffects[index] = SFXInstance.Create(m_game, lia.Name);
        }

        private void listBoxLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxLevel.Items.Count == 0) return;

            Game.Project.CurrentLevel.ClearSelectedModels();
            int index = listBoxLevel.SelectedIndex;
            if (index == -1) return;
            var lia = listBoxLevel.Items[index] as ListItemLevel;
            lia.Model.Selected = true;
        }

        private void GameForm_MouseMove(object sender, MouseEventArgs e)
        {
            var p = new Vector2(e.Location.X, e.Location.Y);
            InputController.Instance.MousePosition = p;
        }
        private void GameForm_MouseWheel(object sender, MouseEventArgs e)
        {
            InputController.Instance.SetWheel(e.Delta / SystemInformation.MouseWheelScrollDelta);
        }
        private void GameForm_MouseUp(object sender, MouseEventArgs e)
        {
            InputController.Instance.SetButtonUp(e.Button);
            var p = new Vector2(e.Location.X, e.Location.Y);
            InputController.Instance.DragEnd = p;
        }
        private void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            InputController.Instance.SetButtonDown(e.Button);
            var p = new Vector2(e.Location.X, e.Location.Y);
            InputController.Instance.DragStart = p;
        }
        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            InputController.Instance.SetKeyUp(e.KeyCode);
            e.Handled = true;
        }
        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            InputController.Instance.SetKeyDown(e.KeyCode);
            e.Handled = true;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void FormEditor_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Game.Exit();
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void FormEditor_SizeChanged(object sender, EventArgs e)
        {
            if (Game == null) return;
            Game.AdjustAspectRatio();
        }

        private void splitContainer1_Panel1_SizeChanged(object sender, EventArgs e)
        {
            if (Game == null) return;
            Game.AdjustAspectRatio();
        }

        private void createToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Game.Project = new(Game, sfd.FileName);
                Game.Project.OnAssetsUpdated += Project_OnAssetsUpdated;
                Game.Project.AssetMonitor.UpdateAssetDB();
                UpdatePrefabsList();
                UpdateModelsList();
                Text = "Our Cool Editor - " + Game.Project.Name;
                Game.AdjustAspectRatio();
            }
            saveToolStripMenuItem_Click(sender, e);
        }

        private void Project_OnAssetsUpdated()
        {
            this.Invoke(delegate
            {
                listBoxAssets.Items.Clear();
                var assets = Game.Project.AssetMonitor.Assets;
                if (!assets.ContainsKey(AssetTypes.MODEL)) return;
                foreach (AssetTypes assetType in Enum.GetValues(typeof(AssetTypes)))
                {
                    if (assets.ContainsKey(assetType))
                    {

                        listBoxAssets.Items.Add(new ListItemAsset()
                        {
                            Name = assetType.ToString().ToUpper() + "S:",
                            Type = AssetTypes.NONE
                        });
                        foreach (string asset in assets[assetType])
                        {
                            ListItemAsset lia = new()
                            {
                                Name = asset,
                                Type = assetType
                            };
                            listBoxAssets.Items.Add(lia);
                        }

                        listBoxAssets.Items.Add(new ListItemAsset()
                        {
                            Name = " ",
                            Type = AssetTypes.NONE
                        });
                    }
                }
            });
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string fname = Path.Combine(Game.Project.Folder, Game.Project.Name);
            using var stream = File.Open(fname, FileMode.Create);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
            Game.Project.Serialize(writer);
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.Filter = "OCE FILES|*.oce";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                using var stream = File.Open(ofd.FileName, FileMode.Open);
                using var reader = new BinaryReader(stream, Encoding.UTF8, false);
                Game.Project = new();
                Game.Project.Deserialize(reader, Game);
                Game.Project.OnAssetsUpdated += Project_OnAssetsUpdated;
                Game.Project.AssetMonitor.UpdateAssetDB();
                UpdatePrefabsList();
                UpdateModelsList();
                Text = "Our Cool Editor - " + Game.Project.Name;
                Game.AdjustAspectRatio();
            }
        }

        private void addSunToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void addPlanetToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void addMoonToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void projectToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void propertyGrid_Click(object sender, EventArgs e)
        {

        }

        private void propertyGrid1_Click(object sender, EventArgs e)
        {

        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string mgcbEditorPath = ConfigurationManager.AppSettings["MGCB_EditorPath"];
            ProcessStartInfo startInfo = new()
            {
                FileName = "\"" + Path.Combine(mgcbEditorPath, "mgcb-editor-windows.exe") + "\"",
                Arguments = "\"" + Path.Combine(Game.Project.ContentFolder, "Content.mgcb") + "\""
            };
            m_MGCBProcess = Process.Start(startInfo);
        }
        private void FormEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_MGCBProcess == null) return;
            m_MGCBProcess.Kill();
        }

        private void listBoxAssets_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void splitContainer3_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer3_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer3_Panel2_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer4_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer4_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void createPrefabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var models = Game.Project.CurrentLevel.GetSelectedModels();
            if (models.Count == 0)
            {
                MessageBox.Show("Please select a game object in the level to convert to a prefab.",
                                "No Game Object Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Models m = models[0] as Models;
            string fileName = Path.Combine(Game.Project.Folder, m.Name) + ".prefab";
            if (File.Exists(fileName))
            {
                MessageBox.Show("Prefab already exists. Try renaming the game object or delete the existing prefab.",
                                "Prefab Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var stream = File.Open(fileName, FileMode.Create))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                m.Serialize(writer);
            }

            ListItemPrefab item = new() { Name = m.Name + ".prefab" };
            listBoxPrefabs.Items.Add(item);
        }

    }
}
